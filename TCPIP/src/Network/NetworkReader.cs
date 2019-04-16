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
        private readonly Network.FrameBus frameBus;

        [OutputBus]
        public readonly Internet.DatagramBus datagramBus = Scope.CreateBus<Internet.DatagramBus>();

        // Local storage
        private uint cur_frame_number = UInt32.MaxValue;
        private uint byte_idx = 0x00; // Keeps track of number of bytes read in current frame
        private ushort type = 0x00;


        public NetworkReader(Network.FrameBus frameBus)
        {
            this.frameBus = frameBus ?? throw new System.ArgumentNullException(nameof(frameBus));
        }

        protected override void OnTick()
        {
            // If new frame
            if (frameBus.frame_number != cur_frame_number)
            {
                // Reset values
                byte_idx = 0x00;
                type = 0x00;

                // Update frame number 
                cur_frame_number = frameBus.frame_number;
            }

            // Unrolled for sake for FPGA space
            if (byte_idx == 0x0C) // upper type byte
            {
                type = (ushort)(frameBus.data << 0x08);
            }
            else if (byte_idx == 0x0D) // lower type byte
            {
                type |= (ushort)(frameBus.data);
            }
            else if (byte_idx == 0x0E) // End of ethernet_frame
            {
                // Update the datagramBus so the next stage can start
                datagramBus.frame_number = cur_frame_number;
                datagramBus.type = type;

                SimulationOnly(() =>
                {
                    LOGGER.log.Debug($"Propagating control to Internet with type: '0x{type:X}'");
                });
            }


            // Increment
            byte_idx++;
        }
    }

}