using SME;

namespace TCPIP
{
    public partial class Transport
    {
        public interface SegmentBus : IBus
        {
            [InitialValue(0x00)]
            uint ip_id { get; set; } // 32 bits so that we can have ipv4 and ipv6 IDs

            [InitialValue(0x00)]
            uint fragment_offset { get; set; } // offset in bytes for protocols supporting this (IPv4)

            [InitialValue(0x00)]
            byte protocol { get; set; }

            [InitialValue(0x00)]
            ushort pseudoheader_checksum { get; set; }
        }

        public interface InterfaceWriter : IBus
        {
            [InitialValue]
            uint Addr { get; set; }
        }

    }
}