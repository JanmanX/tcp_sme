using System;

using SME;
using SME.Components;
using SME.VHDL;

namespace TCPIP
{
    partial class InternetIn
    {
        // Parse the initial header
        void ParseICMP(ushort offset)
        {
            byte type = buffer_in[offset + ICMP.TYPE_OFFSET];
            byte code = buffer_in[offset + ICMP.CODE_OFFSET];
            
            // Get total length
            ushort checksum = (ushort)((buffer_in[offset + ICMP.CHECKSUM_OFFSET_0] << 0x08)
                                       | buffer_in[offset + ICMP.CHECKSUM_OFFSET_0]);

            ushort calculated_checksum = ChecksumBufferIn(offset,cur_segment_data.ip.total_len );
            if (calculated_checksum != 0x00)
            {
                LOGGER.WARN($"Invalid ICMP checksum: 0x{calculated_checksum:X2}");
            }
            
            ushort identifier = 0, sequence_number = 0;
            switch(type){
                case (byte)ICMP.Type.ECHO_REPLY:
                case (byte)ICMP.Type.ECHO_REQUEST:
                    identifier = (ushort)((buffer_in[offset + ICMP.IDENTIFIER_OFFSET_0] << 0x08)
                                          |buffer_in[offset + ICMP.IDENTIFIER_OFFSET_1]);
                    sequence_number = (ushort)((buffer_in[offset + ICMP.SEQUENCE_NUMBER_OFFSET_0] << 0x08)
                                               |buffer_in[offset + ICMP.SEQUENCE_NUMBER_OFFSET_1]);
                    break;
                default:
                    LOGGER.ERROR($"ICMP type not parsed {type}");
                    break;
            }
            SaveSegmentDataICMP(type,code,identifier,sequence_number,checksum,offset);           
        }

        
        // Test and generate if we should respond to the icmp request
        bool ResponseICMP(){
            switch(cur_segment_data.icmp.type){
                case (byte)ICMP.Type.ECHO_REQUEST:
                    ResponseICMPEcho();
                    return true;
                case (byte)ICMP.Type.ECHO_REPLY:
                    LOGGER.WARN("$ICMP: Should not reply to reply, only request");
                    return false;
                default:
                    LOGGER.ERROR($"ICMP no ResponseICMP defined {cur_segment_data.icmp.type}");
                    break;
            }
            return false;
        }
        void ResponseICMPEcho(){
            uint offset = cur_segment_data.offset;
            // Flip the sender and destination from the ip header
            ulong src_temp_0 = cur_segment_data.ip.src_addr_0;
            ulong src_temp_1 = cur_segment_data.ip.src_addr_1;
            cur_segment_data.ip.src_addr_0 = cur_segment_data.ip.dst_addr_0;
            cur_segment_data.ip.src_addr_1 = cur_segment_data.ip.dst_addr_1;
            cur_segment_data.ip.dst_addr_0 = src_temp_0;
            cur_segment_data.ip.dst_addr_1 = src_temp_1;
            // Set the type, and burn in the bits
            cur_segment_data.icmp.type = (byte)ICMP.Type.ECHO_REPLY;
            buffer_in[offset + ICMP.TYPE_OFFSET] = cur_segment_data.icmp.type;
            
            // Calculate the checksum, and burn them in
            cur_segment_data.icmp.checksum = ChecksumBufferIn(offset,cur_segment_data.ip.total_len,
                                                              (int)offset + (int)ICMP.CHECKSUM_OFFSET_0);
            buffer_in[offset + ICMP.CHECKSUM_OFFSET_0] = (byte)(cur_segment_data.icmp.checksum & 0xff);
            buffer_in[offset + ICMP.CHECKSUM_OFFSET_1] = (byte)(cur_segment_data.icmp.checksum >> 0x08);

            // Set the send type, so we can distinguish 
            cur_segment_data.send_type = SendType.IMCP_Echo;
        }


    }
}