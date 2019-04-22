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
        public readonly Internet.DatagramBusInControl datagramBusInControl = Scope.CreateBus<Internet.DatagramBusInControl>();

        [OutputBus]
        public readonly Transport.SegmentBusIn segmentBusIn = Scope.CreateBus<Transport.SegmentBusIn>();

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

                // We are ready to receive data
                datagramBusInControl.ready = true;

                // Do not skip
                datagramBusInControl.skip = false;
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

            // Writing


            // Pass data
        }

        private void propagatePacket(uint id, byte protocol, uint fragment_offset = 0,
                                        ushort pseudoheader_checksum = 0x00)
        {
            segmentBusIn.ip_id = id;
            segmentBusIn.fragment_offset = fragment_offset;
            segmentBusIn.protocol = protocol;
            segmentBusIn.pseudoheader_checksum = pseudoheader_checksum;
        }
    }
}