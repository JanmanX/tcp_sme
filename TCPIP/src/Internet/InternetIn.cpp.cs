using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    public partial class InternetIn : SimpleProcess
    {
        // CONFIG
        // TODO: Find a better place to put this?
        public uint IP_ADDRESS_0 = 0x0CA82B01; // 192.168.43.1
        public uint IP_ADDRESS_1 = 0x00;

        [InputBus]
        private readonly Internet.DatagramBusIn datagramBusIn;

        [OutputBus]
        public readonly Internet.DatagramBusInControl datagramBusInControl = Scope.CreateBus<Internet.DatagramBusInControl>();

        [OutputBus]
        public readonly Transport.SegmentBusIn segmentBusIn = Scope.CreateBus<Transport.SegmentBusIn>();

        [OutputBus]
        private readonly PacketOut.BufferIn bufferInternet;

        // Local storage
        private struct SegmentData
        {
            public bool valid;  // Valid is needed to indicate valid byte *for the next clock*!
                                // Please do not hate me for this ...
            public ushort type;
            public long frame_number;
            public ushort offset; // The byte offset for data that needs to be passed through
            public SegmentDataIP ip;
            public SegmentDataICMP icmp;
            public SendType send_type;
        };
        // Local storage for IP information
        private struct SegmentDataIP
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
        // Local storage for ICMP
        private struct SegmentDataICMP
        {
            public byte type;
            public byte code;

            public ushort checksum;
            public ushort identifier;
            public ushort sequence_number;
        };

        // Structure used to store information about the segment, updates the
        // bus at the start of every clock cycle
        private SegmentData cur_segment_data;

        private LayerProcessState state = LayerProcessState.Reading;

        // The state in which the parser can be.
        private enum ParsingState{
            IPv4Optionals, // The IPv4 Packet contains optionals
            ICMP, // The packet is ICMP
            Pass, // no additional parsing needed, pass to the next element
            PreParsing, // The initial parsing, before we have enough information for additional parsing
        }
        // Enum specifying how to construct the outgoing packet
        private enum SendType
        {
            IMCP_Echo
        }

        private ParsingState parsing_state = ParsingState.PreParsing;


        private const uint BUFFER_SIZE = 100;
        private byte[] buffer_in = new byte[BUFFER_SIZE]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..
        private uint idx_in = 0x00;

        // XXX : Depricated?
        private byte[] buffer_out = new byte[BUFFER_SIZE]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..

        private uint idx_out = 0x00;
        private uint write_len = 0x00;


        public InternetIn(Internet.DatagramBusIn datagramBusIn,PacketOut.BufferIn bufferInternet)
        {
            this.datagramBusIn = datagramBusIn ?? throw new ArgumentNullException(nameof(datagramBusIn));
            this.bufferInternet = bufferInternet;
            // Initialize
            StartReading();
       }


        private void Write()
        {
            if (idx_out > write_len)
            {
                bufferInternet.active = false;
                StartReading();
            }
            else
            {
                LOGGER.INFO($"Writing from {idx_out} data: {buffer_in[idx_out]:X2}");
                // Send the general data to the buffer
                bufferInternet.active = true;
                bufferInternet.ip_dst_addr_0 = cur_segment_data.ip.dst_addr_0;
                bufferInternet.ip_dst_addr_1 = cur_segment_data.ip.dst_addr_1;
                bufferInternet.ip_src_addr_0 = cur_segment_data.ip.src_addr_0;
                bufferInternet.ip_src_addr_1 = cur_segment_data.ip.src_addr_1;
                bufferInternet.number = cur_segment_data.frame_number;
                bufferInternet.data = buffer_in[idx_out++];
                bufferInternet.total_len = cur_segment_data.ip.total_len - cur_segment_data.offset;
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
                parsing_state = ParsingState.PreParsing;
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
                            // If the IP packet contains more informaiton, we can set it here
                            // and go into a new parsing state
                            if (cur_segment_data.ip.protocol == (byte)IPv4.Protocol.ICMP)
                            {
                                parsing_state = ParsingState.ICMP;
                            }
                            else
                            {
                                parsing_state = ParsingState.Pass;
                            }
                        }
                        // We test what parsing state we are in, and handle accordingly
                        switch(parsing_state)
                        {
                            case ParsingState.PreParsing:
                                // We do not have the information yet to parse additional stuff. Do nothing
                                break;
                            case ParsingState.ICMP:
                                // Wait for the full length, then parse
                                if (idx_in == cur_segment_data.ip.total_len)
                                {
                                    LOGGER.DEBUG("Parsing ICMP");
                                    ParseICMP((ushort)IPv4.HEADER_SIZE); // If there exist IPv4 options, set length here
                                    // Should this message be responded too
                                    if(ResponseICMP()){
                                        LOGGER.INFO("Responding to ICMP packet");
                                        // Subtract by one to get the last byte
                                        StartWriting((ushort)(cur_segment_data.ip.total_len - 1));
                                    }else{
                                        // Reset maybe?
                                    }
                                }
                                break;
                            case ParsingState.Pass:
                                // The data should go to the Transport layer, pass it on
                                StartPassing();
                                break;
                        default:
                            LOGGER.ERROR($"Undefined parsing state: {parsing_state}");
                            break;
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
                //LOGGER.INFO("Passing");
                // Pass values
                segmentBusIn.ip_id = cur_segment_data.ip.id;
                segmentBusIn.fragment_offset = cur_segment_data.ip.fragment_offset;
                segmentBusIn.protocol = cur_segment_data.ip.protocol;
                segmentBusIn.pseudoheader_checksum = cur_segment_data.ip.pseudoheader_checksum;
                segmentBusIn.src_ip_addr_0 = cur_segment_data.ip.src_addr_0;
                segmentBusIn.src_ip_addr_1 = cur_segment_data.ip.src_addr_0;


                // go go go
                cur_segment_data.valid = true;
                segmentBusIn.valid = true;
                segmentBusIn.data = datagramBusIn.data;
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


        // Save the ICMP data to the current local data storage
        private void SaveSegmentDataICMP(byte type,byte code,
                                    ushort identifier,ushort sequence_number,
                                    ushort checksum,ushort offset)
        {
            cur_segment_data.icmp.type = type;
            cur_segment_data.icmp.code = code;
            cur_segment_data.icmp.identifier = identifier;
            cur_segment_data.icmp.sequence_number = sequence_number;
            cur_segment_data.icmp.checksum = checksum;
            cur_segment_data.offset = offset;


        }
        // Save the ip segment to the current local data storage
        private void SaveSegmentDataIp(uint id, byte protocol, ushort total_len,
                                    uint fragment_offset,
                                    ushort pseudoheader_checksum,
                                    ulong dst_addr_0, ulong src_addr_0,
                                    ulong dst_addr_1 = 0, ulong src_addr_1 = 0 )

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
        private void ClearBufferOut()
        {
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                buffer_out[i] = 0x00;
            }
        }

        // calculates the checksum from buffer_out[offset] to buffer_out[len]
        // Exclude a 16 bit step if necessary, to calculate a senders checksum
        private ushort ChecksumBufferIn(uint offset, uint len, int exclude = -1)
        {
            ulong acc = 0x00;

            // XXX: Odd lengths might cause trouble!!!
            for (uint i = offset; i < len; i = i + 2)
            {
                if (i != exclude){
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

            // XXX also reset ICMP


            // Data currently in segmentBusIn not valid
            cur_segment_data.valid = false;
            segmentBusIn.valid = false;

            // We are ready to receive data
            datagramBusInControl.ready = true;

            // Do not skip
            datagramBusInControl.skip = false;
        }

        // TODO:
        void StartWriting(ushort last_byte)
        {
            state = LayerProcessState.Writing;

            // We are going to write
            idx_out = cur_segment_data.offset;
            write_len = last_byte;
            //segmentBusIn.protocol = protocol;

            // We are not ready to receive new packets until this one is sent
            datagramBusInControl.ready = false;
        }

        private void StartPassing()

        {
            state = LayerProcessState.Passing;
        }
    }
}
