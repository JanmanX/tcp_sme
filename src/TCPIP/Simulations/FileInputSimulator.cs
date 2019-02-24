using SME;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TCPIP
{
    /// <summary>
    /// Helper process that loads images and writes them into the simulation.
    /// Since this is a simulation process, it will not be rendered as hardware
    /// and we can use any code and dynamic properties we want
    /// </summary>
    public class FileInputSimulator : SimulationProcess
    {
        /// <summary>
        /// The camera connection bus
        /// </summary>
        [OutputBus]
        public readonly Network.FrameBus frameBus = Scope.CreateBus<Network.FrameBus>();

        private readonly String file;

        public FileInputSimulator(String file)
        {
            this.file = file;
        }

        /// <summary>
        /// Run this instance.
        /// </summary>
        public override async Task Run()
        {
            // Wait for the initial reset to propagate
            await ClockAsync();

            if (!System.IO.File.Exists(file))
            {
                Console.WriteLine($"File not found: {file}");
                await ClockAsync();
                return;
            }


            byte[] bytes = File.ReadAllBytes(this.file);
            foreach (byte b in bytes)
            {
                frameBus.data = b;
                // Write progress after each line
                Console.WriteLine($"Written: {b}.");

                await ClockAsync();
            }
        }
    }
}
