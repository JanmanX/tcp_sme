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
            LOGGER.log.Debug($"Parsing TCP with 0x{segmentBusIn.ip_id:X}");

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
                    && pcbs[i].f_port == src_port)
                {
                    pcb_idx = i;
                }
            }

            if (pcb_idx == -1)
            {
                // TODO: Drop with reset
            }



            // Calculate checksum
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
            ushort calculated_checksum = (ushort)~((acc & 0xFFFF) + (acc >> 0x10));
            if (calculated_checksum != 0x00)
            {
                SimulationOnly(() =>
                {
                    LOGGER.log.Warn($"Invalid checksum: 0x{calculated_checksum:X}");
                });
            }
        }

        private void DropWithReset()
        {

        }
    }
}