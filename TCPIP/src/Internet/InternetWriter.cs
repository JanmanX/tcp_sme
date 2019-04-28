using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    public partial class InternetWriter : SimpleProcess
    {
        [InputBus]
        private readonly Transport.SegmentBusOut segmentBusOut;

        [OutputBus]
        public readonly Internet.DatagramBusOut datagramBusOut = Scope.CreateBus<Internet.DatagramBusOut>();


        public const uint BUFFER_SIZE = 60;
        private byte[] buffer = new byte[BUFFER_SIZE];

        public InternetWriter(Transport.SegmentBusOut segmentBusOut)
        {
            this.segmentBusOut = segmentBusOut ?? throw new ArgumentNullException(nameof(segmentBusOut));
        }

        protected override void OnTick()
        {
            LOGGER.DEBUG($"0x{segmentBusOut.data:X}");

        }

    }
}
