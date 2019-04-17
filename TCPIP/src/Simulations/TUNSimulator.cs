using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text;

using SME;
using SME.Components;

namespace TCPIP
{
    public class TUNSimulator : SimulationProcess
    {
        // static
        private const string PIPE_NAME = "/tmp/tun_sme_pipe";
        private const int PORT = 8888;

        [OutputBus]
        public readonly Internet.DatagramBus datagramBus = Scope.CreateBus<Internet.DatagramBus>();

        uint frame_number = 0;

        private const int BUFFER_SIZE = 1024;
        byte[] buffer = new byte[BUFFER_SIZE];

        UdpClient udpClient;
        IPEndPoint localEndPoint;

        public TUNSimulator()
        {
            Console.WriteLine("=== TUN SIMULATOR ===");

            udpClient = new UdpClient(PORT);
            localEndPoint = new IPEndPoint(IPAddress.Any, PORT);
        }

        public override async Task Run()
        {
            // Init
            await ClockAsync();

            while (true)
            {
                do
                {
                    buffer = udpClient.Receive(ref localEndPoint);
                } while (buffer.Length == 0);

                ushort proto = (ushort)(buffer[2] << 8 | buffer[3]);
                Console.WriteLine($"Proto: 0x{proto:X}");


                for (int i = 2; i < buffer.Length; i++)
                {
                    datagramBus.frame_number = frame_number;
                    datagramBus.type = proto;
                    datagramBus.data = buffer[i];

                    await ClockAsync();
                }

                frame_number++;
            }

        }
    }
}
