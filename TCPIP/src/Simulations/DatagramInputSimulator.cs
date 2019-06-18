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

        [OutputBus]
        public readonly ComputeProducerControlBus computeProducerControlBus = Scope.CreateBus<ComputeProducerControlBus>();

        [InputBus]
        public ConsumerControlBus consumterControlBus;

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
                for(int i = 0; i < bytes.Length; i++)
                {
                    // Control
                    //computeProducerControlBus.available = true;
                    computeProducerControlBus.valid = true;
                    computeProducerControlBus.bytes_left = 1;
                    if(i-1 == bytes.Length) {
                        computeProducerControlBus.bytes_left = 0;
                    }

                    datagramBusIn.frame_number = frame_number;
                    datagramBusIn.type = (ushort)EthernetIIFrame.EtherType.IPv4; // XXX: Hardcoded
                    datagramBusIn.data = bytes[i];

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
