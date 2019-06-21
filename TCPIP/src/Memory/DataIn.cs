using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    [ClockedProcess]
    public partial class DataIn : SimpleProcess
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


        ////////// Data_in from T process
        [InputBus]
        public WriteBus dataIn;
        [InputBus]
        public ComputeProducerControlBus dataInComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus dataInComputeConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();


        //////////// Data_in to interface
        //[OutputBus]
        public ReadBus dataOut = Scope.CreateBus<ReadBus>();
        [OutputBus]
        public BufferProducerControlBus dataOutBufferProducerControlBusOut = Scope.CreateBus<BufferProducerControlBus>();
        [InputBus]
        public ConsumerControlBus dataOutBufferConsumerControlBusIn;


        private MultiMemorySegmentsRingBufferFIFO<int> mem_calc;
        private readonly int mem_calc_num_segments = 10;


        // The table for tcp lookups
        private const int tcp_seq_lookup_size = 100;
        private int[] tcp_seq_lookup = new int[tcp_seq_lookup_size];

        private DictionaryListSparseLinked dict;

        private int cur_write_socket = int.MaxValue;
        private uint cur_write_tcp_seq = int.MaxValue;
        private int cur_write_block_id = int.MaxValue;

        private int cur_read_socket = int.MaxValue;
        private uint cur_read_tcp_seq = int.MaxValue;
        private int cur_read_block_id = int.MaxValue;


        // Indicators for clock offsets when reading from memory
        private bool send_requested = false;
        private bool send_receiving = false;
        private bool send_last_byte_requested = false;


        public DataIn(TrueDualPortMemory<byte> memory, int memory_size){
            // Set up the header information
            this.memory = memory;
            this.memory_size = memory_size;
            this.controlB = memory.ControlB;
            this.controlA = memory.ControlA;
            this.readResultA = memory.ReadResultA;
            this.readResultB = memory.ReadResultB;
            this.mem_calc = new MultiMemorySegmentsRingBufferFIFO<int>(mem_calc_num_segments,memory_size);
            // XXX better magic numbers
            this.dict = new DictionaryListSparseLinked(10,100);

        }
        protected override void OnTick()
        {
            // Send out new packs to interface
            Send();
            // Write to memory what we got from T
            Write();
        }

        private void Write(){
            // Disable the write bus, enable if there is stuff in the packet
            controlA.Enabled = false;
            // XXX datain ready should be stopped when there is no more good data
            dataInComputeConsumerControlBusOut.ready = true;
            // Data on the bus is currently valid


            if(dataInComputeProducerControlBusIn.valid){
                // This is a new packet
                if(dataIn.socket != cur_write_socket || dataIn.tcp_seq != cur_write_tcp_seq)
                {
                    // Set the current socket and tcp_seq
                    cur_write_socket = dataIn.socket;
                    cur_write_tcp_seq = dataIn.tcp_seq;

                    // get the new write id
                    cur_write_block_id = mem_calc.AllocateSegment(dataIn.data_length);
                    // Check if we need to create key, if so make it
                    if(!this.dict.ContainsKey((int)cur_write_socket))
                    {
                        this.dict.New((int)cur_write_socket);
                    }
                    // This gets the correct address for the current socket and tec_Seq, and saves
                    // the cur write block to that, so we can iterate over it when sending it to the user
                    tcp_seq_lookup[this.dict.Insert((int)cur_write_socket,(int)cur_write_tcp_seq)] = cur_write_block_id;
                }
                // Submit the data
                controlA.Enabled = true;
                controlA.IsWriting = true;
                controlA.Address = mem_calc.SaveData(cur_write_block_id);
                controlA.Data = dataIn.data;
                /// XXX reset if it is not valid any more
            }
        }
        private void Send()
        {
            controlB.Enabled = false;

            // If the current key does not exist, Find a new one
            if(!this.dict.ContainsKey((int)cur_read_socket)){
                // Get the first existing key
                int tmp = this.dict.GetFirstKey();
                cur_read_socket = tmp;
                // If there are no first keys, indicate that no data is avaliable and loop
                if(tmp == -1)
                {
                    //dataOutBufferProducerControlBusOut.available = false;
                    dataOutBufferProducerControlBusOut.valid = false;
                    return;
                }
            }

            // if the list in the dict have 0 elements, delete the key and return
            if(this.dict.ListLength((int)cur_read_socket) >= 0)
            {
                this.dict.Free((int)cur_read_socket);
                // Indicate that there are no avaliable data,
                //dataOutBufferProducerControlBusOut.available = false;
                dataOutBufferProducerControlBusOut.valid = false;
                return;
            }

            // We now know that we have an socket ready, with at least one element
            // Get What segment to focus on
            cur_read_block_id = tcp_seq_lookup[dict.GetFirstValue((int)cur_read_socket)];

            // If we are to receive stuff, but the request are false
            // we can assume that we need to roll back the last counter by one
            if(send_receiving && !send_requested && !send_last_byte_requested){
                mem_calc.SegmentRollback(cur_read_block_id);
            }

            // We are now receiving stuff from memory, send to the consumer
            // If we are not, say to T that the data is not valid
            if(send_receiving){
                dataOutBufferProducerControlBusOut.valid = true;
                // XXX id_send can change to different segment that what we got from ram
                dataOutBufferProducerControlBusOut.bytes_left = (uint)mem_calc.SegmentBytesLeft(cur_read_block_id);
                dataOut.data = readResultB.Data;

                // We reset receiving, since it needs to be set implicit
                send_receiving = false;
            }else{
                dataOutBufferProducerControlBusOut.valid = false;
            }

            // If the last clock set the request to true, we must be
            // receiving in the next, therefore set the send_receiving to true
            // The request are set to false, since request must be set implicit
            if(send_requested){
                send_requested = false;
                send_receiving = true;
            }

            // We have a full segment ready, we can send it
            if (mem_calc.IsSegmentFull(cur_read_block_id)){
                //dataOutBufferProducerControlBusOut.available = true;
            }else{
                //dataOutBufferProducerControlBusOut.available = false;
            }

            // The consumer are ready, ask memory and mark that we requested memory
            if(dataOutBufferConsumerControlBusIn.ready){
                controlB.Enabled = true;
                controlB.IsWriting = false;
                controlB.Address = mem_calc.LoadData(cur_read_block_id);
                send_requested = true;
                // If we get a request for the last byte, we do not roll back
                send_last_byte_requested = mem_calc.IsSegmentDone(cur_read_block_id);
            }
        }
    }
}
