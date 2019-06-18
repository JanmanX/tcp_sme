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
        [InputBus]
        public ConsumerControlBus consumerControlBus;

        [OutputBus]
        public readonly PacketIn.ReadBus packetInBus = Scope.CreateBus<PacketIn.ReadBus>();

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
            await ClockAsync();
            await ClockAsync();
            await ClockAsync();

            //bufferProducerControlBus.available = true;


            int frame_number = 0;
            uint ip_id = 1;
            string[] files = Directory.GetFiles(dir);
            Array.Sort(files); // Are dot net developers even human?

            foreach (var file in files)
            {
                byte[] bytes = File.ReadAllBytes(file);
                uint bytes_left = (uint)bytes.Length;
                foreach (byte b in bytes)
                {
                    do {
                        // Control bus
                        bufferProducerControlBus.bytes_left = bytes_left--;
                        bufferProducerControlBus.valid = true;

                        packetInBus.frame_number = frame_number;
                        packetInBus.ip_id = ip_id;
                        packetInBus.protocol = (byte)IPv4.Protocol.UDP;
                        packetInBus.data = b;

                        await ClockAsync();
                    } while(consumerControlBus.ready == false); // resend previous byte if consumer is not ready this cycle
                }

                ip_id++;
            }
            await ClockAsync();
        }
    }
}
