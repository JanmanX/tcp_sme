using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;

using SME;
using SME.Components;

namespace TCPIP
{
    public class InterfaceSimulator : SimulationProcess
    {
        public InterfaceSimulator(String dir)
        {
        }

        public override async Task Run()
        {
            // Init
            await ClockAsync();
            await ClockAsync();
            await ClockAsync();
            await ClockAsync();
        }
    }
}
