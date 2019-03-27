using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;


namespace TCPIP
{
    [ClockedProcess]
    public partial class Network : Process
    {
        [InputBus]
        private readonly Network.FrameBus frameBus;

        [InputBus]
        private readonly EightPortMemory<byte>.IReadResultB readResultB;

        [OutputBus]
        public readonly EightPortMemory<byte>.IControlB controlB;

        [OutputBus]
        public readonly Network.NetworkStatusBus networkStatusBus;

        [OutputBus]
        public readonly Internet.DatagramBus datagramBus = Scope.CreateBus<Internet.DatagramBus>();

        public Network(Network.FrameBus frameBus, 
                EightPortMemory<byte>.IReadResultB readResultB,
                EightPortMemory<byte>.IControlB controlB,
                NetworkStatusBus networkStatusBus)
        {
            this.frameBus = frameBus ?? throw new System.ArgumentNullException(nameof(frameBus));
            this.readResultB = readResultB ?? throw new System.ArgumentNullException(nameof(readResultB));
            this.controlB = controlB ?? throw new System.ArgumentNullException(nameof(controlB));
            this.networkStatusBus = networkStatusBus ?? throw new System.ArgumentNullException(nameof(networkStatusBus));
        }

        
        public override async Task Run()
        {
            while (true)
            {
                while (frameBus.Ready == false)
                {
                    await ClockAsync();
                }

                // Read type
                uint addr = frameBus.Addr;
                controlB.Enabled = true;
                controlB.IsWriting = false;
                controlB.Address = (int)(addr + EthernetIIFrame.ETHERTYPE_OFFSET);
                await ClockAsync();
                await ClockAsync();
                ushort type = (ushort)readResultB.Data;

                // Propagate
                datagramBus.Addr = addr;
                datagramBus.Type = type;

                SimulationOnly(() => {
                    Console.WriteLine("datagramBus.Type = " + type);
                });

                datagramBus.Ready = true;
                await ClockAsync();
            }
        }
    }

}