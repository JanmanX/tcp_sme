using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    [ClockedProcess]
    public partial class PacketIn : SimpleProcess
    {
        /////////////////////// Memory busses and ports
        private TrueDualPortMemory<byte> memory;

        [OutputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IControlA controlA;

        [InputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IReadResultA readResultA;

        [OutputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IControlB controlB;

        [InputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IReadResultB readResultB;
        private readonly int memory_size;


        ////////// Packet in from I_in proccess
        [InputBus]
        public WriteBus packetInBus;
        [InputBus]
        public ComputeProducerControlBus packetInComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus packetInComputeConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();


        //////////// Packet out to T
        [OutputBus]
        public ReadBus packetOutBus = Scope.CreateBus<ReadBus>();
        [OutputBus]
        public BufferProducerControlBus packetOutBufferProducerControlBusOut = Scope.CreateBus<BufferProducerControlBus>();
        [InputBus]
        public ConsumerControlBus packetOutBufferConsumerControlBusIn;


        public struct TempData{
            public byte protocol;
            public long frame_number; // Increments so we can distinguish between new packages
            public uint ip_id;
            public ushort total_len;
            public ulong ip_src_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong ip_src_addr_1; // Upper 8 bytes of IP addr
            public ulong ip_dst_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong ip_dst_addr_1; // Upper 8 bytes of IP addr
        }
        private TempData tmp_write_info;

        private int cur_ip_id;

        private DictionaryListSparseLinked segment_lookup;

        private MultiMemorySegmentsRingBufferFIFO<TempData> mem_calc;
        private readonly int mem_calc_num_segments = 10;

        private bool write_next_packet = true;

        private int cur_write_block_id = 0;



        // little ringbuffer for the data out
        private const int send_buffer_size = 4;
        private byte[] send_buffer = new byte[send_buffer_size];
        private SingleMemorySegmentsRingBufferFIFO<TempData> buffer_calc;

        private bool memory_requested = false;
        private bool memory_receiving = false;

        private bool send_buffer_ready = true;

        public PacketIn(TrueDualPortMemory<byte> memory, int memory_size){
            // Set up the header information
            this.memory = memory;
            this.memory_size = memory_size;
            this.controlB = memory.ControlB;
            this.controlA = memory.ControlA;
            this.readResultA = memory.ReadResultA;
            this.readResultB = memory.ReadResultB;
            this.mem_calc = new MultiMemorySegmentsRingBufferFIFO<TempData>(mem_calc_num_segments,memory_size);
            // XXX better magic numbers
            this.segment_lookup = new DictionaryListSparseLinked(10,100);
            // Define the litte ring buffer for the outgoing packets
            this.buffer_calc = new SingleMemorySegmentsRingBufferFIFO<TempData>(send_buffer_size,send_buffer_size);

        }

        protected override void OnTick()
        {
            Write();
            Send();

        }
        private void Write()
        {
            // We need to buffer the incoming packets, and save them in order.
            // This is done with the Multi memory segments.

            // When segmented packets are received, their respective multi memory segments are saved,
            // and the id is saved in an dict with <(packet_ident),[segment_pointer]>

            // Disable the write bus, enable if there is stuff in the packet
            controlA.Enabled = false;

            // If the data is valid, we write it
            if(packetInComputeProducerControlBusIn.valid){
                // This is a new packet
                if(write_next_packet)
                {

                    // Fill in the temporary data
                    tmp_write_info.ip_dst_addr_0 = packetInBus.ip_dst_addr_0;
                    tmp_write_info.ip_dst_addr_1 = packetInBus.ip_dst_addr_1;
                    tmp_write_info.ip_src_addr_0 = packetInBus.ip_src_addr_0;
                    tmp_write_info.ip_src_addr_1 = packetInBus.ip_src_addr_1;
                    tmp_write_info.protocol = packetInBus.protocol;
                    tmp_write_info.ip_id = packetInBus.ip_id;
                    tmp_write_info.frame_number = packetInBus.frame_number;
                    // Get the new block we write to, and save the metadata
                    cur_write_block_id = mem_calc.AllocateSegment(packetInBus.data_length);
                    mem_calc.SaveMetaData(cur_write_block_id,tmp_write_info);
                    // XXX: Fix fragmentation
                    // We have made the new packet, set the flag to false
                    write_next_packet = false;

                    Logging.log.Info($"New packet! frame_number:{packetInBus.frame_number} " +
                                     $"write_mem_id:{cur_write_block_id} " +
                                     $"segment_length:{packetInBus.data_length} ");
                }
                // Submit the data
                controlA.Enabled = true;
                controlA.IsWriting = true;
                int addr = mem_calc.SaveData(cur_write_block_id);
                controlA.Address = addr;
                Logging.log.Trace($"Receiving: data: {packetInBus.data:X2} "+
                                  $"addr: {addr} "+
                                  $"in memory block: {cur_write_block_id} "+
                                  $"data left: {packetInComputeProducerControlBusIn.bytes_left}");
                controlA.Data = packetInBus.data;

            }

            // Test if we are at the last byte in this segment, and mark it for next clock, where we get a the next packet
            if (packetInComputeProducerControlBusIn.bytes_left == 0)
            {
                write_next_packet = true;
            }
        }
        private void Send()
        {
            // Look in the FIFO buffer, and send the next element. If the element is ip segmented,
            // Look through the segments in order, and override the normal fifo order. This way ip segmentation is
            // solved in the buffer. TCP segmentation is solved in the dataout buffer.

            // TODO: If we are at an segment in the ringbuffer where we should submit an segment, but the segment
            // is not done, put it on the top of the FIFO queue, and update the segment pointer, then go to next packet

            // Reset until they are needed
            controlB.Enabled = false;
            packetOutBufferProducerControlBusOut.available = false;
            //packetOutBufferProducerControlBusOut.valid = false;



            ////////////// BUFFER code
            // if we are receiving stuff from the memory, save it to the buffer
            if(memory_receiving)
            {
                int buffer = buffer_calc.SaveData();
                byte data = readResultB.Data;
                send_buffer[buffer] = data;
                Logging.log.Trace($"Got memory. goes to buffer:{buffer} data:{data:X2}");
                buffer_calc.FinishFillingCurrentSaveSegment();
                memory_receiving = false;
            }

            // If the last clock had an request, we know that the next clock has
            // to be receiving stuff.
            if(memory_requested){
                memory_receiving = true;
                memory_requested = false;
            }

            // If there are no avaliable next segments, we cannot save data
            if(buffer_calc.NextSegmentReady())
            {
                // Get the address for the current focus element
                // XXX: Use list of ip segment sinstead of focus segment
                int addr = mem_calc.LoadData(mem_calc.FocusSegment());


                // If we actually can get the address(If buffer empty etc)
                if(addr != -1)
                {
                    Logging.log.Trace($"requesting memory from addr:{addr}");
                    // Request the data
                    controlB.Enabled = true;
                    controlB.IsWriting = false;
                    controlB.Address = addr;

                    // we now have an request from memory
                    memory_requested = true;

                    // We save the metadata onto the buffer
                    buffer_calc.NextSegment(mem_calc.LoadMetaData(mem_calc.FocusSegment()));
                }
            }


            // If the load segment is ready, we have avaliable data in the buffer
            if(buffer_calc.LoadSegmentReady())
            {
                packetOutBufferProducerControlBusOut.available = true;
            }


             ///////////// Sending code
            // They are ready, we submit stuff
            if(packetOutBufferConsumerControlBusIn.ready && buffer_calc.LoadSegmentReady())
            {
                packetOutBus.data_length = buffer_calc.MetadataCurrentLoadSegment().total_len;
                packetOutBus.ip_dst_addr_0 = buffer_calc.MetadataCurrentLoadSegment().ip_dst_addr_0;
                packetOutBus.ip_dst_addr_1 = buffer_calc.MetadataCurrentLoadSegment().ip_dst_addr_1;
                packetOutBus.ip_src_addr_0 = buffer_calc.MetadataCurrentLoadSegment().ip_src_addr_0;
                packetOutBus.ip_src_addr_1 = buffer_calc.MetadataCurrentLoadSegment().ip_src_addr_1;
                long frame_number = buffer_calc.MetadataCurrentLoadSegment().frame_number;
                packetOutBus.frame_number = frame_number;
                packetOutBus.fragment_offset = 0;
                packetOutBus.ip_id = buffer_calc.MetadataCurrentLoadSegment().ip_id;
                packetOutBus.protocol = buffer_calc.MetadataCurrentLoadSegment().protocol;
                int addr = buffer_calc.LoadData();
                byte data = send_buffer[addr];
                packetOutBus.data = data;
                buffer_calc.FinishReadingCurrentLoadSegment();

                packetOutBufferProducerControlBusOut.valid = true;
                Logging.log.Error($"Sending: data: {data:X2} buffer_addr: {addr} frame_number: {frame_number}");

            }else{
                //Logging.log.Error($"--------------------Not valid any more!!! ready:{packetOutBufferConsumerControlBusIn.ready}");
                packetOutBufferProducerControlBusOut.valid = false;
            }
            Logging.log.Warn($"The load segment status: {buffer_calc.LoadSegmentReady()} ready: {packetOutBufferConsumerControlBusIn.ready}");

        }
    }
}
