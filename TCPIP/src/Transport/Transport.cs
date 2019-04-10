using System;
using SME;


namespace TCPIP
{
    public partial class Transport : SimpleProcess
    {
        [InputBus]
        private readonly Transport.SegmentBus segmentBus;

        [OutputBus]
        public readonly Transport.OutputBus outputBus = Scope.CreateBus<Transport.OutputBus>();


        public Transport(Transport.SegmentBus segmentBus)
        {
            this.segmentBus = segmentBus ?? throw new ArgumentNullException(nameof(segmentBus));
        }


        protected override void OnTick()
        {

            outputBus.Addr = segmentBus.Addr;
           // outputBus.Addr = segmentBus.Addr;
        }
    }

}