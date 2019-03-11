using System;
using System.Threading.Tasks;
using SME;

namespace TCPIP
{
    public partial class Network : SimpleProcess
    {
        [InputBus]
        private readonly Network.FrameBus frameBus;

        [OutputBus]
        public readonly Internet.DatagramBus datagramBus = Scope.CreateBus<Internet.DatagramBus>();

        public Network(Network.FrameBus frameBus)
        {
            this.frameBus = frameBus ?? throw new ArgumentNullException(nameof(frameBus));
        }

        protected override void OnTick()
        {
            
            //await ClockAsync();
            datagramBus.Addr = frameBus.Addr;
        }
    }

}