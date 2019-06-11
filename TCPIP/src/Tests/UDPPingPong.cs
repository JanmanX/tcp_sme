using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;

using SME;
using SME.Components;

namespace TCPIP
{
    public class UDPPingPong : SimulationProcess
    {
        // PacketIn
        [InputBus]
        public ConsumerControlBus packetInConsumerControlBus;
        [OutputBus]
        public readonly PacketIn.ReadBus packetInBus = Scope.CreateBus<PacketIn.ReadBus>();
        [OutputBus]
        public readonly BufferProducerControlBus packetInBufferProducerControlBus = Scope.CreateBus<BufferProducerControlBus>();

        // DataIn
        [InputBus]
        public ComputeProducerControlBus dataInComputeProducerControlBus;
        [InputBus]
        public DataIn.WriteBus dataInWriteBus;
        [OutputBus]
        public ConsumerControlBus dataInConsumerControlBus = Scope.CreateBus<ConsumerControlBus>();

        // DataOut
        [OutputBus]
        public BufferProducerControlBus dataOutBufferProducerControlBus = Scope.CreateBus<BufferProducerControlBus>();
        [OutputBus]
        public DataOut.ReadBus dataOutReadBus = Scope.CreateBus<DataOut.ReadBus>();
        [InputBus]
        public ConsumerControlBus dataOutConsumerControlBus;

        // PacketOut
        [InputBus]
        public ComputeProducerControlBus packetOutComputeProducerControlBusOut;
        [InputBus]
        public PacketOut.WriteBus packetOutWriteBus;
        [OutputBus]
        public ConsumerControlBus packetOutComputeConsumerControlBusIn = Scope.CreateBus<ConsumerControlBus>();



        // Interface
        [OutputBus]
        public Interface.InterfaceBus interfaceBus = Scope.CreateBus<Interface.InterfaceBus>();
        [InputBus]
        public Interface.InterfaceControlBus interfaceControlBus;



        // Simulation fields
        private readonly String dir;

        public UDPPingPong(String dir)
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

            InterfaceData request = new InterfaceData();

            // Interface request socket
            do
            {
                interfaceBus.valid = true;
                interfaceBus.interface_function = (byte)InterfaceFunction.CONNECT;

                request.ip = 0;
                request.protocol = (byte)IPv4.Protocol.UDP;
                request.port = 31337;

                interfaceBus.request = request;
                await ClockAsync();
            } while (interfaceControlBus.valid == false);

            if (interfaceControlBus.exit_status != (byte)ExitStatus.OK)
            {
                Console.WriteLine("exit_status != OK");
                return;
            }
            int socket = interfaceControlBus.response.socket;
            Console.WriteLine($"Got socket: {socket}");



            // Start sending packets
            packetInBufferProducerControlBus.available = true;
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
                    do
                    {
                        // Control bus
                        packetInBufferProducerControlBus.bytes_left = bytes_left--;
                        packetInBufferProducerControlBus.valid = true;

                        // Data bus
                        packetInBus.frame_number = frame_number;
                        packetInBus.ip_id = ip_id;
                        packetInBus.ip_protocol = (byte)IPv4.Protocol.UDP;
                        packetInBus.data = b;

                        // Console.WriteLine("Tick");
                        await ClockAsync();
                    } while (packetInConsumerControlBus.ready == false); // resend previous byte if consumer is not ready this cycle
                }

                ip_id++;
            }
            await ClockAsync();
        }
    }
}
