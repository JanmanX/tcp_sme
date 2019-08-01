using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    [ClockedProcess]
    public partial class DataOut : SimpleProcess
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


        ////////// Data_out in from Interface
        [InputBus]
        public WriteBus dataIn;
        [InputBus]
        public ComputeProducerControlBus dataInComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus dataInComputeConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();


        //////////// Data_out out to T
        [OutputBus]
        public ReadBus dataOut = Scope.CreateBus<ReadBus>();
        [OutputBus]
        public BufferProducerControlBus dataOutBufferProducerControlBusOut = Scope.CreateBus<BufferProducerControlBus>();
        [InputBus]
        public ConsumerControlBus dataOutBufferConsumerControlBusIn;


        public struct TempData{
            public int socket;
            public ushort accum_len; // Accumulator for the length.
            public long frame_number; // Increments so we can distinguish between new packages
        }
        private TempData tmp_write_info;
        private TempData tmp_send_info;

        private bool write_next_packet = true;

        private SingleMemorySegmentsRingBufferFIFO<TempData> mem_calc;
        private readonly int mem_calc_num_segments = 50;


        private bool memory_requested = false;
        private bool memory_receiving = false;
        private bool send_preload = true;

         // little ringbuffer for the data out
        public struct SendRingBuffer{
            public byte data;
            public ushort length;
        }
        SendRingBuffer tempSendRingBuffer;
        private const int send_buffer_size = 4;
        private SendRingBuffer[] send_buffer = new SendRingBuffer[send_buffer_size];

        private SingleMemorySegmentsRingBufferFIFO<TempData> buffer_calc;


        public DataOut(TrueDualPortMemory<byte> memory, int memory_size){
            // Set up the header information
            this.memory = memory;
            this.memory_size = memory_size;
            this.controlB = memory.ControlB;
            this.controlA = memory.ControlA;
            this.readResultA = memory.ReadResultA;
            this.readResultB = memory.ReadResultB;
            this.mem_calc = new SingleMemorySegmentsRingBufferFIFO<TempData>(mem_calc_num_segments,memory_size);
            this.buffer_calc = new SingleMemorySegmentsRingBufferFIFO<TempData>(send_buffer_size,send_buffer_size);

        }

        protected override void OnTick()
        {
            Write();
            Send();
        }
        private void Write()
        {
            // Disable the write bus, enable if there is stuff in the packet
            controlA.Enabled = false;
            Logging.log.Trace($"ready: {dataInComputeProducerControlBusIn.valid} data: 0x{dataIn.data:X2}");
            // If the data is valid, we write it
            if(dataInComputeProducerControlBusIn.valid){
                // This is a new packet
                if(write_next_packet)
                {
                    tmp_write_info.socket = dataIn.socket;
                    tmp_write_info.frame_number = dataIn.frame_number;

                    mem_calc.NextSegment(tmp_write_info);
                    write_next_packet = false;
                    Logging.log.Info($"New packet! frame_number: {dataIn.frame_number} " +
                                     $"socket: {dataIn.socket} ");
                }
                // Submit the data
                controlA.Enabled = true;
                controlA.IsWriting = true;
                int addr = mem_calc.SaveData();
                controlA.Address = addr;
                Logging.log.Info($"Receiving: data: 0x{dataIn.data:X2} "+
                                  $"addr: {addr}");
                controlA.Data = dataIn.data;
            }else
            {
                if(write_next_packet == false)
                {
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

                    tmp_send_info = mem_calc.MetadataCurrentLoadSegment();

                    tmp_send_info.accum_len = (ushort)mem_calc.LoadDataBytesLeft();
                    if(tmp_send_info.accum_len == 0)
                    {
                        mem_calc.FinishReadingCurrentLoadSegment();
                    }

                    // We save the metadata onto the buffer
                    buffer_calc.NextSegment(tmp_send_info);
                }
            }

            ///////////// Sending code
            // They are ready, we submit stuff

            Logging.log.Info($"The load segment status: {buffer_calc.LoadSegmentReady()} ready: {dataOutBufferConsumerControlBusIn.ready}");


            dataOutBufferProducerControlBusOut.valid = buffer_calc.LoadSegmentReady();

            if(dataOutBufferConsumerControlBusIn.ready){
                send_preload = true;
            }

            if(send_preload && buffer_calc.LoadSegmentReady())
            {
                int addr = buffer_calc.LoadData();
                byte data = send_buffer[addr].data;
                int socket = buffer_calc.MetadataCurrentLoadSegment().socket;
                dataOut.data = data;
                dataOut.socket = socket;



                dataOutBufferProducerControlBusOut.valid = true;
                dataOutBufferProducerControlBusOut.bytes_left = send_buffer[addr].length;

                buffer_calc.FinishReadingCurrentLoadSegment();

                send_preload = false;

                Logging.log.Info($"Sending: data: 0x{data:X2} buffer_addr: {addr} socket: {socket}");
            }
        }
    }
}
