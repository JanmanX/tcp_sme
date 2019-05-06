using System;
using System.IO;
using System.Threading.Tasks;

using SME;
using SME.Components;

namespace TCPIP
{
    public class FrameInputSimulator : SimulationProcess
    {
        [OutputBus]
        public readonly Network.FrameBusIn frameBus = Scope.CreateBus<Network.FrameBusIn>();

        [OutputBus]
        public readonly TrueDualPortMemory<byte>.IControlA controlA;


        // Simulation fields
        private readonly String dir;


        public FrameInputSimulator(String dir, TrueDualPortMemory<byte>.IControlA controlA)
        {
            this.dir = dir;
            this.controlA = controlA;
        }

        public override async Task Run()
        {
            // Wait for the initial reset to propagate
            await ClockAsync();

            // Initial setup 
            controlA.Enabled = true;
            controlA.IsWriting = true;

            // Initial values
            int addr = 0x00;
            uint frame_number = 0x00;

            string[] files = Directory.GetFiles(dir);
            foreach (var file in files)
            {
                byte[] bytes = File.ReadAllBytes(file);

                foreach (byte b in bytes)
                {
                    // Send to memory 
                    controlA.Address = addr;
                    controlA.Data = b;

                    // Send to Network
                    frameBus.frame_number = frame_number;

                    await ClockAsync();
                    addr++;
                }

                // Next packet
                frame_number++;
            }

            Console.WriteLine($"{frame_number} packets sent.");
        }
    }
}
