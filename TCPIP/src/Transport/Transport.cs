using System;
using SME;


namespace TCPIP
{
    public partial class TTransport : SimpleProcess
    {
        [InputBus]
        private readonly Internet.SegmentBus segmentBus;

        [OutputBus]
        public readonly Transport.OutputBus outputBus = Scope.CreateBus<Transport.OutputBus>();


        public TTransport(Internet.SegmentBus segmentBus)
        {
            this.segmentBus = segmentBus ?? throw new ArgumentNullException(nameof(segmentBus));
        }


        protected override void OnTick()
        {
            outputBus.data = segmentBus.data;
        }
    }

}