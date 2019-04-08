using System;
using SME;
using SME.Components;

#define PRINT(x) Console.WriteLine(x)


namespace TCPIP
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            PRINT("Preprocessor running");
            using (var sim = new Simulation())
            {
                var mem = new TrueDualPortMemory<byte>(8192);
                var simulator = new FileInputSimulator("data/dump1/", mem.ControlA);
                var network = new Network(simulator.frameBus, mem.ControlA);
                var internet = new Internet(network.datagramBus,
                                            mem.ControlA);
                /*
                var simulator = new MemoryFileSimulatior<byte>("data/dump25/00000packet.bin");
                var network = new Network(simulator.frameBus,
                                        simulator.GetReadResultB(),
                                        simulator.GetControlB(),
                                        simulator.networkStatusBus);

                var internet = new Internet(network.datagramBus);
                var transport = new TTransport(internet.segmentBus);
                */

                // Use fluent syntax to configure the simulator.
                // The order does not matter, but `Run()` must be
                // the last method called.

                // The top-level input and outputs are exposed
                // for interfacing with other VHDL code or board pins

                sim
                    .AddTopLevelOutputs(internet.segmentBus)
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