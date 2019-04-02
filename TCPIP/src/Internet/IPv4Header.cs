using System;

namespace TCPIP
{
    class IPv4Header
    {
        public const uint VERSION = 0x04;

        public const uint VERSION_OFFSET = 0x00;
        public const uint IHL_OFFSET = 0x00;

        public const uint TOTAL_LEN_OFFSET_0 = 0x02;
        public const uint TOTAL_LEN_OFFSET_1 = 0x03;

        public const uint PROTOCOL_OFFSET = 0x09;

        public const uint CHECKSUM_OFFSET_0 = 0x0A;
        public const uint CHECKSUM_OFFSET_1 = 0x0B;


    }
}