using System;

using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    unsafe struct PCB
    {
        // Foreign address and port
        public uint f_address;
        public ushort f_port;

        // Local address and port
        public uint l_address;
        public ushort l_port;

        // Accumulated checksum
        public uint checksum_acc;

        // Counts the bytes received for the current packet
        public uint bytes_received;

        // data for protocol-specific data
        private const int DATA_SIZE = 64;
        public fixed byte data[DATA_SIZE];
    }
}