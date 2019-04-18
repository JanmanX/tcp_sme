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
        private const int RAW_PACKET_OFFSET = 0x04;

        [OutputBus]
        public readonly Internet.DatagramBusIn datagramBusIn = Scope.CreateBus<Internet.DatagramBusIn>();

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


                for (int i = RAW_PACKET_OFFSET; i < buffer.Length; i++)
                {
                    datagramBusIn.frame_number = frame_number;
                    datagramBusIn.type = proto;
                    datagramBusIn.data = buffer[i];

                    await ClockAsync();
                }

                frame_number++;
            }

        }
    }
}
