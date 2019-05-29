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

                //var mem = new TrueDualPortMemory<byte>(8192);
                //var simulator = new DatagramInputSimulator("data/udp_25/");
                // var simulator = new TUNSimulator();
                //                var network = new NetworkReader(simulator.frameBus);
                //var internet = new InternetIn(simulator.datagramBusIn);
                //simulator.datagramBusInControl = internet.datagramBusInControl;

                //var transport = new Transport(internet.segmentBusIn);
                //var dataInReader = new DataInReader(transport.dataInBus);


                // Graph simulator
                // int packet_out_mem_size = 8192;
                // var packet_out_mem = new TrueDualPortMemory<byte>(packet_out_mem_size);
                //var packet_out = new PacketOut(packet_out_mem,packet_out_mem_size);
                PacketInSimulator simulator = new PacketInSimulator("data/transport/udp_25/");
                // var internetIn = new InternetIn(simulator.datagramBusIn,packet_out.bus_in_internet);
                // var internetOut = new InternetOut(simulator.datagramBusOut,
                //                                   packet_out.bus_out,
                //                                   packet_out.bus_out_control);
                var transport = new Transport();
                transport.packetInProducerControlBus = simulator.bufferProducerControlBus;
                transport.packetInBus = simulator.packetInBus;

                simulator.consumerControlBus = transport.packetInConsumerControlBus;

                var p = new PrinterProcess(transport.dataInWriteBus, transport.dataInProducerControlBus);
                transport.dataInConsumerControlBus = p.consumerControlBus;

                // Use fluent syntax to configure the simulator.
                // The order does not matter, but `Run()` must be
                // the last method called.

                // The top-level input and outputs are exposed
                // for interfacing with other VHDL code or board pins

                sim
                    .AddTopLevelOutputs(transport.dataInWriteBus)
                    .AddTopLevelInputs(simulator.packetInBus)
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
