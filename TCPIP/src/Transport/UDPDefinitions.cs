using System;

using SME;
using SME.VHDL;

namespace TCPIP
{
    class UDP 
    {
        public const uint HEADER_SIZE = 0x08;

        public const uint SRC_PORT_OFFSET_0 = 0x00;
        public const uint SRC_PORT_OFFSET_1 = 0x01;

        public const uint DST_PORT_OFFSET_0 = 0x02;
        public const uint DST_PORT_OFFSET_1 = 0x03;

        public const uint LENGTH_OFFSET_0 = 0x04;
        public const uint LENGTH_OFFSET_1 = 0x05;

        public const uint CHECKSUM_OFFSET_0 = 0x06;
        public const uint CHECKSUM_OFFSET_1 = 0x07;
    }
}