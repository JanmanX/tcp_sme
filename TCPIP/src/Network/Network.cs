using System;

using SME;

namespace TCPIP
{
    public partial class Network : SimpleProcess
    {
        [InputBus]
        private readonly FrameBus frameBus;

        [OutputBus]
        public readonly DatagramBus datagramBus = Scope.CreateBus<DatagramBus>();

        public Network(FrameBus frameBus)
        {
            this.frameBus = frameBus ?? throw new ArgumentNullException(nameof(frameBus));
        }

        protected override void OnTick()
        {
            datagramBus.data = frameBus.data;
        }
    }

}