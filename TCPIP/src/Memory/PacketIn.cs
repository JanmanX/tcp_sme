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
        private TempData tmp_ip_info;

        private long cur_frame_number = long.MaxValue;

        // The current id,s defining what memory block is written or sending from
        private int id_send;
        private int id_write;

        // Indicators for clock offsets when reading from memory
        private bool send_requested = false;
        private bool send_receiving = false;
        private bool send_last_byte_requested = false;



        private MultiMemorySegmentsRingBufferFIFO<TempData> mem_calc;
        private readonly int mem_calc_num_segments = 10;

        public PacketIn(TrueDualPortMemory<byte> memory, int memory_size){
            // Set up the header information
            this.memory = memory;
            this.memory_size = memory_size;
            this.controlB = memory.ControlB;
            this.controlA = memory.ControlA;
            this.readResultA = memory.ReadResultA;
            this.readResultB = memory.ReadResultB;
            this.mem_calc = new MultiMemorySegmentsRingBufferFIFO<TempData>(mem_calc_num_segments,memory_size);

        }

        protected override void OnTick()
        {
            // Send out new packs to T
            Send();
            // Write to memory what we got from I_in
            Write();
        }

        private void Write(){
            // Disable the write bus, enable if there is stuff in the packet
            controlA.Enabled = false;

            // Data on the bus is currently valid

            if(packetInComputeProducerControlBusIn.valid){
                // This is a new packet
                if(packetInBus.frame_number != cur_frame_number)
                {
                    // get the new write id
                    id_write = mem_calc.AllocateSegment(packetInBus.data_length);
                    tmp_ip_info.ip_dst_addr_0 = packetInBus.ip_dst_addr_0;
                    tmp_ip_info.ip_dst_addr_1 = packetInBus.ip_dst_addr_1;
                    tmp_ip_info.ip_src_addr_0 = packetInBus.ip_src_addr_0;
                    tmp_ip_info.ip_src_addr_1 = packetInBus.ip_src_addr_1;
                    tmp_ip_info.protocol = packetInBus.protocol;
                    tmp_ip_info.ip_id = packetInBus.ip_id;
                    tmp_ip_info.frame_number = packetInBus.frame_number;
                    // Save the struct in the memory structure
                    mem_calc.SaveMetaData(id_write,tmp_ip_info);
                }
                // Submit the data
                controlA.Enabled = true;
                controlA.IsWriting = true;
                controlA.Address = mem_calc.SaveData(id_write);
                controlA.Data = packetInBus.data;
            }
        }
        private void Send()
        {
            controlB.Enabled = false;
            // Check if we have packets ready
            // XXXX Lookup id if split packet

            id_send = mem_calc.FocusSegment();

            // If we are to receive stuff, but the request are false
            // we can assume that we need to roll back the last counter by one
            if(send_receiving && !send_requested && !send_last_byte_requested){
                mem_calc.SegmentRollback(id_send);
            }

            // We are now receiving stuff from memory, send to the consumer
            // If we are not, say to T that the data is not valid
            if(send_receiving){
                packetOutBufferProducerControlBusOut.valid = true;
                // XXX id_send can change to different segment that what we got from ram
                packetOutBufferProducerControlBusOut.bytes_left = (uint)mem_calc.SegmentBytesLeft(id_send);
                TempData temp_data = mem_calc.LoadMetaData(id_send);
                packetOutBus.data = readResultB.Data;
                packetOutBus.ip_dst_addr_0 = temp_data.ip_dst_addr_0;
                packetOutBus.ip_dst_addr_1 = temp_data.ip_dst_addr_1;
                packetOutBus.ip_src_addr_0 = temp_data.ip_src_addr_0;
                packetOutBus.ip_src_addr_1 = temp_data.ip_src_addr_1;
                packetOutBus.protocol = temp_data.protocol;
                packetOutBus.frame_number = temp_data.frame_number;
                packetOutBus.ip_id = temp_data.ip_id;
                //packetOut.frame_number = XXX;
                // We reset receiving, since it needs to be set implicit
                send_receiving = false;
            }else{
                packetOutBufferProducerControlBusOut.valid = false;
            }

            // If the last clock set the request to true, we must be
            // receiving in the next, therefore set the send_receiving to true
            // The request are set to false, since request must be set implicit
            if(send_requested){
                send_requested = false;
                send_receiving = true;
            }

            // We have a full segment ready, we can send it
            if (mem_calc.IsSegmentFull(id_send)){
                packetOutBufferProducerControlBusOut.available = true;
            }else{
                packetOutBufferProducerControlBusOut.available = false;
            }

            // The consumer are ready, ask memory and mark that we requested memory
            if(packetOutBufferConsumerControlBusIn.ready){
                controlB.Enabled = true;
                controlB.IsWriting = false;
                controlB.Address = mem_calc.LoadData(id_send);
                send_requested = true;
                // If we get a request for the last byte, we do not roll back
                send_last_byte_requested = mem_calc.IsSegmentDone(id_send);
            }
        }
    }
}
