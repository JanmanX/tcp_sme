using SME;
using SME.VHDL;

namespace TCPIP
{
    public partial class Transport
    {
        public interface SegmentBusOut : IBus
        {
            [InitialValue(0x00)]
            uint ip_addr { get; set; }

            [InitialValue(0x00)]
            byte data { get; set; }

            [InitialValue(0x00)]
            byte protocol { get; set; }

            [InitialValue(0x40)] // XXX: what initial length of TTL?
            byte ttl { get; set; }

            [InitialValue(0x00)]
            byte data_mode { get; set; }
        }

        public interface DataInBus : IBus
        {
            [InitialValue(false)]
            bool valid { get; set; }

            [InitialValue(-1)]
            int socket { get; set; }

            [InitialValue(0x00)]
            uint ip_id { get; set; }

            [InitialValue(0x00)]
            uint tcp_seq { get; set; }

            [InitialValue(0x00)]
            byte data { get; set; }

            [InitialValue(false)]
            bool finished { get; set; }

            [InitialValue(false)]
            bool invalidate { get; set; } // XXX: not used yet
        }
    }
}