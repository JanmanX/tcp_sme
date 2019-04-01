using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    public partial class Network : SimpleProcess
    {
        [InputBus]
        private readonly Network.FrameBus frameBus;

        [InputBus]
        public readonly TrueDualPortMemory<byte>.IControlA controlA;

        [OutputBus]
        public readonly Internet.DatagramBus datagramBus = Scope.CreateBus<Internet.DatagramBus>();

        public Network(Network.FrameBus frameBus, 
                        TrueDualPortMemory<byte>.IControlA controlA)
        {
            this.frameBus = frameBus ?? throw new System.ArgumentNullException(nameof(frameBus));
            this.controlA = controlA?? throw new System.ArgumentNullException(nameof(controlA));

        }

        protected override void OnTick() {
            datagramBus.Addr = controlA.Data;
        }
    }

}