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
        private readonly SME.Components.TrueDualPortMemory<uint>.IReadResultB readResultB;

        [OutputBus]
        public readonly SME.Components.TrueDualPortMemory<uint>.IControlB controlB;

        [OutputBus]
        public readonly Network.NetworkStatusBus networkStatusBus;

        [OutputBus]
        public readonly Internet.DatagramBus datagramBus = Scope.CreateBus<Internet.DatagramBus>();

        public Network(Network.FrameBus frameBus, 
                SME.Components.TrueDualPortMemory<uint>.IReadResultB readResultB,
                SME.Components.TrueDualPortMemory<uint>.IControlB controlB,
                NetworkStatusBus networkStatusBus)
        {
            this.frameBus = frameBus ?? throw new System.ArgumentNullException(nameof(frameBus));
            this.readResultB = readResultB ?? throw new System.ArgumentNullException(nameof(readResultB));
            this.networkStatusBus = networkStatusBus ?? throw new System.ArgumentNullException(nameof(networkStatusBus));
        }

        
        public override async Task Run()
        {
            if( frameBus.IsValid == false ) {
                await ClockAsync();
                return;
            }

            // Read type
            uint addr = frameBus.Addr;
            controlB.Address = (int)addr;
            await ClockAsync();
            ushort type = (ushort)readResultB.Data;

            // Propagate
            datagramBus.Addr = addr;
            datagramBus.Type = type;
            await ClockAsync();

        }
    }

}