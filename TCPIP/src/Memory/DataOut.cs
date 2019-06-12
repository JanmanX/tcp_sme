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
        private readonly SME.Components.TrueDualPortMemory<byte>.IControlA controlA;

        [InputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IReadResultA readResultA;

        [OutputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IControlB controlB;

        [InputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IReadResultB readResultB;
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
            public byte xxx;
        }
        private TempData tmp_ip_info;



        private MultiMemorySegmentsRingBufferFIFO<TempData> mem_calc;
        private readonly int mem_calc_num_segments = 10;

        protected override void OnTick()
        {
            throw new NotImplementedException();
        }

        public DataOut(TrueDualPortMemory<byte> memory, int memory_size){
            // Set up the header information
            this.memory = memory;
            this.memory_size = memory_size;
            this.controlB = memory.ControlB;
            this.controlA = memory.ControlA;
            this.readResultA = memory.ReadResultA;
            this.readResultB = memory.ReadResultB;
            this.mem_calc = new MultiMemorySegmentsRingBufferFIFO<TempData>(mem_calc_num_segments,memory_size);

        }
    }
}
