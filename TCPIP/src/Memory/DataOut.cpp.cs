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


        ////////// Packet in from Interface
        [InputBus]
        public Memory.InternetPacketBus dataIn;
        [InputBus]
        public ComputeProducerControlBus dataInComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus dataInComputeConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();


        //////////// Packet out to T
        [OutputBus]
        public Memory.InternetPacketBus dataOut = Scope.CreateBus<Memory.InternetPacketBus>();
        [OutputBus]
        public ComputeProducerControlBus dataOutComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [InputBus]
        public ConsumerControlBus dataOutComputeConsumerControlBusIn;


        public struct TempData{
            public byte xxx;
        }
        private TempData tmp_ip_info;



        private MemorySegmentsRingBufferFIFO<TempData> mem_calc;
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
            this.mem_calc = new MemorySegmentsRingBufferFIFO<TempData>(mem_calc_num_segments,memory_size);

        }
    }
}
