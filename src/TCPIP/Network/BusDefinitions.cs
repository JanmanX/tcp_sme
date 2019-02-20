using SME;

namespace TCPIP
{
    public partial class Network
    {
        public interface FrameBus : IBus
        {
            byte data { get; set; }
        }

        public interface DatagramBus : IBus
        {
            [InitialValue]
            byte data { get; set; }
        }

    }
}