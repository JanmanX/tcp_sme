using SME;
using SME.VHDL;

namespace TCPIP
{
    public partial class Network
    {
        public interface FrameBus : IBus
        {
            uint frame_number { get; set; }
        }
    }
}