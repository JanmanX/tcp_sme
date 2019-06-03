using System;

using SME;

namespace TCPIP
{
    public partial class Transport
    {
        public void ParseUDP()
        {
//            for (int i = 0; i < UDP.HEADER_SIZE; i++)
//            {
//                Console.Write($"0x{buffer_in[i]:X} ");
//            }
//            Console.WriteLine();

            // Ports
            ushort src_port = (ushort)((buffer_in[UDP.SRC_PORT_OFFSET_0] << 0x08
                                 | buffer_in[UDP.SRC_PORT_OFFSET_1]));
            ushort dst_port = (ushort)((buffer_in[UDP.DST_PORT_OFFSET_0] << 0x08
                                 | buffer_in[UDP.DST_PORT_OFFSET_1]));

            // Find PCB
            int pcb_idx = -1;
            for (int i = 0; i < NUM_SOCKETS; i++)
            {
                if (pcbs[i].l_port == dst_port
                    && pcbs[i].f_port == src_port
                    && pcbs[i].protocol == (byte)IPv4.Protocol.UDP)
                {
                    pcb_idx = i;
                }
            }
            if (pcb_idx == -1)
            {
                // TODO: Drop with reset
                LOGGER.WARN("PCB not found");
                pcb_idx = 0; // XXX: Debug  
            }

            // Calculate header checksum
            for (int i = 0; i < UDP.HEADER_SIZE; i += 2)
            {
                pcbs[pcb_idx].checksum_acc += (ushort)((buffer_in[i] << 0x08
                                 | buffer_in[i + 1]));
            }

            // Get checksum. Used only for debug
            ushort checksum = (ushort)((buffer_in[UDP.CHECKSUM_OFFSET_0] << 0x08
                                 | buffer_in[UDP.CHECKSUM_OFFSET_1]));

            // Length
            ushort data_length = (ushort)((buffer_in[UDP.LENGTH_OFFSET_0] << 0x08
                                 | buffer_in[UDP.LENGTH_OFFSET_1])
                                 - UDP.HEADER_SIZE);

            LOGGER.DEBUG($"Parsed UDP: src_port: {src_port}, dst_port: {dst_port}, length: {data_length + UDP.HEADER_SIZE}, checksum: 0x{checksum:X}");

            // Start passing
            StartPass(pcb_idx, ip_id, 0, data_length);
        }


        private void BuildHeaderUDP(PassData data)
        {
            buffer_out[UDP.SRC_PORT_OFFSET_0] = pcbs[data.socket].src_port << 0x08;
            buffer_out[UDP.SRC_PORT_OFFSET_1] = (byte)pcbs[data.socket].src_port;

            buffer_out[UDP.DST_PORT_OFFSET_0] = pcbs[data.socket].dst_port << 0x08;
            buffer_out[UDP.DST_PORT_OFFSET_1] = (byte)pcbs[data.socket].dst_port;

            ushort udp_length = data.bytes_passed + UDP.HEADER_SIZE;
            buffer_out[UDP.LENGTH_OFFSET_0] = udp_length << 0x08;
            buffer_out[UDP.LENGTH_OFFSET_1] = (byte)udp_length;


            // Finish checksum
            uint checksum_acc = pcbs[data.socket].src_port
                            + pcbs[data.socket].dst_port
                            + udp_length
                            + data.checksum_acc;
            checksum_acc = (checksum_acc & 0xFFFF) + (checksum_acc >> 0x10);
            ushort checksum = (ushort)(checksum_acc & 0xFFFF) + (checksum_acc >> 0x10);

            
           buffer_out[UDP.CHECKSUM_OFFSET_0] = checksum << 0x08;
           buffer_out[UDP.CHECKSUM_OFFSET_1] = (byte)checksum; 


        }
    }
}