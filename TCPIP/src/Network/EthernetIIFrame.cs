using System;
using SME;

namespace TCPIP
{
    enum EtherType
    {
        IPv4 = 0x0800,
        ARP = 0x0806,
        IPv6 = 0x86DD,
    }


    class EthernetIIFrame
    {
        public const uint ETHERTYPE_OFFSET = 0x0C; 
    }


}