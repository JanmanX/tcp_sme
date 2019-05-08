using System;

using SME;
using SME.Components;
using SME.VHDL;

namespace TCPIP
{
    partial class Transport
    {

        void ParseICMP()
        {
            // TODO: Checksum. ( a bit of a challenge, because the size of ICMP is variable)


            //            byte type = buffer_in[ICMP.TYPE_OFFSET];
            //            byte code = buffer_in[ICMP.CODE_OFFSET];
            //            switch (type)
            //            {
            //                case (byte)ICMP.TYPE_ECHO:
            //                    //                    ClearBufferOut();
            //
            //                    // Fill buffer
            //                    buffer_out[ICMP.TYPE_OFFSET] = ICMP.TYPE_ECHO_REPLY;
            //                    buffer_out[ICMP.CODE_OFFSET] = ICMP.CODE_NO_CODE;
            //
            //                    // Length of packet
            //                    ushort len = 4;
            //
            //                    // ushort checksum = ChecksumBufferOut(len);
            //
            //                    buffer_out[ICMP.CHECKSUM_OFFSET_0] = (byte)(checksum >> 0x08);
            //                    buffer_out[ICMP.CHECKSUM_OFFSET_1] = (byte)(checksum & 0x00FF);
            //
            //                    StartSending(len, (byte)IPv4.Protocol.ICMP, DataMode.SEND_INTERNET);
            //                    break;
            //
            //
            //                //                case (byte)ICMP.ICMP_UNREACH:
            //                //
            //                //                    break;
            //                //
            //                default:
            //                    LOGGER.ERROR($"ICMP type {type} unknown!");
            //                    break;
            //            }
        }


    }
}