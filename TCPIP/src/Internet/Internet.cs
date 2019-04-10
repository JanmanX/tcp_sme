using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    public partial class Internet : SimpleProcess
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
        private byte[] buffer = new byte[36]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..
        private bool read = false; // Indicates whether process should read into buffer
        private uint byte_idx = 0x00;
        private ushort type = 0x00;
        private long cur_frame_number = long.MaxValue;
        private UInt4 ihl;
        public Internet(Internet.DatagramBus datagramBus,
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
                        if (byte_idx == 0x14)
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
            SimulationOnly(() =>
            {
                Logger.log.Debug($"Parsing IPv4 packet of type 0x{type:X}");
            });


            // Checksum
            ulong acc = 0x00;
            for (uint i = 0; i < 0x14; i = i + 2)
            {
                acc += (ulong)((buffer[i] << 0x08
                                 | buffer[i + 1]));

            }
            // Add carry bits and do one-complement on 16 bits
            // Overflow  can max happen twice
            ushort calculated_checksum = (ushort)((acc & 0xFFFF) + (acc >> 0x10));
            calculated_checksum = (ushort)~((calculated_checksum & 0xFFFF) + (calculated_checksum >> 0x10));
            if (calculated_checksum != 0x00)
            {
                SimulationOnly(() =>
                {
                    Logger.log.Warn($"Invalid checksum: 0x{calculated_checksum:X}");
                });
            }


            // Get ID
            ushort id = (ushort)((buffer[IPv4Header.ID_OFFSET_0] << 0x08)
                                       | buffer[IPv4Header.ID_OFFSET_1]);

            Console.WriteLine($"IPv4 ID: 0x{id:X}");

            // Check version
            if ((buffer[IPv4Header.VERSION_OFFSET] >> 0x04) != IPv4Header.VERSION)
            {
                SimulationOnly(() =>
               {
                   Logger.log.Warn($"Uknown IPv4 version {(buffer[IPv4Header.VERSION_OFFSET] & 0x0F):X}");
               });
            }

            // Get Internet Header Length
            ihl = (UInt4)(buffer[IPv4Header.IHL_OFFSET] & 0x0F);
            if (ihl != 0x05)
            {
                SimulationOnly(() =>
                {
                    Logger.log.Debug($"Odd size of IPv4 Packet: IHL: {(byte)ihl}");
                });
            }

            // Get total length
            ushort total_len = (ushort)((buffer[IPv4Header.TOTAL_LENGTH_OFFSET_0] << 0x08)
                                       | buffer[IPv4Header.TOTAL_LENGTH_OFFSET_1]);

            // Get protocol
            byte protocol = buffer[IPv4Header.PROTOCOL_OFFSET];

            // Flags
            byte flags = (byte)((buffer[IPv4Header.FLAGS_OFFSET] >> 0x05) & 0x0E);
            ushort fragment_offset = (ushort)((buffer[IPv4Header.FRAGMENT_OFFSET_OFFSET_0] << 0x08
                                        | buffer[IPv4Header.FRAGMENT_OFFSET_OFFSET_1])
                                        & IPv4Header.FRAGMENT_OFFSET_MASK);
            if ((flags & (byte)IPv4Header.Flags.MF) != 0x00)
            {
                SimulationOnly(() =>
               {
                   Logger.log.Error($"IP packet fragmentation not supported!");
               });
            }

            // Propagate
            propagatePacket(id, fragment_offset);
        }

        private void propagatePacket(uint id, uint fragment_offset = 0)
        {
            segmentBus.ip_id = id;
            segmentBus.fragment_offset = fragment_offset;
        }

    }

}