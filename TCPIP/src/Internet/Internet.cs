using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    public partial class Internet
    {
        // CONFIG
        // TODO: Find a better place to put this?
        public const uint IPv4_ADDRESS = 0x00;
        public const uint INSTANCES = 0x04;

        // Output busses
        public readonly Transport.SegmentBus segmentBus = Scope.CreateBus<Transport.SegmentBus>();


        // Collections of processes
        private List<IPv4> list_ipv4 = new List<IPv4>();


        public Internet(Internet.DatagramBus datagramBus,
                        TrueDualPortMemory<byte>.IControlA controlA,
                        uint instances = INSTANCES)
        {
           if (datagramBus is null) {
                throw new ArgumentNullException(nameof(datagramBus));
           }
           if (controlA is null) {
                throw new ArgumentNullException(nameof(controlA));
           }

            // Create output 
            // Create IPv4 processes
            for (uint i = 0; i < instances; i++)
            {
                list_ipv4.Add(new IPv4(datagramBus, controlA, segmentBus, i));
            }
        }

        private class IPv4: SimpleProcess 
        {
            public readonly uint id;

            [InputBus]
            private readonly Internet.DatagramBus datagramBus;

            [InputBus]
            private readonly TrueDualPortMemory<byte>.IControlA controlA;

            [OutputBus]
            public readonly Transport.SegmentBus segmentBus; // = Scope.CreateBus<Transport.SegmentBus>();


            // Local storage
            private byte[] buffer = new byte[36]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..
            private bool read = false; // Indicates whether process should read into buffer
            private uint byte_idx = 0x00;
            private ushort type = 0x00;
            private long cur_frame_number = long.MaxValue;
            private UInt4 ihl;


            public IPv4(Internet.DatagramBus datagramBus,
                            TrueDualPortMemory<byte>.IControlA controlA,
                            Transport.SegmentBus segmentBus,
                            uint id)
            {
                    this.datagramBus = datagramBus;
                    this.controlA = controlA;
                    this.segmentBus = segmentBus;
                    this.id = id;
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

                // Signal next process?
                segmentBus.Addr = 0x01;
            }

            protected void parseIPv4()
            {
                SimulationOnly(() =>
                {
                    Logger.log.Debug($"Hello I am IPv4Process id={id}\nParsing IPv4 packet of type 0x{type:X}");
                });

                // XXX: Remove once fragmentation is implemented
                if(id != 0x00) {
                    return;
                }


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

                // Calculate size of packet
                ushort data_length = (ushort)(((buffer[IPv4Header.TOTAL_LENGTH_OFFSET_0] << 0x08)
                                           | buffer[IPv4Header.TOTAL_LENGTH_OFFSET_1])
                                    - (ihl * 4));


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
                propagatePacket(protocol, data_length);
            }

            private void propagatePacket(byte protocol, ushort data_length)
            {
                segmentBus.Addr = 0x1337;

                SimulationOnly(() => {
                    Console.WriteLine("propagating");
                });
            }

        }

    }

}