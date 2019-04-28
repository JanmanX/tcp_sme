using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    public partial class NetworkReader : SimpleProcess
    {
        [InputBus]
        private readonly Network.FrameBusIn frameBusIn;

        [OutputBus]
        public readonly Internet.DatagramBusIn datagramBusIn = Scope.CreateBus<Internet.DatagramBusIn>();

        // Local storage
        private uint cur_frame_number = UInt32.MaxValue;
        private uint byte_idx = 0x00; // Keeps track of number of bytes read in current frame
        private ushort type = 0x00;


        public NetworkReader(Network.FrameBusIn frameBusIn)
        {
            this.frameBusIn = frameBusIn ?? throw new System.ArgumentNullException(nameof(frameBusIn));
        }

        protected override void OnTick()
        {
            // If new frame
            if (frameBusIn.frame_number != cur_frame_number)
            {
                // Reset values
                byte_idx = 0x00;
                type = 0x00;

                // Update frame number
                cur_frame_number = frameBusIn.frame_number;
            }

            // Unrolled for sake for FPGA space
            if (byte_idx == 0x0C) // upper type byte
            {
                type = (ushort)(frameBusIn.data << 0x08);
            }
            else if (byte_idx == 0x0D) // lower type byte
            {
                type |= (ushort)(frameBusIn.data);
            }
            else if (byte_idx == 0x0E) // End of ethernet_frame
            {
                // Update the datagramBusIn so the next stage can start
                datagramBusIn.frame_number = cur_frame_number;
                datagramBusIn.type = type;

                LOGGER.DEBUG($"Propagating control to Internet with type: '0x{type:X}'");
            }


            // Increment
            byte_idx++;
        }
    }

}