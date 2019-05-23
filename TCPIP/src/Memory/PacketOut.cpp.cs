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

        public struct RingEntryIP{
            public byte protocol;
            public ushort total_len;
            public ulong src_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong src_addr_1; // Upper 8 bytes of IP addr
            public ulong dst_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong dst_addr_1; // Upper 8 bytes of IP addr
        }
        private RingEntryIP tmp_ip_info;
        public struct RingEntry{
            public int start; // Include bit
            public int stop; // exclude last bit
            public int current; // The current bit being written
            public bool done; // Mark if we are done with all data, and memory transactions are done
            public bool requesting; // If we have requested for data
            public bool receiving; // Mark if we are currently receiving data form memory
            public long frame_number; // The framenumber of the current packet
            public RingEntryIP ip;
        }
        private enum MemoryTypeManager{
            Transport,
            Internet,
            Out
        }
        private int in_pointer = 0; // The pointer for the data in packages
        private int out_pointer = 0; // The pointer for the out packages

        private int transport_pointer = 0;
        private int internet_pointer = 0;

        private long transport_number = long.MaxValue;
        private long internet_number = long.MaxValue;

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

        [InputBus]
        public readonly PacketOut.PacketOutBus bus_in_internet = Scope.CreateBus<PacketOut.PacketOutBus>();
        [InputBus]
        public readonly PacketOut.PacketOutBus bus_in_transport = Scope.CreateBus<PacketOut.PacketOutBus>();
        [OutputBus]
        public readonly PacketOut.PacketOutBus bus_out = Scope.CreateBus<PacketOut.PacketOutBus>();

        [InputBus]
        public readonly ControlProducer bus_in_internet_control_producer = Scope.CreateBus<ControlProducer>();
        [InputBus]
        public readonly ControlProducer bus_in_transport_control_producer = Scope.CreateBus<ControlProducer>();
        [OutputBus]
        public readonly ControlProducer bus_out_control_producer = Scope.CreateBus<ControlProducer>();


        [InputBus]
        public readonly ControlConsumer bus_in_internet_control_consumer = Scope.CreateBus<ControlConsumer>();
        [InputBus]
        public readonly ControlConsumer bus_in_transport_control_consumer = Scope.CreateBus<ControlConsumer>();
        [OutputBus]
        public readonly ControlConsumer bus_out_control_consumer = Scope.CreateBus<ControlConsumer>();

        private const short ring_bus_size = 10;
        private RingEntry[] ring_bus = new RingEntry[ring_bus_size];


        public PacketOut(TrueDualPortMemory<byte> memory, int memory_size){
            // Set up the header information
            this.memory = memory;
            this.memory_size = memory_size;
            this.controlB = memory.ControlB;
            this.controlA = memory.ControlA;
            this.readResultA = memory.ReadResultA;
            this.readResultB = memory.ReadResultB;
        }

        protected override void OnTick()
        {
            // If transport had nothing, read internet
            if(!ReadTransport()){
                ReadInternet();
            }

            Write();
        }

        private void DisablePorts(){
            this.controlA.Enabled = false;
            this.controlB.Enabled = false;
        }
        // Read from the data input, use the memorybusA for now, possibly add 3 busses for better interaction?
        private bool ReadInternet(){
            // Disable ports until needed
            DisablePorts();

            if (bus_in_internet.active)
            {
                RingEntry cur_range = ring_bus[internet_pointer];

                // If it is a new packet, add it to the pool, else append to current transaction
                if(bus_in_internet.frame_number != internet_number)
                {
                    // Mark the current range as done, so we can transmit it further
                    cur_range.done = true;
                    cur_range.current = 0;
                    internet_number = bus_in_internet.frame_number;
                    tmp_ip_info.dst_addr_0 = bus_in_internet.ip_dst_addr_0;
                    tmp_ip_info.dst_addr_1 = bus_in_internet.ip_dst_addr_1;
                    tmp_ip_info.src_addr_0 = bus_in_internet.ip_src_addr_0;
                    tmp_ip_info.src_addr_1 = bus_in_internet.ip_src_addr_1;
                    tmp_ip_info.total_len = (ushort)bus_in_internet.data_length;
                    tmp_ip_info.protocol = bus_in_internet.ip_protocol;
                    internet_pointer = AddRingEntry(bus_in_internet.data_length,internet_number);

                }

                SaveData(bus_in_internet.data,MemoryTypeManager.Internet,internet_pointer);

                if(cur_range.current == bus_in_internet.data_length - 1)
                {
                    LOGGER.INFO($"Packet received");
                    cur_range.done = true;
                    cur_range.current = 0;
                    ring_bus[internet_pointer] = cur_range;

                }
                return true;
            }
            return false;
        }

        // Read from the data input, use the memorybusA for now, possibly add 3 busses for better interaction?
        private bool ReadTransport(){
            // Disable ports until needed
            DisablePorts();

            if (bus_in_transport.active)
            {
                RingEntry cur_range = ring_bus[transport_pointer];

                // If it is a new packet, add it to the pool, else append to current transaction
                if(bus_in_transport.frame_number != transport_number)
                {
                    // Mark the current range as done, so we can transmit it further
                    cur_range.done = true;
                    cur_range.current = 0;
                    transport_number = bus_in_transport.frame_number;
                    tmp_ip_info.dst_addr_0 = bus_in_transport.ip_dst_addr_0;
                    tmp_ip_info.dst_addr_1 = bus_in_transport.ip_dst_addr_1;
                    tmp_ip_info.src_addr_0 = bus_in_transport.ip_src_addr_0;
                    tmp_ip_info.src_addr_1 = bus_in_transport.ip_src_addr_1;
                    tmp_ip_info.total_len = (ushort)bus_in_transport.data_length;
                    tmp_ip_info.protocol = bus_in_transport.ip_protocol;
                    transport_pointer = AddRingEntry(bus_in_transport.data_length,transport_number);
                }

                SaveData(bus_in_transport.data,MemoryTypeManager.Transport,transport_pointer);

                if(cur_range.current == bus_in_transport.data_length - 1)
                {
                    LOGGER.INFO($"Packet received");
                    cur_range.done = true;
                    cur_range.current = 0;
                    ring_bus[transport_pointer] = cur_range;

                }
                return true;
            }
            return false;
        }


        private void Write(){
            if(!bus_out_control_consumer.ready){
                return;
            }
            // Only activate bus if we actually need it
            bus_out.active = false;
            // XXX Use another memory manager, so we can have controlC?
            RingEntry range = ring_bus[out_pointer];
            int address = (range.start + range.current) % memory_size;

            // If we are receiving stuff from the memory, pass it on
            if(range.receiving){
                LOGGER.INFO($"Receiving from memory {this.readResultB.Data:X2}");
                range.receiving = false;
                bus_out.active = true;
                bus_out.data = this.readResultB.Data;
                bus_out.frame_number = range.frame_number;
                bus_out.ip_protocol = range.ip.protocol;
                bus_out.ip_dst_addr_0 = range.ip.dst_addr_0;
                bus_out.ip_dst_addr_1 = range.ip.dst_addr_1;
                bus_out.ip_src_addr_0 = range.ip.src_addr_0;
                bus_out.ip_src_addr_1 = range.ip.src_addr_1;
                bus_out.data_length = range.ip.total_len;
                bus_out.active = true;

                // If there are no futher data requests, go to next output buffer
                if(!range.requesting){
                    out_pointer = (out_pointer + 1) % ring_bus_size;
                }
            }

            // if the current range is done
            if(range.done){
                // If last range is now requesting, we must be reciving this clock cycle
                if(range.requesting){
                    range.receiving = true;
                }
                // If we are out of bounds, we stop, and go to next packet
                if(address == range.stop){
                    LOGGER.ERROR($"Done sending id:{out_pointer} with {range.start},{range.stop}");
                    range.done = false;
                    range.requesting = false;
                    range.current = 0;


                }
                // else we request the memory, and set the current byte to the next
                else
                {
                    //LOGGER.INFO($"Read request to {address}");
                    this.controlB.Enabled = true;
                    this.controlB.Address = address;
                    this.controlB.IsWriting = false;
                    range.requesting = true;
                    range.current++;
                }

            }
            ring_bus[out_pointer] = range;
        }


        private void SaveData(byte data, MemoryTypeManager type, int cur_pointer){
            RingEntry range = ring_bus[cur_pointer];
            int address = (range.start + range.current) % memory_size;
            LOGGER.INFO($"SaveData data:{data:X2} addr:{address} pointer:{cur_pointer} range:{range.start},{range.stop},{range.current}");
            switch (type)
            {
                case MemoryTypeManager.Internet:
                    this.controlA.Enabled = true;
                    this.controlA.Address = address;
                    this.controlA.Data = data;
                    this.controlA.IsWriting = true;
                    break;
                case MemoryTypeManager.Transport:
                    this.controlA.Enabled = true;
                    this.controlA.Address = address;
                    this.controlA.Data = data;
                    this.controlA.IsWriting = true;
                    break;
                default:
                    LOGGER.ERROR($"Current type {type} not defined as an savable type!");
                    break;
            }
            range.current++;
            ring_bus[cur_pointer] = range;
        }

        private int AddRingEntry(int size, long frame_number){
            // XXX Add guard for overlapping ring elements
            // Last range, so we can append to buffer
            int last_pointer = (in_pointer - 1 > 0) ?
                in_pointer - 1 :
                ring_bus_size - 1;
            RingEntry last_range = ring_bus[last_pointer];
            RingEntry range = ring_bus[in_pointer];
            // We start one after the last range ended
            range.start = last_range.stop;
            // If the memoryrange overflows, we wrap it around by modulo
            range.stop = (range.start + size) % memory_size;
            range.current = 0;
            range.frame_number = frame_number;
            range.done = false;
            range.requesting = false;
            range.ip.total_len = tmp_ip_info.total_len;
            range.ip.dst_addr_0 = tmp_ip_info.dst_addr_0;
            range.ip.dst_addr_1 = tmp_ip_info.dst_addr_1;
            range.ip.src_addr_0 = tmp_ip_info.src_addr_0;
            range.ip.src_addr_1 = tmp_ip_info.src_addr_1;
            range.ip.protocol = tmp_ip_info.protocol;
            // Saving the struct
            ring_bus[in_pointer] = range;

            LOGGER.INFO($"Adding ring entry start:{range.start} stop:{range.stop} pointer:{in_pointer}");
            // Increment the pointer by one, and loop around, return which element we worked on
            int ret_pointer = in_pointer;
            in_pointer = (in_pointer + 1) % ring_bus_size;
            return ret_pointer;


        }
    }
}
