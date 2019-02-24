using System;

using SME;

namespace TCPIP
{
    public partial class Internet : SimpleProcess
    {
        [InputBus]
        private readonly Network.DatagramBus datagramBus;

        [OutputBus]
        public readonly SegmentBus segmentBus = Scope.CreateBus<SegmentBus>();


        public Internet(Network.DatagramBus datagramBus)
        {
            this.datagramBus = datagramBus ?? throw new ArgumentNullException(nameof(datagramBus));
        }

        protected override void OnTick()
        {
            segmentBus.data = datagramBus.data;
        }
    }

}