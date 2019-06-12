using System;
using SME;
using SME.Components;

namespace TCPIP
{
    public class PacketOutPrinter: SimpleProcess
    {
        [InputBus]
        public PacketOut.WriteBus packetBus;
        [InputBus]
        public ComputeProducerControlBus packetBusComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus packetBusComputeConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();

        byte[] buffer = new byte[256];
        uint idx = 0;
        ulong ip_addr = 0;

        protected override void OnTick()
        {
            return;
            packetBusComputeConsumerControlBusOut.ready = true;

            if(packetBusComputeProducerControlBusIn.valid && idx < buffer.Length)
            {
                buffer[idx++] = packetBus.data;
                ip_addr = packetBus.ip_dst_addr_0;
            }

            if(packetBusComputeProducerControlBusIn.bytes_left == 0)
            {

                Console.WriteLine($"Packet to {ip_addr}:");
                for(int i = 0; i < idx; i++) {
                    Console.Write($"0x{buffer[i]:X} ");
                }
                Console.WriteLine();
                idx = 0;
            }
        }
    }
}