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
        private readonly Transport.SegmentBus segmentBus;

        [OutputBus]
        public readonly NetworkDataBuffer.NetworkDataBufferBus networkDataBufferBus
                    = Scope.CreateBus<NetworkDataBuffer.NetworkDataBufferBus>();

        // Local variables
        private const uint NUM_PCB = 10;
        private PCB[] pcbs = new PCB[NUM_PCB];

        private byte[] buffer = new byte[100];
        private bool read = false; // Indicates whether process should read into buffer
        private uint byte_idx = 0x00;
        private byte protocol = 0x00;
        private uint ip_id = 0x00;

        public Transport(Transport.SegmentBus segmentBus)
        {
            this.segmentBus = segmentBus ?? throw new ArgumentNullException(nameof(segmentBus));
        }


        protected override void OnTick()
        {
            // If new segment received, reset
            if (segmentBus.ip_id != ip_id)
            {
                ip_id = segmentBus.ip_id;
                byte_idx = 0x00;
                protocol = segmentBus.protocol;
                read = true;
            }

            if (read)
            {
                buffer[byte_idx++] = segmentBus.data;
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