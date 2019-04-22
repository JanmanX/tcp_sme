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
        private const uint NUM_PCB = 10;
        private PCB[] pcbs = new PCB[NUM_PCB];

        private const int BUFFER_SIZE = 100;
        private byte[] buffer = new byte[BUFFER_SIZE];
        private bool read = false; // Indicates whether process should read into buffer
        private uint byte_idx = 0x00;
        private byte protocol = 0x00;
        private uint ip_id = 0x00;

        public Transport(Transport.SegmentBusIn segmentBusIn)
        {
            this.segmentBusIn = segmentBusIn ?? throw new ArgumentNullException(nameof(segmentBusIn));
        }


        protected override void OnTick()
        {
            // If new segment received, reset
            if (segmentBusIn.ip_id != ip_id)
            {
                ip_id = segmentBusIn.ip_id;
                byte_idx = 0x00;
                protocol = segmentBusIn.protocol;
                read = true;
            }

            if (read && byte_idx < BUFFER_SIZE)
            {
                buffer[byte_idx++] = segmentBusIn.data;
            }

            // Processing
            switch (protocol)
            {
                case (byte)IPv4.Protocol.TCP:
                    // End of header, start parsing
                    if (byte_idx == TCP.HEADER_SIZE)
                    {
                        read = false;
                        ParseTCP();
                    }
                    break;
            }
        }
    }
}