using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    [ClockedProcess]
    public partial class InternetOut: SimpleProcess
    {
        [InputBus]
        public readonly PacketOut.PacketOutBus bus_in;

        [OutputBus]
        public readonly Internet.DatagramBusOut datagramBusOut;

        [InputBus]
        public readonly ControlBus bus_in_control;

        private ushort id_identifier = 0;

        private const uint BUFFER_SIZE = 100;
        private byte[] buffer = new byte[BUFFER_SIZE];
        int idx = 0;

        private long frame_number = long.MinValue;
        private uint header_offset = 0;
        private bool sending = false;

        public InternetOut(Internet.DatagramBusOut datagramBusOut, PacketOut.PacketOutBus bus_in, ControlBus bus_in_control)
        {
            this.datagramBusOut = datagramBusOut ?? throw new ArgumentNullException(nameof(datagramBusOut));
            this.bus_in = bus_in ?? throw new ArgumentNullException(nameof(bus_in));
            this.bus_in_control = bus_in_control ?? throw new ArgumentNullException(nameof(bus_in_control));
        }

        protected override void OnTick()
        {

            // If the bus is active, we are getting data, gather it
            if(bus_in.active){

                if(bus_in.frame_number != frame_number)
                {
                    frame_number = bus_in.frame_number;
                    header_offset = SetupPacket();

                }
                // Use the header offset, and send in data
                buffer[header_offset + idx] = bus_in.data;

//                 LOGGER.TRACE($@"\
// offset:{header_offset} \
// idx:{idx} \
// data:{bus_in.data:X2} \
// calc:{header_offset + idx} \
// data_length:{bus_in.data_length}");
                // Stop one clock beforehand, so we don't start next packet before everything has propagated
                if(idx == bus_in.data_length - 1){
                    LOGGER.WARN($"We need to indicate that we do not want more data next clock");
                    bus_in_control.ready = false;
                }

                // We can now send out stuff
                sending = true;
            }
            if(sending){
                LOGGER.INFO($"sending: idx: {idx} :: {buffer[idx]:X2}");
                datagramBusOut.data = buffer[idx];
                datagramBusOut.frame_number = frame_number;
                datagramBusOut.type = (ushort)EthernetIIFrame.EtherType.IPv4;
                // Increment the pointer
                idx++;
                // Everything has been sent, we now set us as ready, and stop sending
                if(idx == bus_in.data_length + header_offset){
                    LOGGER.WARN($"Everything has been sent,Making bus ready");
                    sending = false;
                    idx = 0;
                    bus_in_control.ready = true;

                }
            }

        }
        // Creates the packet inside the buffer, and returns its data offset
        private uint SetupPacket(){
            // Set the version number
            buffer[IPv4.VERSION_OFFSET] = IPv4.VERSION << 4;
            // Set the IHL part
            buffer[IPv4.IHL_OFFSET] |= 0x05;
            // Set differentiated services(XXX: zero since we do not support it)
            buffer[IPv4.DIFFERENTIATED_SERVICES_OFFSET] = 0x00;
            // Set the packet length
            buffer[IPv4.TOTAL_LENGTH_OFFSET_0] = (byte)(bus_in.data_length + IPv4.HEADER_SIZE >> 0x08);
            buffer[IPv4.TOTAL_LENGTH_OFFSET_1] = (byte)(bus_in.data_length + IPv4.HEADER_SIZE & 0xFF);
            // Set the identifier
            buffer[IPv4.ID_OFFSET_0] = (byte)(id_identifier & 0xFF);
            buffer[IPv4.ID_OFFSET_1] = (byte)(id_identifier >> 0x08);
            id_identifier++; // Increment identifier for next packet (XXX:No support for ip segmentation yet)
            // Set the flags (and offset) to 0 (XXX: fragmentation support maybe?)
            buffer[IPv4.FLAGS_OFFSET] = 0x00;
            // Set time to live to 64 (XXX: save in config somewhere?)
            buffer[IPv4.TTL_OFFSET] = 64;
            // Set the protocol
            buffer[IPv4.PROTOCOL_OFFSET] = bus_in.ip_protocol;
            // Set the ip
            SetIP(IPv4.SRC_ADDRESS_OFFSET_0,bus_in.ip_src_addr_0);
            SetIP(IPv4.DST_ADDRESS_OFFSET_0,bus_in.ip_dst_addr_0);
            // Calculate the checksum, and set it
            ushort checksum = ChecksumBuffer(0,IPv4.HEADER_SIZE,(int)IPv4.CHECKSUM_OFFSET_0);
            buffer[IPv4.CHECKSUM_OFFSET_0] = (byte)(checksum & 0xFF);
            buffer[IPv4.CHECKSUM_OFFSET_1] = (byte)(checksum >> 0x08);

            return IPv4.HEADER_SIZE;
        }

        // Set an IPv4 from an ulong
        private void SetIP(uint offset, ulong ip){
            buffer[offset++] = (byte)(ip & IPv4.ADDRESS_MASK_0);
            buffer[offset++] = (byte)((ip & IPv4.ADDRESS_MASK_1) >> 0x08);
            buffer[offset++] = (byte)((ip & IPv4.ADDRESS_MASK_2) >> 0x10);
            buffer[offset]   = (byte)((ip & IPv4.ADDRESS_MASK_3) >> 0x18);
        }


        private ushort ChecksumBuffer(uint offset, uint len, int exclude = -1)
        {
            ulong acc = 0x00;

            // XXX: Odd lengths might cause trouble!!!
            for (uint i = offset; i < len; i = i + 2)
            {
                if (i != exclude){
                    acc += (ulong)((buffer[i] << 0x08
                                 | buffer[i + 1]));
                }
            }
            // Add carry bits and do one-complement on 16 bits
            // Overflow  can max happen twice
            acc = ((acc & 0xFFFF) + (acc >> 0x10));
            return (ushort)~((acc & 0xFFFF) + (acc >> 0x10));
        }

    }
}
