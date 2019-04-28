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
       private  LayerProcessState state = LayerProcessState.Reading;

        private const uint BUFFER_SIZE = 100;
        private byte[] buffer_in = new byte[BUFFER_SIZE]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..
        private uint idx_in = 0x00;

        private byte[] buffer_out = new byte[BUFFER_SIZE]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..

        private uint idx_out = 0x00;
        private uint write_len = 0x00;

        private ushort type = 0x00;
        private long cur_frame_number = long.MaxValue;


        public InternetReader(Internet.DatagramBusIn datagramBusIn)
        {
            this.datagramBusIn = datagramBusIn ?? throw new ArgumentNullException(nameof(datagramBusIn));
        }


        private void Write()
        {
            segmentBusIn.data = buffer_out[idx_out++];

            // If all bytes have been written, start reading again
            if (idx_out == write_len)
            {
                StartReading();
            }

        }

        private void Read()
        {
            // If new frame
            if (datagramBusIn.frame_number != cur_frame_number)
            {
                StartReading(); // Resets values

                cur_frame_number = datagramBusIn.frame_number;
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

                    case (byte)IPv4.Protocol.ICMP:
                        if (idx_in == ICMP.PACKET_SIZE)
                        {
                            ParseICMP();
                        
                        break;
                }
            }
        }

        private void Pass()
        {
            segmentBusIn.ip_id = id;
            segmentBusIn.fragment_offset = fragment_offset;
            segmentBusIn.protocol = protocol;
            segmentBusIn.pseudoheader_checksum = pseudoheader_checksum;
 

            // If new frame
            if (datagramBusIn.frame_number != cur_frame_number)
            {
                StartReading(); // Resets values
                return;
            }

            // Pass values
            segmentBusIn.data = datagramBusIn.data;
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

        private void PropagatePacket(uint id, byte protocol, uint fragment_offset = 0,
                                        ushort pseudoheader_checksum = 0x00)
        {
             state = LayerProcessState.Passing;

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
            segmentBusIn.fragment_offset = 0x00;
            segmentBusIn.ip_addr = 0x00;
            segmentBusIn.ip_id = 0x00;
            segmentBusIn.protocol = 0x00;
            segmentBusIn.pseudoheader_checksum = 0x00;


            // We are ready to receive data
            datagramBusInControl.ready = true;

            // Do not skip
            datagramBusInControl.skip = false;
        }


        void StartSending(ushort len, byte protocol, DataMode data_mode)
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

        void StartPassing()
        {
            state = LayerProcessState.Passing;

            segmentBusIn.data_mode = (byte)DataMode.NO_SEND;
        }
    }
}