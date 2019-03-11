using System;
using System.Threading.Tasks;
using SME;

namespace TCPIP
{
    public partial class Network : Process
    {
        [InputBus]
        private readonly FrameBus frameBus;

        [OutputBus]
        public readonly Internet.DatagramBus datagramBus = Scope.CreateBus<Internet.DatagramBus>();

        public Network(FrameBus frameBus)
        {
            this.frameBus = frameBus ?? throw new ArgumentNullException(nameof(frameBus));
        }

        public override async Task Run()
        {
            await ClockAsync();
            datagramBus.Addr = frameBus.Addr;
        }
    }

}