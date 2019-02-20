using SME;

namespace TCPIP
{
    public partial class Transport
    {
        public interface SegmentBus : IBus
        {
            byte data { get; set; }
        }

        public interface OutputBus : IBus
        {
            [InitialValue]
            byte data { get; set; }
        }

    }
}