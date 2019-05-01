using System;
using SME;
using SME.VHDL;

namespace TCPIP
{
    partial class InternetReader
    {
        protected void ParseIPv4()
        {
            // Checksum
            ulong acc = 0x00;
            for (uint i = 0; i < IPv4.HEADER_SIZE; i = i + 2)
            {
                acc += (ulong)((buffer_in[i] << 0x08
                                 | buffer_in[i + 1]));

            }
            // Add carry bits and do one-complement on 16 bits
            // Overflow  can max happen twice
            acc = ((acc & 0xFFFF) + (acc >> 0x10));
            ushort calculated_checksum = (ushort)~((acc & 0xFFFF) + (acc >> 0x10));
            if (calculated_checksum != 0x00)
            {
                LOGGER.WARN($"Invalid checksum: 0x{calculated_checksum:X}");
            }


            // Get ID
            ushort id = (ushort)((buffer_in[IPv4.ID_OFFSET_0] << 0x08)
                                       | buffer_in[IPv4.ID_OFFSET_1]);

            // Check version
            if ((buffer_in[IPv4.VERSION_OFFSET] >> 0x04) != IPv4.VERSION)
            {
                LOGGER.WARN($"Unknown IPv4 version {(buffer_in[IPv4.VERSION_OFFSET] & 0x0F):X}");
            }

            // Get Internet Header Length
            byte ihl = (byte)(buffer_in[IPv4.IHL_OFFSET] & 0x0F);
            if (ihl != 0x05)
            {
                LOGGER.DEBUG($"Odd size of IPv4 Packet: IHL: {(byte)ihl}");
            }

            // Get total length
            ushort total_len = (ushort)((buffer_in[IPv4.TOTAL_LENGTH_OFFSET_0] << 0x08)
                                       | buffer_in[IPv4.TOTAL_LENGTH_OFFSET_1]);

            // Get protocol
            byte protocol = buffer_in[IPv4.PROTOCOL_OFFSET];

            // Flags
            byte flags = (byte)((buffer_in[IPv4.FLAGS_OFFSET] >> 0x05) & 0x0E);
            ushort fragment_offset = (ushort)((buffer_in[IPv4.FRAGMENT_OFFSET_OFFSET_0] << 0x08
                                        | buffer_in[IPv4.FRAGMENT_OFFSET_OFFSET_1])
                                        & IPv4.FRAGMENT_OFFSET_MASK);
            if ((flags & (byte)IPv4.Flags.MF) != 0x00)
            {
                LOGGER.ERROR($"IP packet fragmentation not supported!");
            }

            // Destionation address
            uint dst_address = (uint)(buffer_in[IPv4.SRC_ADDRESS_OFFSET_0] << 0x18)
                            | (uint)(buffer_in[IPv4.SRC_ADDRESS_OFFSET_1] << 0x10)
                            | (uint)(buffer_in[IPv4.SRC_ADDRESS_OFFSET_2] << 0x08)
                            | (uint)(buffer_in[IPv4.SRC_ADDRESS_OFFSET_3]);

            // TODO: implement check if packet for us

            // Source Address
            uint src_address = (uint)(buffer_in[IPv4.SRC_ADDRESS_OFFSET_0] << 0x18)
                            | (uint)(buffer_in[IPv4.SRC_ADDRESS_OFFSET_1] << 0x10)
                            | (uint)(buffer_in[IPv4.SRC_ADDRESS_OFFSET_2] << 0x08)
                            | (uint)(buffer_in[IPv4.SRC_ADDRESS_OFFSET_3]);

            LOGGER.DEBUG($@"Received packet for:
                                {buffer_in[IPv4.SRC_ADDRESS_OFFSET_0]}.
                                {buffer_in[IPv4.SRC_ADDRESS_OFFSET_1]}.
                                {buffer_in[IPv4.SRC_ADDRESS_OFFSET_2]}.
                                {buffer_in[IPv4.SRC_ADDRESS_OFFSET_3]}");


            // Calculate pseudoheader checksum
            ulong acc2 = (ulong)(total_len
                                + protocol
                                + src_address
                                + (dst_address >> 0x10)
                                + (dst_address & 0xFFFF)
                                + (src_address >> 0x10)
                                + (src_address & 0xFFFF));
            acc2 = (acc2 & 0xFFFF) + (acc2 >> 0x10);
            ushort pseudoheader_checksum = (ushort)~((acc2 & 0xFFFF) + (acc2 >> 0x10));


            // Propagate parsed packet
            PropagatePacket(id, protocol, fragment_offset, pseudoheader_checksum);
        }


    }
}