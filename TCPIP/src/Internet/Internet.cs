using System;

using SME;

namespace TCPIP
{
    public partial class Internet : SimpleProcess
    {
        [InputBus]
        private readonly Internet.DatagramBus datagramBus;

        [OutputBus]
        public readonly Transport.SegmentBus segmentBus = Scope.CreateBus<Transport.SegmentBus>();


        public Internet(Internet.DatagramBus datagramBus)
        {
            this.datagramBus = datagramBus ?? throw new ArgumentNullException(nameof(datagramBus));
        }

        protected override void OnTick()
        {
            segmentBus.Addr = datagramBus.Addr;
        }
    }

}