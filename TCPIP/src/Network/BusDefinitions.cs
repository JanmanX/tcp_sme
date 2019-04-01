using SME;
using SME.VHDL;

namespace TCPIP
{
    public partial class Network
    {
        public interface FrameBus : IBus
        {
            uint number { get; set; }
       }
    }
}