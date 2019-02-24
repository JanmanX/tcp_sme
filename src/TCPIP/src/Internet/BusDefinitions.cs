using SME;

namespace TCPIP
{
    public partial class Internet
    {
        public interface SegmentBus : IBus
        {
            byte data { get; set; }
        }

    }
}