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
        private readonly Internet.DatagramBusIn datagramBusIn;

        [OutputBus]
        public readonly Transport.SegmentBus segmentBus = Scope.CreateBus<Transport.SegmentBus>();

        // Local storage
        private byte[] buffer = new byte[50]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..
        private bool read = false; // Indicates whether process should read into buffer
        private uint byte_idx = 0x00;
        private ushort type = 0x00;
        private long cur_frame_number = long.MaxValue;


        public InternetReader(Internet.DatagramBusIn datagramBusIn)
        {
            this.datagramBusIn = datagramBusIn ?? throw new ArgumentNullException(nameof(datagramBusIn));
        }

        protected override void OnTick()
        {
            // If new frame
            if (datagramBusIn.frame_number != cur_frame_number)
            {
                // Reset values
                read = true;
                cur_frame_number = datagramBusIn.frame_number;
                type = datagramBusIn.type;
                byte_idx = 0x00;
            }

            // Save data and process
            if (read && byte_idx < buffer.Length)
            {
                buffer[byte_idx++] = datagramBusIn.data;

                // Processing
                switch (type)
                {
                    case (ushort)EtherType.IPv4:
                        // End of header, start parsing
                        if (byte_idx == IPv4.HEADER_SIZE)
                        {
                            read = false;
                            ParseIPv4();
                        }
                        break;
                }
            }

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