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
        private readonly TrueDualPortMemory<byte>.IControlA controlA;

        [InputBus]
        private readonly TrueDualPortMemory<byte>.IReadResultA readResultA;

        [OutputBus]
        private readonly TrueDualPortMemory<byte>.IControlB controlB;

        [InputBus]
        private readonly TrueDualPortMemory<byte>.IReadResultB readResultB;
        private readonly int memory_size;


        ////////// Data_in from T process
        [InputBus]
        public WriteBus dataIn;
        [InputBus]
        public ComputeProducerControlBus dataInComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus dataInComputeConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();


        //////////// Data_in to interface
        [OutputBus]
        public ReadBus dataOut = Scope.CreateBus<ReadBus>();
        [OutputBus]
        public BufferProducerControlBus dataOutBufferProducerControlBusOut = Scope.CreateBus<BufferProducerControlBus>();
        [InputBus]
        public ConsumerControlBus dataOutBufferConsumerControlBusIn;



        public struct InputData{
            public int socket;
            public uint sequence;
            public bool invalidate;
            public int data_length;
            public ushort accum_len; // Accumulator for the length.
        }
        private InputData tmp_write_inputdata;
        private InputData tmp_send_inputdata;

        private MultiMemorySegmentsRingBufferFIFO<InputData> mem_calc;
        private readonly int mem_calc_num_segments = 10;


        // The table for tcp lookups
        private const int memory_lookup_size = 100;
        private int[] memory_lookup = new int[memory_lookup_size];


        public struct SequenceInfo{
            public int maximum_sequence;
        }
        private SequenceInfo tmp_sequenceinfo;
        private DictionaryListSparseLinked<SequenceInfo> sequence_dict;


        private int cur_write_socket = int.MaxValue;
        private uint cur_write_sequence = int.MaxValue;
        private int cur_write_block_id = int.MaxValue;


        private int cur_read_socket = int.MaxValue;
        private uint cur_read_tcp_seq = int.MaxValue;
        private int cur_read_block_id = int.MaxValue;


        // little ringbuffer for the data out
        public struct SendRingBuffer{
            public byte data;
            public ushort length;
        }
        SendRingBuffer tempSendRingBuffer;
        private const int send_buffer_size = 4;
        private SendRingBuffer[] send_buffer = new SendRingBuffer[send_buffer_size];
        private SingleMemorySegmentsRingBufferFIFO<InputData> buffer_calc;

        private bool memory_requested = false;
        private bool memory_receiving = false;

        private bool send_preload = true;


        public DataIn(TrueDualPortMemory<byte> memory, int memory_size){
            // Set up the header information
            this.memory = memory;
            this.memory_size = memory_size;
            this.controlB = memory.ControlB;
            this.controlA = memory.ControlA;
            this.readResultA = memory.ReadResultA;
            this.readResultB = memory.ReadResultB;
            this.mem_calc = new MultiMemorySegmentsRingBufferFIFO<InputData>(mem_calc_num_segments,memory_size);
            // XXX better magic numbers
            this.sequence_dict = new DictionaryListSparseLinked<SequenceInfo>(100,100);
            // Define the little ring buffer for the outgoing packets
            this.buffer_calc = new SingleMemorySegmentsRingBufferFIFO<InputData>(send_buffer_size,send_buffer_size);

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
                if(dataIn.socket != cur_write_socket ||
                   dataIn.sequence != cur_write_sequence)
                {
                    // Set the current socket, sequence and packet number
                    cur_write_socket = dataIn.socket;
                    cur_write_sequence = dataIn.sequence;

                    // get the new write id
                    cur_write_block_id = mem_calc.AllocateSegment(dataIn.data_length);

                    // Fist we test and fill the socket dict

                    // Check if we need to create key in the socket, if so make it
                    if(!this.sequence_dict.ContainsKey((int)cur_write_socket))
                    {
                        if(!this.sequence_dict.New((int)cur_write_socket))
                        {
                            Logging.log.Fatal($"Could not create new key for socket_dict key:{cur_write_socket}");
                            throw new Exception($"Could not create new key for socket_dict key:{cur_write_socket}");
                        }
                        // Set the sequence to zero
                        tmp_sequenceinfo.maximum_sequence = 0;
                        this.sequence_dict.SaveMetaData(cur_write_socket,tmp_sequenceinfo);
                    }

                    // We save information about the newly seen segment for that socket
                    int test = this.sequence_dict.Insert(cur_write_socket,(int)cur_write_sequence);
                    memory_lookup[test] = cur_write_block_id;

                    // Test if we have gotten a new sequence that is higher
                    tmp_sequenceinfo = this.sequence_dict.LoadMetaData(cur_write_socket);
                    if(tmp_sequenceinfo.maximum_sequence < dataIn.highest_sequence_ready){
                        tmp_sequenceinfo.maximum_sequence = (int)dataIn.highest_sequence_ready;
                        this.sequence_dict.SaveMetaData(cur_write_socket,tmp_sequenceinfo);
                    }

                    // We create the struct for the address block
                    tmp_write_inputdata.data_length = dataIn.data_length;
                    tmp_write_inputdata.invalidate = dataIn.invalidate;
                    tmp_write_inputdata.socket = dataIn.socket;
                    tmp_write_inputdata.sequence = dataIn.sequence;
                    tmp_write_inputdata.accum_len = (ushort)dataIn.data_length;
                    mem_calc.SaveMetaData(cur_write_block_id,tmp_write_inputdata);
                }
                // Submit the data
                controlA.Enabled = true;
                controlA.IsWriting = true;
                Logging.log.Warn($"Received data 0x{dataIn.data:X2} socket: {dataIn.socket} sequence number: {dataIn.sequence}");
                controlA.Address = mem_calc.SaveData(cur_write_block_id);
                controlA.Data = dataIn.data;
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
                tempSendRingBuffer.length = buffer_calc.MetadataCurrentLoadSegment().accum_len;
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
                bool invalid = false;
                int addr = -1;
                int socket = -1;
                int sequence = -1;
                // Get the current focus element, and test if it is ready
                int focused_memory_block = mem_calc.FocusSegment();
                if(mem_calc.IsSegmentDone(focused_memory_block)){
                    Logging.log.Info($"memory segment is not ready, waiting. memory segment: {focused_memory_block}");
                    invalid = true;
                }

                if(!invalid)
                {
                    // Get the socket and the sequence from the current memory block
                    socket = mem_calc.LoadMetaData(focused_memory_block).socket;
                    sequence = (int)mem_calc.LoadMetaData(focused_memory_block).sequence;
                    int maximum_sequence = sequence_dict.LoadMetaData(socket).maximum_sequence;

                    // Get the first sequence
                    int sequence_pointer = sequence_dict.GetFirstValue(socket);

                    int sequence_memory_block = memory_lookup[sequence_pointer];


                    // If this segment is not the first, push the current segment to top and try again
                    if (sequence_memory_block != focused_memory_block){
                        Logging.log.Info("Segment is not the last one");
                        mem_calc.DelaySegment(focused_memory_block);
                        invalid = true;
                    }

                    // If the sequence is bigger than the max, we should not use it, as it is not in order
                    if(sequence > maximum_sequence){
                        Logging.log.Info("Segment is in front of sequence, must be missing blocks");
                        mem_calc.DelaySegment(focused_memory_block);
                        invalid = true;
                    }
                }

                // Segment has been tested, and we can now load its data
                if(invalid){
                    addr = -1;
                }else{
                    addr = mem_calc.LoadData(focused_memory_block);
                }

                // If we actually can get the address(If buffer empty etc)
                if(addr != -1)
                {
                    Logging.log.Trace($"Requesting memory from addr: {addr} on segment: {focused_memory_block}");
                    // Request the data
                    controlB.Enabled = true;
                    controlB.IsWriting = false;
                    controlB.Address = addr;

                    // we now have an request from memory
                    memory_requested = true;

                    // Get the metadata from the memory calculator, subtract the total by one, and
                    // Push it into the segment. This makes it possible to detect the last byte in the loaded data
                    tmp_send_inputdata = mem_calc.LoadMetaData(focused_memory_block);
                    tmp_send_inputdata.accum_len -= 1;
                    mem_calc.SaveMetaData(focused_memory_block,tmp_send_inputdata);

                    // We save the metadata onto the buffer
                    buffer_calc.NextSegment(mem_calc.LoadMetaData(focused_memory_block));

                    // If the segment is fully loaded, we remove it from the sequence dict.
                    if(mem_calc.IsSegmentDone(focused_memory_block)){

                        sequence_dict.Delete(socket,sequence);
                    }
                }
            }

            ///////////// Sending code
            // They are ready, we submit stuff

            Logging.log.Trace($"The load segment status: {buffer_calc.LoadSegmentReady()} ready: {dataOutBufferConsumerControlBusIn.ready}");


            dataOutBufferProducerControlBusOut.valid = buffer_calc.LoadSegmentReady();

            if(dataOutBufferConsumerControlBusIn.ready){
                send_preload = true;
            }

            // Logging.log.Warn($"ready:{dataOutBufferConsumerControlBusIn.ready} " +
            //                  $"valid:{dataOutBufferProducerControlBusOut.valid} " +
            //                  $"send_preload:{send_preload}");

            if(send_preload && buffer_calc.LoadSegmentReady())
            {
                dataOut.socket = buffer_calc.MetadataCurrentLoadSegment().socket;
                long sequence = buffer_calc.MetadataCurrentLoadSegment().sequence;
                int addr = buffer_calc.LoadData();
                byte data = send_buffer[addr].data;
                dataOut.data = data;

                dataOutBufferProducerControlBusOut.valid = true;
                dataOutBufferProducerControlBusOut.bytes_left = send_buffer[addr].length;

                buffer_calc.FinishReadingCurrentLoadSegment();

                send_preload = false;

                Logging.log.Warn($"Sending(or preloading valid): data: 0x{data:X2} buffer_addr: {addr} sequence number: {sequence}");

            }
        }
    }
}
