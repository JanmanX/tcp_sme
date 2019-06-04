using SME;

namespace TCPIP
{
    public partial class Memory
    {
        public interface InternetPacketBus : IBus
        {
            long frame_number { get; set; } // Increments so we can distinguish between new packages
            byte ip_protocol { get; set; }
            ulong ip_dst_addr_0 { get; set; }
            ulong ip_dst_addr_1 { get; set; }
            ulong ip_src_addr_0 { get; set; }
            ulong ip_src_addr_1 { get; set; }
            uint ip_id { get; set; }
            uint fragment_offset { get; set; }
            int data_length { get; set; } // the size we are writing currently
            byte data { get; set; } // The data needed
        }
        public interface DataInWriteBus : IBus
        {
            int socket { get; set; }
            uint tcp_seq { get; set; }
            byte data { get; set; }
            bool invalidate { get; set; }
            int data_length { get; set; }
        }
        public interface DataOutReadBus : IBus
        {
            int socket { get; set; }
            byte data { get; set; }
        }


    }

    public partial class PacketOut
    {
        public interface PacketOutWriteBus: IBus
        {
            [InitialValue(0x00)]
            byte data { get; set; }

            [InitialValue(0x00)]
            uint addr { get; set; }
        }
    }

    public partial class PacketIn
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


    }
}