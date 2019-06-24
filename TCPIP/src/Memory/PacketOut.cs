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
            public long frame_number; // Increments so we can distinguish between new packages
            public uint ip_id;
            public ulong ip_src_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong ip_src_addr_1; // Upper 8 bytes of IP addr
            public ulong ip_dst_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong ip_dst_addr_1; // Upper 8 bytes of IP addr
        }
        private TempData tmp_ip_info;

        private long cur_frame_number = long.MaxValue;

        // The memory calculations
        private SingleMemorySegmentsRingBufferFIFO<TempData> mem_calc;
        private readonly int mem_calc_num_segments = 10;




        // Information about the send buffer. The send buffer contains
        // The history of the packets in an ordred fashion, from the
        // send_buffer_new_index being the newest index, and counting down,
        // being the one before etc. the buffer circles around,
        // so if the buffer is 4 big, and the newest index is 2,
        // the history(newest->oldest) like 2,1,0,3.
        private const int send_buffer_max = 4;

        private const int send_buffer_max_fill = 2;

        // Number illustrating how many entries in the buffer are ready
        private int send_buffer_count = 0;

        // The index for the memory, where it has just written
        private int send_buffer_memory_index = 0;

        // The index for the writer, where it should read from next
        private int send_buffer_write_index = 0;

        // The buffer definitions themselves
        public struct SendBufferData{
            public byte data;
            public int data_length;
            public TempData tempData;
        }
        private SendBufferData[] send_buffer = new SendBufferData[send_buffer_max];

        private bool memory_requested = false;
        private bool memory_receiving = false;

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
        }

        protected override void OnTick()
        {
            return;
            Send();
            Write();
        }

        private void Write(){
            // Disable the write bus, enable if there is stuff in the packet
            controlA.Enabled = false;

            // Data on the bus is currently valid

            if(packetInComputeProducerControlBusIn.valid){
                // This is a new packet
                if(packetIn.frame_number != cur_frame_number)
                {
                    tmp_ip_info.ip_dst_addr_0 = packetIn.ip_dst_addr_0;
                    tmp_ip_info.ip_dst_addr_1 = packetIn.ip_dst_addr_1;
                    tmp_ip_info.ip_src_addr_0 = packetIn.ip_src_addr_0;
                    tmp_ip_info.ip_src_addr_1 = packetIn.ip_src_addr_1;
                    tmp_ip_info.protocol = packetIn.protocol;
                    tmp_ip_info.ip_id = packetIn.ip_id;
                    tmp_ip_info.frame_number = packetIn.frame_number;
                    // Mark the last segment as filled
                    mem_calc.FinishFillingCurrentSaveSegment();
                    if(!mem_calc.NextSegment(tmp_ip_info))
                    {
                        throw new System.Exception("Cannot create next segment for the singleMemorySegment");
                    }
                }
                // Submit the data
                controlA.Enabled = true;
                controlA.IsWriting = true;
                controlA.Address = mem_calc.SaveData();
                controlA.Data = packetIn.data;
            }
        }
        private void Send()
        {

            // Reset until they are needed
            controlB.Enabled = false;
            //packetOutBufferProducerControlBusOut.available = false;


            ////////////// BUFFER code
            // if we are receiving stuff from the memory, save it to the buffer
            if(memory_receiving)
            {
                SendBufferData buf = send_buffer[send_buffer_memory_index];
                buf.data = readResultB.Data;
                // Increment the memory index by one
                send_buffer_memory_index = (send_buffer_memory_index + 1) % send_buffer_max;
            }
            // If the last clock had an request, we know that the next clock has
            // to be receiving stuff.
            if(memory_requested){
                memory_receiving = true;
                memory_requested = false;
            }

            // If the buffer is not full, we need to fill it
            if(send_buffer_count < send_buffer_max_fill)
            {
                // Request the data
                controlB.Enabled = true;
                controlB.IsWriting = false;
                int addr = mem_calc.LoadData();
                // If we actually can get the address(If buffer empty etc)
                if(addr != -1)
                {
                    controlB.Address = addr;
                    // Increment the send buffer count
                    send_buffer_count++;
                    // We have requested memory
                    memory_requested = true;
                    // We need to save the current meta data, since we have to wait
                    // two clocks for the actual data. This means that the meta data is
                    // two clocks in front of the actual data
                    SendBufferData buf = send_buffer[(send_buffer_memory_index + 2) % send_buffer_max];
                    buf.tempData = mem_calc.MetadataCurrentLoadSegment();
                    buf.data_length = mem_calc.LoadDataBytesLeft(); // Is the full segment since we have not
                    send_buffer[(send_buffer_memory_index + 2) % send_buffer_max] = buf;
                    // If we are at the last byte in that specific load data, we request a new one
                    if(mem_calc.LoadDataBytesLeft() == 0)
                    {
                        mem_calc.FinishReadingCurrentLoadSegment();
                    }
                }

            }
            // The indexes are not the same, therefore there must be data on the pipeline
            if(send_buffer_memory_index != send_buffer_write_index)
            {
                //packetOutBufferProducerControlBusOut.available = true;
            }

            ///////////// Sending code
            // They are ready, we submit stuff
            if(packetOutBufferConsumerControlBusIn.ready /* && packetOutBufferProducerControlBusOut.available*/)
            {
                SendBufferData buf = send_buffer[send_buffer_write_index];
                packetOut.data = buf.data;
                packetOut.data_length = buf.data_length;
                packetOut.ip_dst_addr_0 = buf.tempData.ip_dst_addr_0;
                packetOut.ip_dst_addr_1 = buf.tempData.ip_dst_addr_1;
                packetOut.ip_src_addr_0 = buf.tempData.ip_src_addr_0;
                packetOut.ip_src_addr_1 = buf.tempData.ip_src_addr_1;
                packetOut.frame_number = buf.tempData.frame_number;
                // XXX: fragmenttation for us?
                packetOut.fragment_offset = 0;
                packetOut.ip_id = buf.tempData.ip_id;
                packetOut.protocol = buf.tempData.protocol;
                // Increment the memory index by one
                send_buffer_write_index = (send_buffer_write_index + 1) % send_buffer_max;
            }
        }
    }
}
