using System;
using System.Threading.Tasks;
using SME;
using SME.Components;

namespace TCPIP
{
    public partial class Internet : SimpleProcess
    {
        [InputBus]
        private readonly Internet.DatagramBus datagramBus;

        [InputBus]
        private readonly TrueDualPortMemory<byte>.IControlA controlA;

        [OutputBus]
        public readonly Transport.SegmentBus segmentBus = Scope.CreateBus<Transport.SegmentBus>();


        // Local storage
        private byte[] buffer = new byte[36]; // XXX: Set fixed size to longest header. Currently IPv4 without opt..
        private bool read = false; // Indicates whether process should read into buffer
        private uint byte_idx = 0x00;
        private ushort type = 0x00;
        private long cur_frame_number = long.MaxValue;

        public Internet(Internet.DatagramBus datagramBus,
                        TrueDualPortMemory<byte>.IControlA controlA)
        {
            this.datagramBus = datagramBus ?? throw new ArgumentNullException(nameof(datagramBus));
            this.controlA = controlA ?? throw new ArgumentNullException(nameof(controlA));
        }

        protected override void OnTick()
        {
            // If new frame
            if (datagramBus.frame_number != cur_frame_number)
            {
                // Reset values
                read = true;
                cur_frame_number = datagramBus.frame_number;
                type = datagramBus.type;
                byte_idx = 0x00;
            }

            // Save data and process
            if (read && byte_idx < buffer.Length)
            {
                buffer[byte_idx++] = controlA.Data;

                // Processing
                switch (type)
                {
                    case (ushort)EtherType.IPv4:
                        // End of header, start parsing
                        if (byte_idx == 0x14)
                        {
                            read = false;
                            parseIPv4();
                        }
                        break;
                }
            }

            // Signal next process?
            segmentBus.Addr = 0x01;
        }

        protected void parseIPv4()
        {
            SimulationOnly(() =>
            {
                Logger.log.Warn($"Parsing IPv4 packet because {type:X}");
            });

        }
    }

}