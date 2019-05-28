using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;

using SME;
using SME.Components;

namespace TCPIP
{
    public class PacketInSimulator : SimulationProcess
    {
        [OutputBus]
        public readonly PacketIn.PacketInBus packetInBus = Scope.CreateBus<PacketIn.PacketInBus>();

        [OutputBus]
        public readonly BufferProducerControlBus bufferProducerControlBus = Scope.CreateBus<BufferProducerControlBus>();

        // Simulation fields
        private readonly String dir;

        public PacketInSimulator(String dir)
        {
            this.dir = dir;
        }

        public override async Task Run()
        {
            // Init
            await ClockAsync();


            int frame_number = 0;
            uint ip_id = 1;
            string[] files = Directory.GetFiles(dir);
            Array.Sort(files);

            foreach (var file in files)
            {
                byte[] bytes = File.ReadAllBytes(file);
                Console.WriteLine(Encoding.Default.GetString(bytes));
                Console.WriteLine($"Sending packet {bytes[0]}{bytes[1]}{bytes[2]}{bytes[3]}");
                uint bytes_left = (uint)bytes.Length;
                foreach (byte b in bytes)
                {
                    // Control bus
                    bufferProducerControlBus.bytes_left = bytes_left--;
                    bufferProducerControlBus.available = true;
                    bufferProducerControlBus.valid = true;

                    // Data bus
                    packetInBus.frame_number = frame_number;
                    packetInBus.ip_id = ip_id;
                    packetInBus.protocol = (byte)IPv4.Protocol.UDP;
                    packetInBus.data = b;

                    await ClockAsync();
                }

                ip_id++;
            }
            await ClockAsync();
        }
    }
}
