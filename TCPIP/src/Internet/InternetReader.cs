using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    public partial class InternetReader : SimpleProcess
    {
        // CONFIG
        // TODO: Find a better place to put this?
        public const uint IP_ADDRESS = 0x00;

        [InputBus]
        private readonly Internet.DatagramBus datagramBus;

        [InputBus]
        private readonly TrueDualPortMemory<byte>.IControlA controlA;

        [OutputBus]
        public readonly Transport.SegmentBus segmentBus = Scope.CreateBus<Transport.SegmentBus>();

        // Local storage
        private byte[] buffer = new byte[50]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..
        private bool read = false; // Indicates whether process should read into buffer
        private uint byte_idx = 0x00;
        private ushort type = 0x00;
        private long cur_frame_number = long.MaxValue;
        private UInt4 ihl;
        public InternetReader(Internet.DatagramBus datagramBus,
                        TrueDualPortMemory<byte>.IControlA controlA)
        {
            this.datagramBus = datagramBus ?? throw new ArgumentNullException(nameof(datagramBus));
            this.controlA = controlA ?? throw new ArgumentNullException(nameof(controlA));
        }

        protected override void OnTick()
        {
            // If new frame
            if (datagramBus.frame_number != cur_frame_number)
            {
                // Reset values
                read = true;
                cur_frame_number = datagramBus.frame_number;
                type = datagramBus.type;
                byte_idx = 0x00;
            }

            // Save data and process
            if (read && byte_idx < buffer.Length)
            {
                buffer[byte_idx++] = controlA.Data;

                // Processing
                switch (type)
                {
                    case (ushort)EtherType.IPv4:
                        // End of header, start parsing
                        if (byte_idx == IPv4.SIZE)
                        {
                            read = false;
                            parseIPv4();
                        }
                        break;
                }
            }

        }

        protected void parseIPv4()
        {
            // Checksum
            ulong acc = 0x00;
            for (uint i = 0; i < 0x14; i = i + 2)
            {
                acc += (ulong)((buffer[i] << 0x08
                                 | buffer[i + 1]));

            }
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


            // Get ID
            ushort id = (ushort)((buffer[IPv4.ID_OFFSET_0] << 0x08)
                                       | buffer[IPv4.ID_OFFSET_1]);

            // Check version
            if ((buffer[IPv4.VERSION_OFFSET] >> 0x04) != IPv4.VERSION)
            {
                SimulationOnly(() =>
               {
                   LOGGER.log.Warn($"Uknown IPv4 version {(buffer[IPv4.VERSION_OFFSET] & 0x0F):X}");
               });
            }

            // Get Internet Header Length
            ihl = (UInt4)(buffer[IPv4.IHL_OFFSET] & 0x0F);
            if (ihl != 0x05)
            {
                SimulationOnly(() =>
                {
                    LOGGER.log.Debug($"Odd size of IPv4 Packet: IHL: {(byte)ihl}");
                });
            }

            // Get total length
            ushort total_len = (ushort)((buffer[IPv4.TOTAL_LENGTH_OFFSET_0] << 0x08)
                                       | buffer[IPv4.TOTAL_LENGTH_OFFSET_1]);

            // Get protocol
            byte protocol = buffer[IPv4.PROTOCOL_OFFSET];

            // Flags
            byte flags = (byte)((buffer[IPv4.FLAGS_OFFSET] >> 0x05) & 0x0E);
            ushort fragment_offset = (ushort)((buffer[IPv4.FRAGMENT_OFFSET_OFFSET_0] << 0x08
                                        | buffer[IPv4.FRAGMENT_OFFSET_OFFSET_1])
                                        & IPv4.FRAGMENT_OFFSET_MASK);
            if ((flags & (byte)IPv4.Flags.MF) != 0x00)
            {
                SimulationOnly(() =>
               {
                   LOGGER.log.Error($"IP packet fragmentation not supported!");
               });
            }

            // Destionation address
            uint dst_address = (uint)(buffer[IPv4.SRC_ADDRESS_OFFSET_0] << 0x18)
                            | (uint)(buffer[IPv4.SRC_ADDRESS_OFFSET_1] << 0x10)
                            | (uint)(buffer[IPv4.SRC_ADDRESS_OFFSET_2] << 0x08)
                            | (uint)(buffer[IPv4.SRC_ADDRESS_OFFSET_3]);

            // TODO: implement check if packet for us

            // Source Address
            uint src_address = (uint)(buffer[IPv4.SRC_ADDRESS_OFFSET_0] << 0x18)
                            | (uint)(buffer[IPv4.SRC_ADDRESS_OFFSET_1] << 0x10)
                            | (uint)(buffer[IPv4.SRC_ADDRESS_OFFSET_2] << 0x08)
                            | (uint)(buffer[IPv4.SRC_ADDRESS_OFFSET_3]);
            // TODO: Check(?)




            // Calculate pseudoheader checksum
            // TODO: Can we reuse variables? (acc would be handly here, saving us
            // a whole 8 bytes!)
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
            propagatePacket(id, protocol, fragment_offset, pseudoheader_checksum);
        }


        private void propagatePacket(uint id, byte protocol, uint fragment_offset = 0,
                                        ushort pseudoheader_checksum = 0x00)
        {
            segmentBus.ip_id = id;
            segmentBus.fragment_offset = fragment_offset;
            segmentBus.protocol = protocol;
            segmentBus.pseudoheader_checksum = pseudoheader_checksum;
        }
    }
}