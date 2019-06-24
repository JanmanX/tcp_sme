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
        public Internet.DatagramBusIn datagramInBus;
        [InputBus]
        public BufferProducerControlBus datagramBusInBufferProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus datagramBusInBufferConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();


        //////////// IP packet to packet in
        [OutputBus]
        public PacketIn.WriteBus packetInBus = Scope.CreateBus<PacketIn.WriteBus>();
        [OutputBus]
        public ComputeProducerControlBus packetInComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [InputBus]
        public ConsumerControlBus packetInComputeConsumerControlBusIn;

        //////////// IP packet to packet out
        [OutputBus]
        public PacketOut.WriteBus packetOut = Scope.CreateBus<PacketOut.WriteBus>();
        [OutputBus]
        public ComputeProducerControlBus packetOutComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [InputBus]
        public ConsumerControlBus packetOutComputeConsumerControlBusIn;

        // Local storage
        private struct SegmentData
        {
            public bool valid;
            public ushort type;
            public long frame_number;
            public ushort offset; // The byte offset for data that needs to be passed through
            public uint size; // Total size of the segment
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


        enum InternetInState : byte
        {
            Idle,
            Read,
            Write,
            Pass,
            Finish, // Transition state
        };
        private InternetInState state = InternetInState.Idle;

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
            //cur_segment_data.frame_number = long.MinValue;
        }


        protected override void OnTick()
        {
            switch (state)
            {
                case InternetInState.Finish:
                    StartIdle();
                    // Csharp do not permit fall through.. but can be forced with goto. clever language design!
                    goto case InternetInState.Idle;
                // Fall through!
                case InternetInState.Idle:
                    Idle();
                    break;

                case InternetInState.Write:
                    Write();
                    break;

                case InternetInState.Read:
                    Read();
                    break;

                case InternetInState.Pass:
                    Pass();
                    break;
            }
        }

        // Intermediate state for transition without resetting busses
        private void Finish()
        {
            state = InternetInState.Finish;
        }

        private void StartIdle()
        {
            state = InternetInState.Idle;

            ResetAllBusses();
        }

        private void Idle()
        {
            if (datagramBusInBufferProducerControlBusIn.valid)
            {
                StartReading();
            }
        }

        void StartWriting(ushort last_byte)
        {
            StartIdle();

            Logging.log.Warn($"WRITING CURRENTLY NOT SUPPORTED ON INTERNET_IN");
            state = InternetInState.Write;

            idx_out = cur_segment_data.offset;
            write_len = last_byte;

            ResetAllBusses();
        }


        private void Write()
        {
            Logging.log.Info($"Writing from {idx_out} data: {buffer_in[idx_out]:X2}");

            // Send the general data to the buffer
            packetOutComputeProducerControlBusOut.valid = true;
            packetOutComputeProducerControlBusOut.bytes_left = 1;
            packetOut.ip_dst_addr_0 = cur_segment_data.ip.dst_addr_0;
            packetOut.ip_dst_addr_1 = cur_segment_data.ip.dst_addr_1;
            packetOut.ip_src_addr_0 = cur_segment_data.ip.src_addr_0;
            packetOut.ip_src_addr_1 = cur_segment_data.ip.src_addr_1;
            packetOut.data = buffer_in[idx_out++];
            packetOut.data_length = cur_segment_data.ip.total_len - cur_segment_data.offset;

            if (idx_out > write_len)
            {
                Finish();
            }

        }

        void StartReading()
        {
            state = InternetInState.Read;

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

            ResetAllBusses();

            //datagramBusInBufferConsumerControlBusOut.ready = true;
        }


        private void Read()
        {
            // If the data is not valid, we are not ready
            if(datagramBusInBufferProducerControlBusIn.valid == false)
            {
                Logging.log.Warn("Stuff not ready on reading bus");
                datagramBusInBufferConsumerControlBusOut.ready = false;
                StartIdle();
                return;
            }

            if (idx_in < buffer_in.Length)
            {
                datagramBusInBufferConsumerControlBusOut.ready = true;

                buffer_in[idx_in++] = datagramInBus.data;
                cur_segment_data.frame_number = datagramInBus.frame_number;
                Logging.log.Trace($"Receiving frame: {datagramInBus.frame_number} data: 0x{datagramInBus.data:X2} idx_in: {idx_in}");
                // Processing
                switch (datagramInBus.type)
                {
                    case (ushort)EthernetIIFrame.EtherType.IPv4:
                        // End of header, start parsing
                        if (idx_in == IPv4.HEADER_SIZE)
                        {
                            // Parse the ip packet
                            Logging.log.Info("Parsing IPv4 packet");
                            ParseIPv4();
                        }
                        break;

                    default:
                        Logging.log.Error($"Segment type not defined: {datagramInBus.type}");
                        break;
                }

            }
        }

        private void Pass()
        {
            if (datagramBusInBufferProducerControlBusIn.valid == false)
            {
                Logging.log.Error("Passing stopped prematurely!");
                StartIdle();
                return;
            }



            // Pass values
            packetInBus.ip_id = cur_segment_data.ip.id;
            packetInBus.fragment_offset = cur_segment_data.ip.fragment_offset;
            packetInBus.protocol = cur_segment_data.ip.protocol;
            packetInBus.ip_src_addr_0 = cur_segment_data.ip.src_addr_0;
            packetInBus.ip_src_addr_1 = cur_segment_data.ip.src_addr_1;
            packetInBus.ip_dst_addr_0 = cur_segment_data.ip.dst_addr_0;
            packetInBus.ip_dst_addr_1 = cur_segment_data.ip.dst_addr_1;
            packetInBus.data_length = (int)cur_segment_data.size;
            packetInBus.frame_number = cur_segment_data.frame_number;
            packetInBus.data = datagramInBus.data;
            datagramBusInBufferConsumerControlBusOut.ready = true;
            // go go go
            cur_segment_data.valid = true;
            packetInComputeProducerControlBusOut.valid = true;
            packetInComputeProducerControlBusOut.bytes_left = 1;


            // Increment number of bytes sent, and mark last byte if necessary
            cur_segment_data.offset++;
            Logging.log.Trace($"Passing offset: {cur_segment_data.offset} total: {cur_segment_data.size} data: 0x{datagramInBus.data:X2}");
            if (cur_segment_data.offset == cur_segment_data.size)
            {
                Logging.log.Info("Passing done!");
                datagramBusInBufferConsumerControlBusOut.ready = true;
                packetInComputeProducerControlBusOut.bytes_left = 0;
                Finish();
            }
        }
        private void StartPassing(uint id, byte protocol, ushort pass_length,
                                    uint fragment_offset,
                                    ushort pseudoheader_checksum,
                                    ulong dst_addr_0, ulong src_addr_0,
                                    ulong dst_addr_1 = 0, ulong src_addr_1 = 0)
        {
            state = InternetInState.Pass;

            cur_segment_data.offset = 0;
            cur_segment_data.ip.id = id;
            cur_segment_data.ip.protocol = protocol;
            cur_segment_data.size = pass_length;

            cur_segment_data.ip.fragment_offset = fragment_offset;
            cur_segment_data.ip.pseudoheader_checksum = pseudoheader_checksum;

            cur_segment_data.ip.dst_addr_0 = dst_addr_0;
            cur_segment_data.ip.dst_addr_1 = dst_addr_1;

            cur_segment_data.ip.src_addr_0 = src_addr_0;
            cur_segment_data.ip.src_addr_1 = src_addr_1;

            Logging.log.Info("Passing started!");

            ResetAllBusses();

            // We know that the next byte is for passing so to limit interrupts, we set it here already
            datagramBusInBufferConsumerControlBusOut.ready = true;
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


        private void ResetAllBusses()
        {
            datagramBusInBufferConsumerControlBusOut.ready = false;

            packetInComputeProducerControlBusOut.valid = false;
            //packetInComputeProducerControlBusOut.available = false;

            packetOutComputeProducerControlBusOut.valid = false;
            //packetOutComputeProducerControlBusOut.available = false;
        }

    }
}
