using System;
using SME;
using SME.Components;

namespace TCPIP
{
    public class DataInPrinter: SimpleProcess
    {
        [InputBus]
        public DataIn.WriteBus dataIn;
        [InputBus]
        public ComputeProducerControlBus dataInComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus dataInComputeConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();


        byte[] buffer = new byte[256];
        uint idx = 0;

        protected override void OnTick()
        {
            dataInComputeConsumerControlBusOut.ready = true;

            if(dataInComputeProducerControlBusIn.valid && idx < buffer.Length)
            {
                buffer[idx++] = dataIn.data;

                if (dataInComputeProducerControlBusIn.bytes_left == 0)
                {
                    for (int i = 0; i < idx; i++)
                    {
                        Console.Write($"0x{buffer[i]:X} ");
                    }
                    Console.WriteLine();
                    idx = 0;
                }

            }

        }
    }
}