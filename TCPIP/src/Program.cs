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
                var simulator = new FileInputSimulator("data/dump25/", mem.ControlA);
                var network = new NetworkReader(simulator.frameBus, mem.ControlA);
                var internet = new InternetReader(network.datagramBus,
                                            mem.ControlA);
                var transport = new Transport(internet.segmentBus);

                // Use fluent syntax to configure the simulator.
                // The order does not matter, but `Run()` must be 
                // the last method called.

                // The top-level input and outputs are exposed
                // for interfacing with other VHDL code or board pins

                sim
                    //                    .AddTopLevelOutputs(transport)
                    .AddTopLevelInputs(simulator.frameBus)
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
