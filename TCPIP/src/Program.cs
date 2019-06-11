using System;

using SME;
using SME.Components;

namespace TCPIP
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                // Notes:
                // * implement reverse load and save in the MemorySegmentsRingBufferFiFo
                // * Fix internet_out to use new standards
                // * Make the databuffers ready with 2 bytes(request-respond) so
                //   ready on consumers will give no latency
                // * Make a first in first out ring buffer for the packet out classes
                // * Use the frame number to distinguish between new packets, instead of

                var simulator = new UDPPingPong("data/transport/udp_25");
                var transport = new Transport();

                // PacketIn
                simulator.packetInConsumerControlBus = transport.packetInBufferConsumerControlBusOut;
                transport.packetInBus = simulator.packetInBus;
                transport.packetInBufferProducerControlBusIn = simulator.packetInBufferProducerControlBus;

                // DataIn
                transport.dataInComputeConsumerControlBusIn = simulator.dataInConsumerControlBus;
                simulator.dataInWriteBus = transport.dataInWriteBus;
                simulator.dataInComputeProducerControlBus = transport.dataInComputeProducerControlBusOut;

                // DataOut
                transport.dataOutReadBus = transport.dataOutReadBus;
                transport.dataOutBufferProducerControlBusIn = simulator.dataOutBufferProducerControlBus;
                simulator.dataOutConsumerControlBus = transport.dataOutBufferConsumerControlBusOut;

                // PacketOut
                simulator.packetOutComputeProducerControlBusOut = simulator.packetOutComputeProducerControlBusOut;
                simulator.packetOutWriteBus = simulator.packetOutWriteBus;
                transport.packetOutComputeConsumerControlBusIn = simulator.packetOutComputeConsumerControlBusIn;

                // Interface
                transport.interfaceBus = simulator.interfaceBus;
                simulator.interfaceControlBus = transport.interfaceControlBus;








               // Use fluent syntax to configure the simulator.
                // The order does not matter, but `Run()` must be
                // the last method called.

                // The top-level input and outputs are exposed
                // for interfacing with other VHDL code or board pins

                sim.AddTopLevelInputs(simulator.dataInWriteBus)
                    .BuildCSVFile()
                    //.BuildVHDL()
                    .Run();

                // After `Run()` has been invoked the folder
                // `output/vhdl` contains a Makefile that can
                // be used for testing the generated design
            }
        }
    }

}
