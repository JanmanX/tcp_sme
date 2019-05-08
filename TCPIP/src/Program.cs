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

                var mem = new TrueDualPortMemory<byte>(8192);
                var simulator = new DatagramInputSimulator("data/dump5_datagrams/");
                // var simulator = new TUNSimulator();
                //                var network = new NetworkReader(simulator.frameBus);
                var internet = new InternetIn(simulator.datagramBusIn);
                simulator.datagramBusInControl = internet.datagramBusInControl;

                var transport = new Transport(internet.segmentBusIn);

                // Use fluent syntax to configure the simulator.
                // The order does not matter, but `Run()` must be 
                // the last method called.

                // The top-level input and outputs are exposed
                // for interfacing with other VHDL code or board pins

                sim
                    .AddTopLevelOutputs(transport.networkDataBufferBus)
                    .AddTopLevelInputs(simulator.datagramBusIn)
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();

                // After `Run()` has been invoked the folder
                // `output/vhdl` contains a Makefile that can
                // be used for testing the generated design
            }
        }
    }

}
