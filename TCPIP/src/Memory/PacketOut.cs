using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    [ClockedProcess]
    public partial class PacketOut : SimpleProcess
    {

         /////////////////////// Memory busses and ports
        private TrueDualPortMemory<byte> memory;

        [OutputBus]
        private readonly TrueDualPortMemory<byte>.IControlA controlA;

        [InputBus]
        private readonly TrueDualPortMemory<byte>.IReadResultA readResultA;

        [OutputBus]
        private readonly TrueDualPortMemory<byte>.IControlB controlB;

        [InputBus]
        private readonly TrueDualPortMemory<byte>.IReadResultB readResultB;
        private readonly int memory_size;

        ////////// Packet in from producer of packets
        [InputBus]
        public WriteBus packetIn;
        [InputBus]
        public ComputeProducerControlBus packetInComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus packetInComputeConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();


        //////////// Packet out to I out
        [OutputBus]
        public ReadBus packetOut = Scope.CreateBus<ReadBus>();
        [OutputBus]
        public BufferProducerControlBus packetOutBufferProducerControlBusOut = Scope.CreateBus<BufferProducerControlBus>();
        [InputBus]
        public ConsumerControlBus packetOutBufferConsumerControlBusIn;


        public struct TempData{
            public byte protocol;
            public long frame_number;
            public uint ip_id;
            public ushort total_len;
            public ushort accum_len; // Accumulator for the length.
            public ulong ip_src_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong ip_src_addr_1; // Upper 8 bytes of IP addr
            public ulong ip_dst_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong ip_dst_addr_1; // Upper 8 bytes of IP addr
        }
        private TempData tmp_write_ip_info;
        private TempData tmp_send_ip_info;
        private bool write_next_packet = true;

        private long cur_frame_number = long.MaxValue;

        // The memory calculations
        private SingleMemorySegmentsRingBufferFIFO<TempData> mem_calc;
        private readonly int mem_calc_num_segments = 100;

        // little ringbuffer for the data out
        public struct SendRingBuffer{
            public byte data;
            public ushort length;
            public int socket;
            public int sequence;
        }
        SendRingBuffer tempSendRingBuffer;
        private const int send_buffer_size = 4;
        private SendRingBuffer[] send_buffer = new SendRingBuffer[send_buffer_size];
        private SingleMemorySegmentsRingBufferFIFO<TempData> buffer_calc;

        private bool memory_requested = false;
        private bool memory_receiving = false;
        private bool send_preload = true;

        public PacketOut(TrueDualPortMemory<byte> memory, int memory_size)
        {
            // Set up the header information
            this.memory = memory;
            this.memory_size = memory_size;
            this.controlB = memory.ControlB;
            this.controlA = memory.ControlA;
            this.readResultA = memory.ReadResultA;
            this.readResultB = memory.ReadResultB;
            this.mem_calc = new SingleMemorySegmentsRingBufferFIFO<TempData>(mem_calc_num_segments,memory_size);
            // Define the little ring buffer for the outgoing packets
            this.buffer_calc = new SingleMemorySegmentsRingBufferFIFO<TempData>(send_buffer_size,send_buffer_size);
        }

        protected override void OnTick()
        {
            Send();
            Write();
        }
        private void Write()
        {
            // Disable the write bus, enable if there is stuff in the packet
            controlA.Enabled = false;

            // If the data is valid, we write it
            if(packetInComputeProducerControlBusIn.valid){
                // This is a new packet
                if(write_next_packet)
                {

                    // Fill in the temporary data
                    tmp_write_ip_info.ip_dst_addr_0 = packetIn.ip_dst_addr_0;
                    tmp_write_ip_info.ip_dst_addr_1 = packetIn.ip_dst_addr_1;
                    tmp_write_ip_info.ip_src_addr_0 = packetIn.ip_src_addr_0;
                    tmp_write_ip_info.ip_src_addr_1 = packetIn.ip_src_addr_1;
                    tmp_write_ip_info.protocol = packetIn.protocol;
                    tmp_write_ip_info.ip_id = packetIn.ip_id;
                    tmp_write_ip_info.frame_number = packetIn.frame_number;

                    // Set the accumulator and total length to zero, since we do not know how
                    // much space is needed yet
                    tmp_write_ip_info.total_len = 0;
                    tmp_write_ip_info.accum_len = 0;
                    // Get the new block we write to, and save the metadata
                    mem_calc.NextSegment(tmp_write_ip_info);
                    write_next_packet = false;
                    Logging.log.Warn($"New packet! frame_number: {packetIn.frame_number}");

                }
                // Submit the data
                controlA.Enabled = true;
                controlA.IsWriting = true;
                int addr = mem_calc.SaveData(packetIn.addr);
                controlA.Address = addr;
                Logging.log.Warn($"Receiving: data: 0x{packetIn.data:X2} "+
                                  $"addr: {addr} "+
                                  $"data left: {packetInComputeProducerControlBusIn.bytes_left}");
                controlA.Data = packetIn.data;

            }

            // Test if we are at the last byte in this segment, and mark it for next clock, where we get a the next packet
            if (packetInComputeProducerControlBusIn.bytes_left == 0)
            {
                if(write_next_packet == false)
                {
                    Logging.log.Info("Packet done!");
                    write_next_packet = true;
                    mem_calc.FinishFillingCurrentSaveSegment();
                }
            }
        }
        private void Send()
        {
            // Reset until they are needed
            controlB.Enabled = false;

            ////////////// BUFFER code
            // if we are receiving stuff from the memory, save it to the buffer
            if(memory_receiving)
            {
                int buffer = buffer_calc.SaveData();
                byte data = readResultB.Data;
                tempSendRingBuffer.data = data;
                tempSendRingBuffer.length = buffer_calc.MetadataCurrentSaveSegment().accum_len;
                send_buffer[buffer] = tempSendRingBuffer;
                Logging.log.Trace($"Got memory. goes to buffer:{buffer} data:0x{data:X2}");
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
                int addr = mem_calc.LoadData();

                // If we actually can get the address(If buffer empty etc)
                if(addr != -1)
                {
                    Logging.log.Trace($"Requesting memory from addr: {addr}");
                    // Request the data
                    controlB.Enabled = true;
                    controlB.IsWriting = false;
                    controlB.Address = addr;

                    // we now have an request from memory
                    memory_requested = true;

                    tmp_send_ip_info = mem_calc.MetadataCurrentLoadSegment();

                    tmp_send_ip_info.accum_len = (ushort)mem_calc.LoadDataBytesLeft();
                    // test if the total size is missing
                    if(tmp_send_ip_info.total_len == 0)
                    {
                        tmp_send_ip_info.total_len = tmp_send_ip_info.accum_len;
                        mem_calc.MetadataCurrentLoadSegment(tmp_send_ip_info);
                    }
                    if(tmp_send_ip_info.accum_len == 0)
                    {
                        mem_calc.FinishReadingCurrentLoadSegment();
                    }

                    // We save the metadata onto the buffer
                    buffer_calc.NextSegment(tmp_send_ip_info);
                }
            }

            ///////////// Sending code
            // They are ready, we submit stuff

            Logging.log.Info($"The load segment status: {buffer_calc.LoadSegmentReady()} ready: {packetOutBufferConsumerControlBusIn.ready}");


            packetOutBufferProducerControlBusOut.valid = buffer_calc.LoadSegmentReady();

            if(packetOutBufferConsumerControlBusIn.ready){
                send_preload = true;
            }

            if(send_preload && buffer_calc.LoadSegmentReady())
            {
                int addr = buffer_calc.LoadData();
                byte data = send_buffer[addr].data;
                int bytes_left = send_buffer[addr].length;
                packetOut.data = data;
                packetOut.ip_dst_addr_0 = buffer_calc.MetadataCurrentLoadSegment().ip_dst_addr_0;
                packetOut.ip_dst_addr_1 = buffer_calc.MetadataCurrentLoadSegment().ip_dst_addr_1;
                packetOut.ip_src_addr_0 = buffer_calc.MetadataCurrentLoadSegment().ip_src_addr_0;
                packetOut.ip_src_addr_1 = buffer_calc.MetadataCurrentLoadSegment().ip_src_addr_1;
                packetOut.frame_number = buffer_calc.MetadataCurrentLoadSegment().frame_number;
                packetOut.data_length = buffer_calc.MetadataCurrentLoadSegment().total_len;
                //packetOut.fragment_offset = 0;
                packetOut.ip_id = buffer_calc.MetadataCurrentLoadSegment().ip_id;
                packetOut.protocol = buffer_calc.MetadataCurrentLoadSegment().protocol;



                packetOutBufferProducerControlBusOut.valid = true;
                packetOutBufferProducerControlBusOut.bytes_left = (uint)bytes_left;

                buffer_calc.FinishReadingCurrentLoadSegment();

                send_preload = false;

                Logging.log.Info($"Sending: data: 0x{data:X2} " +
                                 $"buffer_addr: {addr} " +
                                 $"bytes left: {bytes_left} " +
                                 $"total length : {buffer_calc.MetadataCurrentLoadSegment().total_len}");
            }
        }
    }
}
