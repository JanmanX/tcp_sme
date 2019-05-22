using SME;
using SME.VHDL;

namespace TCPIP
{
    public partial class Transport
    {
        public interface SegmentBusIn : IBus
        {
            [InitialValue(false)]
            bool valid { get; set; } // Indicates whether the current data is valid

            [InitialValue(0x00)]
            uint ip_id { get; set; } // 32 bits so that we can have ipv4 and ipv6 IDs

            [InitialValue(long.MaxValue)]
            long frame_number { get; set; }

            [InitialValue(0x00)]
            uint fragment_offset { get; set; } // offset in bytes for protocols supporting this (IPv4)

            [InitialValue(0x00)]
            byte data { get; set; }

            [InitialValue(0x00)]
            uint data_length { get; set; } // Length of the data to receive

            [InitialValue(0x00)]
            byte protocol { get; set; }

            [InitialValue(0x00)]
            ushort pseudoheader_checksum { get; set; }

            // Up to 128 bit addressing
            [InitialValue(0x00)]
            ulong src_ip_addr_0 { get; set; }
            [InitialValue(0x00)]
            ulong src_ip_addr_1 { get; set; }

            // Up to 128 bit addressing
            [InitialValue(0x00)]
            ulong dst_ip_addr_0 { get; set; }
            [InitialValue(0x00)]
            ulong dst_ip_addr_1 { get; set; }
        }

        public interface SegmentBusInControl : IBus
        {
            [InitialValue(true)]
            bool ready { get; set; } // Indicates whether the DataGramBusIn can be written to

            [InitialValue(false)]
            bool skip { get; set; } // Whether we want the next frame
        }

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