using SME;
using SME.VHDL;

namespace TCPIP
{
    public partial class Transport
    {
        public interface PacketInBus : IBus
        {
            [InitialValue(0x00)]
            uint ip_id { get; set; } // 32 bits so that we can have ipv4 and ipv6 IDs

            [InitialValue(long.MaxValue)]
            long frame_number { get; set; }

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

        public interface TransportBus : IBus
        {
            [InitialValue(false)]
            bool valid { get; set; }

            [InitialValue(0)]
            byte interface_function { get; set; }

            Interface.InterfaceArgs args { get; set; }

            [InitialValue(-1)]
            int socket { get; set; }
        }

        public interface TransportControlBus : IBus
        {
            [InitialValue(false)]
            bool valid { get; set; }

            [InitialValue(0)]
            byte interface_function { get; set; }

            [InitialValue(0)]
            byte exit_status { get; set; }

            [InitialValue(-1)]
            int socket { get; set; }
        }
    }
}