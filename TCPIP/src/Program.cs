using System;
using SME;

namespace TCPIP
{
    class MainClass
    {
        public static void Main(string[] args)
        {

            using (var sim = new Simulation())
            {
                var simulator = new MemoryFileSimulatior("data/data0.bin");
//                var network = new Network();
//                var internet = new Internet(network.datagramBus);
//                var transport = new TTransport(internet.segmentBus);

                // Use fluent syntax to configure the simulator.
                // The order does not matter, but `Run()` must be 
                // the last method called.

                // The top-level input and outputs are exposed
                // for interfacing with other VHDL code or board pins

                sim
//                    .AddTopLevelOutputs(transport.outputBus)
//                    .AddTopLevelInputs(simulator.GetControlB)
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