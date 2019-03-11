using SME;

namespace TCPIP
{
    public partial class Network
    {
        public interface FrameBus : IBus
        {
            uint Addr { get; set; }
        }

    }
}