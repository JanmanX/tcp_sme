using SME;
using SME.VHDL;

namespace TCPIP
{
    public partial class Transport
    {
        public interface SegmentBusIn : IBus
        {
            [InitialValue(0x00)]
            uint ip_id { get; set; } // 32 bits so that we can have ipv4 and ipv6 IDs

            [InitialValue(0x00)]
            uint fragment_offset { get; set; } // offset in bytes for protocols supporting this (IPv4)

            [InitialValue(0x00)]
            byte data { get; set; }

            [InitialValue(0x00)]
            // Refer to Globals.DataMode
            byte data_mode { get; set; }

            [InitialValue(0x00)]
            byte protocol { get; set; }

            [InitialValue(0x00)]
            ushort pseudoheader_checksum { get; set; }

            [InitialValue(0x00)]
            uint ip_addr { get; set; }
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
    }
}