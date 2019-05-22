using System;

using SME;
using SME.Components;
using SME.VHDL;

namespace TCPIP
{
    public partial class Transport
    {
        // Parse the initial header
        void ParseICMP()
        {
            byte type = buffer_in[ICMP.TYPE_OFFSET];
            byte code = buffer_in[ICMP.CODE_OFFSET];

            // Get total length
            ushort checksum = (ushort)((buffer_in[ICMP.CHECKSUM_OFFSET_0] << 0x08)
                                       | buffer_in[ICMP.CHECKSUM_OFFSET_0]);

            ushort calculated_checksum = ChecksumBufferIn(0,segmentBusIn.data_length);
            if (calculated_checksum != 0x00)
            {
                LOGGER.WARN($"Invalid ICMP checksum: 0x{calculated_checksum:X2}");
            }

            ushort identifier = 0, sequence_number = 0;
            switch(type){
                case (byte)ICMP.Type.ECHO_REPLY:
                case (byte)ICMP.Type.ECHO_REQUEST:
                    identifier = (ushort)((buffer_in[ICMP.IDENTIFIER_OFFSET_0] << 0x08)
                                          |buffer_in[ICMP.IDENTIFIER_OFFSET_1]);
                    sequence_number = (ushort)((buffer_in[ICMP.SEQUENCE_NUMBER_OFFSET_0] << 0x08)
                                               |buffer_in[ICMP.SEQUENCE_NUMBER_OFFSET_1]);

                    ResponseICMP(ICMP.Type.ECHO_REQUEST);
                    break;
                default:
                    LOGGER.ERROR($"ICMP type not parsed {type}");
                    break;
            }
        }


        // Test and generate if we should respond to the icmp request
        bool ResponseICMP(ICMP.Type type){
            switch(type){
                case ICMP.Type.ECHO_REQUEST:
                    ResponseICMPEcho();
                    return true;
                case ICMP.Type.ECHO_REPLY:
                    LOGGER.WARN("$ICMP: Should not reply to reply, only request");
                    return false;
                default:
                    LOGGER.ERROR($"ICMP no ResponseICMP defined {type}");
                    break;
            }
            return false;
        }
        void ResponseICMPEcho(){
            // Flip the sender and destination from the ip header
            // Set the type, and burn in the bits
            buffer_in[ICMP.TYPE_OFFSET] = (byte)ICMP.Type.ECHO_REPLY;

            // Calculate the checksum, and burn them in
            ushort checksum = ChecksumBufferIn(0,segmentBusIn.data_length,(int)ICMP.CHECKSUM_OFFSET_0);
            buffer_in[ICMP.CHECKSUM_OFFSET_0] = (byte)(checksum & 0xff);
            buffer_in[ICMP.CHECKSUM_OFFSET_1] = (byte)(checksum >> 0x08);


            // Bypass with the next
            StartBypass();


        }


    }
}