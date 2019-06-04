using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    public partial class PrinterProcess : SimpleProcess
    {
        [InputBus]
        public ComputeProducerControlBus computeProducerControlBus;
        [InputBus]
        public PacketOut.PacketOutWriteBus bus;

        [OutputBus]
        public readonly ConsumerControlBus consumerControlBus = Scope.CreateBus<ConsumerControlBus>();

        private byte[] buffer = new byte[8192];
        uint length = 0;

        public PrinterProcess()
        {
        }

        protected override void OnTick()
        {
            consumerControlBus.ready = true;


            if (computeProducerControlBus.valid)
            {
                buffer[bus.addr] = bus.data;
                length++;

                if (computeProducerControlBus.bytes_left == 0)
                {
                    for (int i = 0; i < length; i = i + 2)
                    {
                        Console.Write($"0x{buffer[i]:X2}{buffer[i+1]:X2}+");
                        buffer[i] = 0x00;
                        buffer[i+1] = 0x00;
                    }
                    Console.WriteLine();

                    length = 0; 
                }
            }
        }
    }

}