using SME;

namespace TCPIP
{
    public partial class Internet
    {
        public interface DatagramBus : IBus
        {
            [InitialValue]
            uint Addr { get; set; }

            [InitialValue(0x00)]
            uint Type { get; set; }
        }


    }
}