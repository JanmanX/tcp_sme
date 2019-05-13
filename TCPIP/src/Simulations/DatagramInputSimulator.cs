using System;
using System.IO;
using System.Threading.Tasks;

using SME;
using SME.Components;

namespace TCPIP
{
    public class DatagramInputSimulator : SimulationProcess
    {
        [OutputBus]
        public readonly Internet.DatagramBusIn datagramBusIn = Scope.CreateBus<Internet.DatagramBusIn>();

        [InputBus]
        public Internet.DatagramBusInControl datagramBusInControl;

        // Simulation fields
        private readonly String dir;


        public DatagramInputSimulator(String dir)
        {
            this.dir = dir;
        }

        public override async Task Run()
        {
            // Init
            await ClockAsync();


            uint frame_number = 0;
            string[] files = Directory.GetFiles(dir);
            Array.Sort(files);

            foreach (var file in files)
            {
                byte[] bytes = File.ReadAllBytes(file);

                Console.WriteLine($"Writing new frame from {file}, len: {bytes.Length}");
                foreach (byte b in bytes)
                {
                    if (datagramBusInControl.skip)
                    {
                        Console.WriteLine("SKIP");
                        break; // Breaks into the next file
                    }
                    if (datagramBusInControl.ready)
                    {
                        datagramBusIn.frame_number = frame_number;
                        datagramBusIn.type = (ushort)EtherType.IPv4; // XXX: Hardcoded
                        datagramBusIn.data = b;
                    }

                    await ClockAsync();
                }

                // Next packet
                frame_number++;
            }

            await ClockAsync();
            Console.WriteLine($"{frame_number} packets sent.");
        }
    }
}