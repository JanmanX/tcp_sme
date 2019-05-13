using System;
using SME;

namespace TCPIP
{
    public class EthernetIIFrame
    {
        public const uint HEADER_SIZE = 0x0E; 
        public const uint ETHERTYPE_OFFSET_0 = 0x0C;
        public const uint ETHERTYPE_OFFSET_1 = 0x0D;
        public enum EtherType : ushort
        {
            IPv4 = 0x0800,
            ARP = 0x0806,
            IPv6 = 0x86DD,
        }
    }
}