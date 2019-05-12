using System;

using SME;

namespace TCPIP
{
    public partial class Transport
    {
        // Globals
        private uint tcp_iss = 0xBEEF; // Initial sequence number  (XXX: Should this be random)

        public void ParseTCP()
        {
            ///////////////////// PARSE TCP:
            // - Checksum
            // - Find PCB
            //   - Reset timers (keepalive etc)
            // - if ACK, connection completed
            // - if SYN:

            ushort src_port = (ushort)((buffer_in[TCP.SRC_PORT_OFFSET_0] << 0x08
                                 | buffer_in[TCP.SRC_PORT_OFFSET_1]));
            ushort dst_port = (ushort)((buffer_in[TCP.DST_PORT_OFFSET_0] << 0x08
                                 | buffer_in[TCP.DST_PORT_OFFSET_1]));


            // Find PCB
            int pcb_idx = -1;
            for (int i = 0; i < NUM_PCB; i++)
            {
                if (pcbs[i].l_port == dst_port
                    && pcbs[i].f_port == src_port
                    && pcbs[i].protocol == (byte)IPv4.Protocol.TCP)
                {
                    pcb_idx = i;
                }
            }

            if (pcb_idx == -1)
            {
                // TODO: Drop with reset
                LOGGER.WARN("PCB not found");
                pcb_idx = 0; // debug  
            }

            // Calculate (part of) checksum
            ulong acc = 0x00;
            for (uint i = 0; i < TCP.HEADER_SIZE; i = i + 2)
            {
                acc += (ulong)((buffer_in[i] << 0x08
                                 | buffer_in[i + 1]));
            }
            acc += segmentBusIn.pseudoheader_checksum;

            // Add carry bits and do one-complement on 16 bits
            // Overflow  can max happen twice
            acc = ((acc & 0xFFFF) + (acc >> 0x10));
            pcbs[pcb_idx].checksum_acc = (ushort)~((acc & 0xFFFF) + (acc >> 0x10));

        }



        private void DropWithReset()
        {

        }
    }
}