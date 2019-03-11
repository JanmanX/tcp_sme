using SME;

namespace TCPIP
{
    public partial class Internet
    {
        public interface DatagramBus : IBus
        {
            [InitialValue]
            uint Addr { get; set; }
        }


    }
}