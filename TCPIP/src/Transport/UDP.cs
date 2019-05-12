using System;

using SME;

namespace TCPIP
{
    public partial class Transport
    {
        public void ParseUDP()
        {
            // Ports
            ushort src_port = (ushort)((buffer_in[UDP.SRC_PORT_OFFSET_0] << 0x08
                                 | buffer_in[UDP.SRC_PORT_OFFSET_1]));
            ushort dst_port = (ushort)((buffer_in[UDP.DST_PORT_OFFSET_0] << 0x08
                                 | buffer_in[UDP.DST_PORT_OFFSET_1]));

            // Find PCB
            int pcb_idx = -1;
            for (int i = 0; i < NUM_PCB; i++)
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
            for(int i = 0; i < UDP.HEADER_SIZE; i += 2) {
                pcbs[pcb_idx].checksum_acc += (ushort)((buffer_in[i] << 0x08
                                 | buffer_in[i+1]));
            }


            // Length
            ushort length = (ushort)((buffer_in[UDP.LENGTH_OFFSET_0] << 0x08
                                 | buffer_in[UDP.LENGTH_OFFSET_1]));


            // Start passing
            StartPass(pcb_idx, ip_id, 0, length);
        }
    }
}