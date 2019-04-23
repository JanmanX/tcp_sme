using System;
using SME;

namespace TCPIP
{
/*
         0                   1                   2                   3  
         0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |      Type     |      Code     |            Checksum           |
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |                                                               |
        +                          Message Body                         +
        |                                                               |
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
 */

    public class ICMP
    {
        public const uint PACKET_SIZE = 0x10; // XXX: hardcoded the ICMP packet size...

        public const uint TYPE_OFFSET = 0x00;
        public const uint CODE_OFFSET = 0x01;
        public const uint CHECKSUM_OFFSET_0 = 0x02;
        public const uint CHECKSUM_OFFSET_1 = 0x03;
        public const uint BODY_OFFSET_0 = 0x04;
        public const uint BODY_OFFSET_1 = 0x05;
        public const uint BODY_OFFSET_2 = 0x06;
        public const uint BODY_OFFSET_3 = 0x07;

        // Types 
        // https://www.iana.org/assignments/icmp-parameters/icmp-parameters.xhtml#icmp-parameters-codes-0
        public const byte TYPE_ECHO_REPLY = 0x00;


        public const byte TYPE_ECHO = 0x08;



        // Codes
        public const byte CODE_NO_CODE = 0x00;
    }
}