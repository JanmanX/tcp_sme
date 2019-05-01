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
        private struct SegmentData
        {
            public uint id;
            public ushort protocol;
            public uint fragment_offset;
            public long frame_number;
            public ushort pseudoheader_checksum;

            public ulong ip_addr_0; // Lower 8 bytes of IP addr (lower 4 bytes used in this field on IPv4)
            public ulong ip_addr_1; // Upper 8 bytes of IP addr
        };
        // Structure used to store information about the segment, updates the 
        // bus at the start of every clock cycle
        private SegmentData cur_segment_data;

        private LayerProcessState state = LayerProcessState.Reading;

        private ushort type = 0x00;

        private const uint BUFFER_SIZE = 100;
        private byte[] buffer_in = new byte[BUFFER_SIZE]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..
        private uint idx_in = 0x00;

        private byte[] buffer_out = new byte[BUFFER_SIZE]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..

        private uint idx_out = 0x00;
        private uint write_len = 0x00;


        public InternetReader(Internet.DatagramBusIn datagramBusIn)
        {
            this.datagramBusIn = datagramBusIn ?? throw new ArgumentNullException(nameof(datagramBusIn));

            cur_segment_data.id = 0;
            cur_segment_data.protocol = 0;
            cur_segment_data.fragment_offset = 0;
            cur_segment_data.frame_number = 0;
            cur_segment_data.pseudoheader_checksum = 0;
            cur_segment_data.ip_addr_0 = 0;
            cur_segment_data.ip_addr_1 = 0;
        }


        private void Write()
        {
            if (idx_out > write_len)
            {
                StartReading();
            }
            else
            {
                segmentBusIn.data = buffer_out[idx_out++];
            }
        }

        private void Read()
        {
            // If new frame
            if (datagramBusIn.frame_number != cur_segment_data.frame_number)
            {
                StartReading(); // Resets values

                cur_segment_data.frame_number = datagramBusIn.frame_number;
                type = datagramBusIn.type;
            }

            if (idx_in < buffer_in.Length)
            {
                buffer_in[idx_in++] = datagramBusIn.data;

                // Processing
                switch (type)
                {
                    case (ushort)EtherType.IPv4:
                        // End of header, start parsing
                        if (idx_in == IPv4.HEADER_SIZE)
                        {
                            ParseIPv4();
                        }
                        break;
                }
            }
        }

        private void Pass()
        {
            // If new frame
            if (datagramBusIn.frame_number != cur_segment_data.id)
            {
                StartReading(); // Resets values
            }
            else
            {
                // Pass values
                segmentBusIn.ip_id = cur_segment_data.id;
                segmentBusIn.fragment_offset = cur_segment_data.fragment_offset;
                segmentBusIn.protocol = cur_segment_data.protocol;
                segmentBusIn.pseudoheader_checksum = cur_segment_data.pseudoheader_checksum;
                segmentBusIn.ip_addr_0 = cur_segment_data.ip_addr_0;
                segmentBusIn.ip_addr_1 = cur_segment_data.ip_addr_1;

                // go go go
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

        private void ClearBufferOut()
        {
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                buffer_out[i] = 0x00;
            }
        }

        // calculates the checksum from buffer_out[0] to buffer_out[len]
        private ushort ChecksumBufferOut(uint len)
        {
            ulong acc = 0x00;

            // XXX: Odd lengths might cause trouble!!!
            for (uint i = 0; i < len; i = i + 2)
            {
                acc += (ulong)((buffer_out[i] << 0x08
                                 | buffer_out[i + 1]));

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

            cur_segment_data.id = 0;
            cur_segment_data.protocol = 0;
            cur_segment_data.frame_number = 0;
            cur_segment_data.fragment_offset = 0;
            cur_segment_data.pseudoheader_checksum = 0;
            cur_segment_data.ip_addr_0 = 0;
            cur_segment_data.ip_addr_1 = 0;

            // Data currently in segmentBusIn not valid
            segmentBusIn.valid = false;

            // We are ready to receive data
            datagramBusInControl.ready = true;

            // Do not skip
            datagramBusInControl.skip = false;
        }


        void StartWriting(ushort len, byte protocol, DataMode data_mode)
        {
            state = LayerProcessState.Writing;

            // We are going to write
            idx_out = 0x00;
            write_len = len;
            segmentBusIn.data_mode = (byte)data_mode;
            segmentBusIn.protocol = protocol;

            // We are not ready to receive new packets until this one is sent
            datagramBusInControl.ready = false;
        }


        private void StartPassing(uint id, byte protocol, uint fragment_offset = 0,
                                        ushort pseudoheader_checksum = 0x00,
                                        ulong ip_addr_0 = 0, ulong ip_addr_1 = 0)

        {
            cur_segment_data.id = id;
            cur_segment_data.protocol = protocol;
            cur_segment_data.fragment_offset = fragment_offset;
            cur_segment_data.pseudoheader_checksum = pseudoheader_checksum;
            cur_segment_data.ip_addr_0 = ip_addr_0;
            cur_segment_data.ip_addr_1 = ip_addr_1;

            state = LayerProcessState.Passing;
        }
    }
}