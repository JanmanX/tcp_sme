using System;
using System.IO;
using System.Threading.Tasks;

using SME;
using SME.Components;

namespace TCPIP
{
    public class FileInputSimulator : SimulationProcess
    {
        [OutputBus]
        public readonly Network.FrameBus frameBus = Scope.CreateBus<Network.FrameBus>();

        [OutputBus]
        public readonly TrueDualPortMemory<byte>.IControlA controlA;


        // Simulation fields
        private readonly String dir;


        public FileInputSimulator(String dir, TrueDualPortMemory<byte>.IControlA controlA)
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

                // Announce new packet
                frameBus.number = frame_number;

                foreach (byte b in bytes)
                {
                    controlA.Address = addr;
                    controlA.Data = b;
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
