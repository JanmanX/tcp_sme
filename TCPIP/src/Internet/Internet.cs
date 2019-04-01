using System;
using System.Threading.Tasks;
using SME;
using SME.Components;

namespace TCPIP
{
    [ClockedProcess]
    public partial class Internet : Process
    {
        [InputBus]
        private readonly Internet.DatagramBus datagramBus;

        [InputBus]
        private readonly TrueDualPortMemory<byte>.IControlB controlB;

        [OutputBus]
        public readonly Transport.SegmentBus segmentBus = Scope.CreateBus<Transport.SegmentBus>();


        // Local storage
        private byte[] buffer = new byte[36];
        private uint buffer_index = 0x00;
        private uint prev_packet_number = 0x00;

        public Internet(Internet.DatagramBus datagramBus,
                        TrueDualPortMemory<byte>.IControlB controlB)
        {
            this.datagramBus = datagramBus ?? throw new ArgumentNullException(nameof(datagramBus));
            this.controlB = controlB ?? throw new ArgumentNullException(nameof(controlB));
        }

        public override async Task Run()
        {
            while (true)
            {

            }
        }

        protected void parseIPv4()
        {

        }
    }

}