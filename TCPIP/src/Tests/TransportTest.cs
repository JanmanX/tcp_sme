using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;

using SME;
using SME.Components;

namespace TCPIP
{
    public class TransportTest
    {
        public static bool Run()
        {
            using (var sim = new Simulation())
            {
                var simulator = new PacketInSimulator("data/transport/udp_25");
                var transport = new Transport();
                var dataIn = new DataInPrinter();
                var dataOut = new DataOutWriter();
                var packetOut = new PacketOutPrinter();

                // PacketIn
                simulator.consumerControlBus = transport.packetInBufferConsumerControlBusOut;
                transport.packetInBufferProducerControlBusIn = simulator.bufferProducerControlBus;
                transport.packetInBus = simulator.packetInBus;

                // DataIn
                transport.dataInComputeConsumerControlBusIn = dataIn.dataInComputeConsumerControlBusOut;
                dataIn.dataIn = transport.dataInWriteBus;
                dataIn.dataInComputeProducerControlBusIn = transport.dataInComputeProducerControlBusOut;

                // DataOut
                transport.dataOutReadBus = dataOut.dataOut;
                transport.dataOutBufferProducerControlBusIn = dataOut.bufferProducerControlBus;
                dataOut.consumerControlBus = transport.dataOutBufferConsumerControlBusOut;

                // PacketOut
                transport.packetOutComputeConsumerControlBusIn = packetOut.packetBusComputeConsumerControlBusOut;
                packetOut.packetBus = transport.packetOutWriteBus;
                packetOut.packetBusComputeProducerControlBusIn = transport.packetOutComputeProducerControlBusOut;

                // Interface
                //transport.interfaceBus = simulator.interfaceBus;
                //simulator.interfaceControlBus = transport.interfaceControlBus;


                sim.AddTopLevelInputs(simulator.packetInBus)
                    .BuildCSVFile()
                   // .BuildVHDL()
                    .Run();
           }
           return true;
        }
    }


}
