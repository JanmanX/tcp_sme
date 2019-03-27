using SME;

namespace TCPIP
{
    public partial class Internet
    {
        public interface DatagramBus : IBus
        {
            [InitialValue(false)]
            bool Ready { get; set; }

            [InitialValue]
            uint Addr { get; set; }

            [InitialValue(0x00)]
            ushort Type { get; set; }
        }


    }
}