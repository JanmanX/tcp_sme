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
        public struct RingEntry{
            public int start; // Include bit
            public int stop; // exclude last bit
            public int current; // The current bit being written
            public bool done; // Mark if we are done with all data, and memory transactions are done
            public bool to_transmit; // If the data are currently being transmitted to next level
            public bool transmitting; // If the data are currently being transmitted to next level

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
        private readonly SME.Components.TrueDualPortMemory<byte>.IReadResultA readA;

        [OutputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IControlB controlB;

        [InputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IReadResultB readB;
        private readonly int memory_size;

        [InputBus]
        public readonly PacketOut.BufferIn buffer_in_internet = Scope.CreateBus<PacketOut.BufferIn>();
        [InputBus]
        public readonly PacketOut.BufferIn buffer_in_transport = Scope.CreateBus<PacketOut.BufferIn>();
        [OutputBus]
        public readonly PacketOut.BufferOut buffer_out = Scope.CreateBus<PacketOut.BufferOut>();

        private const short ring_bus_size = 10;
        private RingEntry[] ring_bus = new RingEntry[ring_bus_size];





        public PacketOut(TrueDualPortMemory<byte> memory, int memory_size){
            // Set up the header information
            this.memory = memory;
            this.memory_size = memory_size;
            this.controlB = memory.ControlB;
            this.controlA = memory.ControlA;
            this.readA = memory.ReadResultA;
            this.readB = memory.ReadResultB;
        }

        protected override void OnTick()
        {
            // Read the data from the busses
            
            //ReadTransport();
            ReadInternet();
            //LOGGER.ERROR($"{range_internet.done}");
            
            Write();
        }

        // Read from the data input, use the memorybusA for now, possibly add 3 busses for better interaction?
        private void ReadInternet(){
            if (buffer_in_internet.active)
            {
                RingEntry cur_range = ring_bus[internet_pointer];
                // If it is a new packet, add it to the pool, else append to current transaction
                if(buffer_in_internet.number != internet_number)
                {
                    // Mark the current range as done, so we can transmit it further
                    cur_range.done = true;
                    cur_range.current = 0;
                    internet_pointer = AddRingEntry(buffer_in_internet.total_len);  
                    internet_number = buffer_in_internet.number;    
                }
                SaveData(buffer_in_internet.data,MemoryTypeManager.Internet,internet_pointer);
                
                LOGGER.INFO($"{cur_range.current} == {buffer_in_internet.total_len}");
                // If the current pointer is on the total length, we are done(since we now point one off in memory)
                if(cur_range.current == buffer_in_internet.total_len - 1)
                {   
                    LOGGER.WARN("Marking as done");
                    cur_range.done = true;
                    cur_range.current = 0;
                    ring_bus[internet_pointer] = cur_range;
                }
            }
        }

        
        private void Write(){
            // XXX Use another memory manager, so we can have controlC?
            RingEntry range = ring_bus[out_pointer];
            int address = (range.start + range.current) % memory_size;
            // If the range is now being transmitted out, we can read from the memory
            if(range.transmitting){
                //LOGGER.ERROR("range transmitting");
                // The bus out is now active, and data is ready to be received;
                //buffer_out.active = true;
                buffer_out.data = readB.Data;
                LOGGER.WARN($"Transmitting out data {buffer_out.data:X2}");
            }
            //LOGGER.DEBUG($"done? {range.done}");
            // request an address from the memory, and send it next clock
            if(range.done){
                
                // If we are out of bounds, we stop, and go to next packet
                if(address == range.stop){
                    LOGGER.ERROR("range stop");
                    range.done = false;
                    range.to_transmit = false;
                    range.current = 0;
                    out_pointer = (out_pointer + 1) % ring_bus_size;
                }
                // else we request the memory, and set the current byte to the next
                else
                {
                    LOGGER.ERROR($"range set  control:{address} {range.start} {range.stop} {range.current}");
                    if(range.to_transmit){
                        range.transmitting = true;
                    }
                    controlB.Enabled = true;
                    controlB.Address = address;
                    controlB.IsWriting = false;
                    range.done = true;
                    range.to_transmit = true;
                    range.current++;
                }
                
            }
            ring_bus[out_pointer] = range;
        }


        private void SaveData(byte data, MemoryTypeManager type, int cur_pointer){
            RingEntry range = ring_bus[cur_pointer];
            int address = (range.start + range.current) % memory_size;
            switch (type)
            {
                case MemoryTypeManager.Internet:
                    controlA.Enabled = true;
                    controlA.Address = address;
                    controlA.Data = data;
                    LOGGER.WARN($"Saving on addr {address} value {data:X2}");
                    controlA.IsWriting = true;
                    break;
                case MemoryTypeManager.Transport:
                    controlB.Enabled = true;
                    controlB.Address = address;
                    controlB.Data = data;
                    controlB.IsWriting = true;
                    break;
                default:
                    LOGGER.ERROR($"Current type {type} not defined as an savable type!");
                    break;
            }
            range.current++;
            ring_bus[cur_pointer] = range;
        }

        private int AddRingEntry(int size){
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
            range.done = false;
            range.to_transmit = false;
            
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
