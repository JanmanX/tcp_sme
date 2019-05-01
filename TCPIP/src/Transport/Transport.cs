using System;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    [ClockedProcess]
    public partial class Transport : SimpleProcess
    {
        [InputBus]
        private readonly Transport.SegmentBusIn segmentBusIn;

        [OutputBus]
        public readonly SegmentBusInControl segmentBusInControl
                    = Scope.CreateBus<SegmentBusInControl>();


        [OutputBus]
        public readonly SegmentBusOut segmentBusOut = Scope.CreateBus<SegmentBusOut>();

        [OutputBus]
        public readonly NetworkDataBuffer.NetworkDataBufferBus networkDataBufferBus
                    = Scope.CreateBus<NetworkDataBuffer.NetworkDataBufferBus>();



        // Local variables
        private LayerProcessState state = LayerProcessState.Reading;

        private const uint NUM_PCB = 10;
        private PCB[] pcbs = new PCB[NUM_PCB];

        private const int BUFFER_SIZE = 100;
        private byte[] buffer_in = new byte[BUFFER_SIZE];
        private uint idx_in = 0x00;
        private bool read = false; // Indicates whether process should read into buffer

        private byte[] buffer_out = new byte[BUFFER_SIZE];
        private uint idx_out = 0x00;
        private bool write = false; // Inidicates whetehr process is writing from local buffer

        private byte protocol = 0x00;
        private uint ip_id = 0x00;

        public Transport(Transport.SegmentBusIn segmentBusIn)
        {
            this.segmentBusIn = segmentBusIn ?? throw new ArgumentNullException(nameof(segmentBusIn));
        }


        void Read()
        {
            if (segmentBusIn.data_mode == (byte)DataMode.NO_SEND)
            {
                // Parse
            }
            else
            {
                segmentBusOut.data = segmentBusIn.data;
                segmentBusOut.data_mode = segmentBusIn.data_mode;
                segmentBusOut.ip_addr = segmentBusIn.ip_addr;
                segmentBusOut.protocol = segmentBusIn.protocol;
            }


            // If new segment received, reset
            if (segmentBusIn.ip_id != ip_id)
            {
                ip_id = segmentBusIn.ip_id;
                idx_in = 0x00;
                protocol = segmentBusIn.protocol;
                read = true;
            }

            if (read && idx_in < BUFFER_SIZE)
            {
                buffer_in[idx_in++] = segmentBusIn.data;

                // Processing
                switch (protocol)
                {
                    case (byte)IPv4.Protocol.TCP:
                        // End of header, start parsing
                        if (idx_in == TCP.HEADER_SIZE)
                        {
                            read = false;
                            ParseTCP();
                        }
                        break;
                }
            }
        }

        void Write()
        {

        }

        protected override void OnTick()
        {
            Read();

            Write();

        }

    }
}