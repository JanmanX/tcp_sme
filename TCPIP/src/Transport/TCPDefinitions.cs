using System;

namespace TCPIP
{
    public class TCP
    {
        public const uint HEADER_SIZE = 0x14;

        // Offsets
        public const uint SRC_PORT_OFFSET_0 = 0x00;
        public const uint SRC_PORT_OFFSET_1 = 0x01;

        public const uint DST_PORT_OFFSET_0 = 0x02;
        public const uint DST_PORT_OFFSET_1 = 0x03;

        public const uint SEQ_NUMBER_OFFSET_0 = 0x04;
        public const uint SEQ_NUMBER_OFFSET_1 = 0x05;
        public const uint SEQ_NUMBER_OFFSET_2 = 0x06;
        public const uint SEQ_NUMBER_OFFSET_3 = 0x07;

        public const uint ACK_NUMBER_OFFSET_0 = 0x08;
        public const uint ACK_NUMBER_OFFSET_1 = 0x09;
        public const uint ACK_NUMBER_OFFSET_2 = 0x0A;
        public const uint ACK_NUMBER_OFFSET_3 = 0x0B;

        public const uint DATA_OFFSET_OFFSET = 0x0C;
        public const uint DATA_OFFSET_MASK = 0x0F;

        public const uint FLAGS_OFFSET_0 = 0x0C;
        public const uint FLAGS_OFFSET_1 = 0x0D;

        public const uint WINDOW_OFFSET_0 = 0x0E;
        public const uint WINDOW_OFFSET_1 = 0x0F;

        public const uint CHECKSUM_OFFSET_0 = 0x10;
        public const uint CHECKSUM_OFFSET_1 = 0x11;

        public const uint URGENT_POINTER_OFFSET_0 = 0x12;
        public const uint URGENT_POINTER_OFFSET_1 = 0x13;
    }
}