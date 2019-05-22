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
        public const uint HEADER_SIZE = 0x04; // The fixed size header before data
        public const uint TYPE_OFFSET = 0x00;
        public const uint CODE_OFFSET = 0x01;
        public const uint CHECKSUM_OFFSET_0 = 0x02;
        public const uint CHECKSUM_OFFSET_1 = 0x03;
        
        public const uint IDENTIFIER_OFFSET_0 = 0x04;
        public const uint IDENTIFIER_OFFSET_1 = 0x05;
        public const uint SEQUENCE_NUMBER_OFFSET_0 = 0x05;
        public const uint SEQUENCE_NUMBER_OFFSET_1 = 0x06;


        // Types 
        // https://www.iana.org/assignments/icmp-parameters/icmp-parameters.xhtml#icmp-parameters-codes-0
        // XXX Fill out
        public enum Type : byte{
            ECHO_REPLY = 0,
            ECHO_REQUEST = 8,
        }
    }
}