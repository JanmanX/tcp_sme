using SME;
using SME.VHDL;

namespace TCPIP
{
    public partial class Network
    {
        public interface FrameBusIn : IBus
        {
            uint frame_number { get; set; }

            byte data { get; set; }
        }
    }
}