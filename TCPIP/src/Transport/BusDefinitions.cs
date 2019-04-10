using SME;

namespace TCPIP
{
    public partial class Transport
    {
        public interface SegmentBus : IBus
        {
            [InitialValue(uint.MaxValue)]
            uint Addr { get; set; }
        }

        public interface OutputBus : IBus
        {
            [InitialValue]
            uint Addr { get; set; }
        }

    }
}