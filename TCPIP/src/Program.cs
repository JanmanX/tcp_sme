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
                var _interface = new Interface();

                sim
                    .AddTopLevelOutputs(_interface.interfaceBusOut)
                    .AddTopLevelInputs(_interface.interfaceBus)
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
