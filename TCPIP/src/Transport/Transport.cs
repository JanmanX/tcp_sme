using System;
using SME;
using SME.Components;

namespace TCPIP
{
    public partial class Transport : SimpleProcess
    {
        [InputBus]
        private readonly Transport.SegmentBus segmentBus;

        [InputBus]
        private readonly TrueDualPortMemory<byte>.IControlA controlA;

        [OutputBus]
        public readonly Transport.OutputBus outputBus = Scope.CreateBus<Transport.OutputBus>();


        public Transport(Transport.SegmentBus segmentBus)
        {
            this.segmentBus = segmentBus ?? throw new ArgumentNullException(nameof(segmentBus));
        }


        protected override void OnTick()
        {

            outputBus.Addr = segmentBus.ip_id;
            // outputBus.Addr = segmentBus.Addr;
        }
    }

}