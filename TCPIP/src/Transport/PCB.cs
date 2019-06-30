using System;

using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    public enum PCB_STATE : byte
    {
        CLOSED = 0,
        OPEN = 1,
        LISTENING = 2,
        CONNECTED = 3,
        CONNECTING = 4, // (Trying to connect to remote. Handshakes and stuff)
        // Add more
    }

    struct PCB
    {
        // Connection state
        public byte state;

        // Protocol
        public byte protocol;

        // Foreign address and port
        public uint f_address;
        public ushort f_port;

        // Local address and port
        public uint l_address;
        public ushort l_port;

        // Accumulated checksum (used in TCP)
        public uint checksum_acc;

        // Counts the bytes received for the current packet
        public uint bytes_received;

        // The current sequence number we safely can submit
        public uint last_sequence_ready;

        // // data for protocol-specific data
        // private const int DATA_SIZE = 64;
        // public fixed byte data[DATA_SIZE];
    }
}