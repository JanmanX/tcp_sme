using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    public partial class InternetIn : SimpleProcess
    {
        ////////// Datagram in from L proccess
        [InputBus]
        public Internet.DatagramBusIn datagramBusIn;
        [InputBus]
        public ComputeProducerControlBus datagramBusInComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus datagramBusInComputeConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();


        //////////// IP packet to packet in
        [OutputBus]
        public Memory.InternetPacketBus packetIn = Scope.CreateBus<Memory.InternetPacketBus>();
        [OutputBus]
        public ComputeProducerControlBus packetInComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [InputBus]
        public ConsumerControlBus packetInComputeConsumerControlBusIn;

        //////////// IP packet to packet out
        [OutputBus]
        public Memory.InternetPacketBus packetOut = Scope.CreateBus<Memory.InternetPacketBus>();
        [OutputBus]
        public ComputeProducerControlBus packetOutComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [InputBus]
        public ConsumerControlBus packetOutComputeConsumerControlBusIn;

        // Local storage
        private struct SegmentData
        {
            public bool valid;  // Valid is needed to indicate valid byte *for the next clock*!
                                // Please do not hate me for this ...
            public ushort type;
            public long frame_number;
            public ushort offset; // The byte offset for data that needs to be passed through
            public uint size; // The size of the packet XXX: total size or only packet size? no offset
            public SegmentDataIP ip;
        }
        // Local storage for IP information
        public struct SegmentDataIP
        {
            public uint id;
            public byte protocol;
            public uint fragment_offset;
            public ushort pseudoheader_checksum;
            public ushort total_len;
            public ulong src_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong src_addr_1; // Upper 8 bytes of IP addr
            public ulong dst_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong dst_addr_1; // Upper 8 bytes of IP addr
        };


        // Structure used to store information about the segment, updates the
        // bus at the start of every clock cycle
        private SegmentData cur_segment_data;

        private LayerProcessState state = LayerProcessState.Reading;

        private const uint BUFFER_IN_SIZE = 100;
        private byte[] buffer_in = new byte[BUFFER_IN_SIZE]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..
        private uint idx_in = 0x00;

        private const uint BUFFER_OUT_SIZE = 100;
        private byte[] buffer_out = new byte[BUFFER_OUT_SIZE];
        private uint idx_out = 0x00;
        private uint write_len = 0x00;


        public InternetIn()
        {
            // Initialize
            StartReading();
        }


        private void Write()
        {
            if (idx_out > write_len)
            {
                packetOutComputeProducerControlBusOut.valid = false;

                StartReading();
            }
            else
            {
                LOGGER.INFO($"Writing from {idx_out} data: {buffer_in[idx_out]:X2}");
                // Send the general data to the buffer
                packetOutComputeProducerControlBusOut.valid = true;
                packetOutComputeProducerControlBusOut.bytes_left = 1;
                packetOut.ip_dst_addr_0 = cur_segment_data.ip.dst_addr_0;
                packetOut.ip_dst_addr_1 = cur_segment_data.ip.dst_addr_1;
                packetOut.ip_src_addr_0 = cur_segment_data.ip.src_addr_0;
                packetOut.ip_src_addr_1 = cur_segment_data.ip.src_addr_1;
                packetOut.data = buffer_in[idx_out++];
                packetOut.data_length = cur_segment_data.ip.total_len - cur_segment_data.offset;
            }
        }

        private void Read()
        {
            // If new frame
            if (datagramBusIn.frame_number != cur_segment_data.frame_number)
            {
                StartReading(); // Resets values

                cur_segment_data.frame_number = datagramBusIn.frame_number;
                cur_segment_data.type = datagramBusIn.type;
            }

            if (idx_in < buffer_in.Length)
            {
                buffer_in[idx_in++] = datagramBusIn.data;

                // Processing
                switch (cur_segment_data.type)
                {
                    case (ushort)EthernetIIFrame.EtherType.IPv4:
                        // End of header, start parsing
                        if (idx_in == IPv4.HEADER_SIZE)
                        {
                            // Parse the ip packet
                            ParseIPv4();
                            // XXX: Detect if there are optionals on IPv4 and only pass when that have been read
                            StartPassing();

                        }

                        break;

                    default:
                        LOGGER.ERROR($"Segment type not defined: {cur_segment_data.type}");
                        break;
                }
            }
        }

        private void Pass()
        {
            // If current segment is valid but we have a new frame
            if (cur_segment_data.valid &&
                datagramBusIn.frame_number != cur_segment_data.frame_number)
            {
                Read(); // Resets values in the beginning
            }
            else
            {
                LOGGER.INFO("Passing");
                // Pass values
                packetIn.ip_id = cur_segment_data.ip.id;
                packetIn.fragment_offset = cur_segment_data.ip.fragment_offset;
                packetIn.ip_protocol = cur_segment_data.ip.protocol;
                packetIn.ip_src_addr_0 = cur_segment_data.ip.src_addr_0;
                packetIn.ip_src_addr_1 = cur_segment_data.ip.src_addr_1;
                packetIn.ip_dst_addr_0 = cur_segment_data.ip.dst_addr_0;
                packetIn.ip_dst_addr_1 = cur_segment_data.ip.dst_addr_1;
                packetIn.data_length = (int)(cur_segment_data.ip.total_len - IPv4.HEADER_SIZE);
                packetIn.frame_number = cur_segment_data.frame_number;


                // go go go
                cur_segment_data.valid = true;
                packetInComputeProducerControlBusOut.valid = true;
                packetIn.data = datagramBusIn.data;
            }
        }

        protected override void OnTick()
        {
            switch (state)
            {
                case LayerProcessState.Writing:
                    Write();
                    break;

                case LayerProcessState.Reading:
                    Read();
                    break;

                case LayerProcessState.Passing:
                    Pass();
                    break;
            }
        }



        // Save the ip segment to the current local data storage
        private void SaveSegmentDataIp(uint id, byte protocol, ushort total_len,
                                    uint fragment_offset,
                                    ushort pseudoheader_checksum,
                                    ulong dst_addr_0, ulong src_addr_0,
                                    ulong dst_addr_1 = 0, ulong src_addr_1 = 0)

        {
            cur_segment_data.ip.id = id;
            cur_segment_data.ip.protocol = protocol;
            cur_segment_data.ip.total_len = total_len;

            cur_segment_data.ip.fragment_offset = fragment_offset;
            cur_segment_data.ip.pseudoheader_checksum = pseudoheader_checksum;

            cur_segment_data.ip.dst_addr_0 = dst_addr_0;
            cur_segment_data.ip.dst_addr_1 = dst_addr_1;

            cur_segment_data.ip.src_addr_0 = src_addr_0;
            cur_segment_data.ip.src_addr_1 = src_addr_1;
        }


        // calculates the checksum from buffer_out[offset] to buffer_out[len]
        // Exclude a 16 bit step if necessary, to calculate a senders checksum
        private ushort ChecksumBufferIn(uint offset, uint len, int exclude = -1)
        {
            ulong acc = 0x00;

            // XXX: Odd lengths might cause trouble!!!
            for (uint i = offset; i < len; i = i + 2)
            {
                if (i != exclude)
                {
                    acc += (ulong)((buffer_in[i] << 0x08
                                 | buffer_in[i + 1]));
                }
            }
            // Add carry bits and do one-complement on 16 bits
            // Overflow  can max happen twice
            acc = ((acc & 0xFFFF) + (acc >> 0x10));
            return (ushort)~((acc & 0xFFFF) + (acc >> 0x10));
        }


        // Start or resume reading
        void StartReading()
        {
            state = LayerProcessState.Reading;

            // Reset various values
            idx_in = 0x00;

            cur_segment_data.ip.id = 0;
            cur_segment_data.ip.protocol = 0;
            cur_segment_data.frame_number = long.MinValue;
            cur_segment_data.ip.fragment_offset = 0;
            cur_segment_data.ip.pseudoheader_checksum = 0;
            cur_segment_data.ip.src_addr_0 = 0;
            cur_segment_data.ip.src_addr_1 = 0;
            cur_segment_data.ip.dst_addr_0 = 0;
            cur_segment_data.ip.dst_addr_1 = 0;

            // When reading, the data is not valid
            cur_segment_data.valid = false;
            packetInComputeProducerControlBusOut.valid = false;
            packetInComputeProducerControlBusOut.valid = false;
            // We are ready to receive data
            datagramBusInComputeConsumerControlBusOut.ready = true;
        }

        void StartWriting(ushort last_byte)
        {
            state = LayerProcessState.Writing;

            idx_out = cur_segment_data.offset;
            write_len = last_byte;


            datagramBusInComputeConsumerControlBusOut.ready = false;
        }

        private void StartPassing()
        {
            state = LayerProcessState.Passing;
        }
    }
}
