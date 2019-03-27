using SME;
using SME.VHDL;

namespace TCPIP
{
    public partial class Network
    {
        public interface FrameBus : IBus
        {
            uint Addr { get; set; }

            [InitialValue(false)]
            bool Ready { get; set; }
        }

        public interface NetworkStatusBus : IBus 
        {
            UInt1 Busy { get; set; } 
        }
    }
}