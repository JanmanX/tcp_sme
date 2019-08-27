using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    [ClockedProcess]
    public partial class InternetOut: StateProcess
    {
        // STATIC
        public readonly uint IP_ADDRESS_0 = 0xC0A82B01; // 192.168.43.1
        public readonly uint IP_ADDRESS_1 = 0x00;

        // PacketOut
        // packetInComputeProducerControlBusOut
        [InputBus]
        public BufferProducerControlBus packetOutBufferProducerControlBusIn;
        [InputBus]
        public PacketOut.ReadBus packetOutWriteBus;
        [OutputBus]
        public ConsumerControlBus packetOutBufferConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();

        // LinkOut
        [OutputBus]
        public ComputeProducerControlBus frameOutComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [OutputBus]
        public readonly FrameOut.WriteBus frameOutWriteBus = Scope.CreateBus<FrameOut.WriteBus >();
        [InputBus]
        public ConsumerControlBus frameOutComputeConsumerControlBusIn;


        // Local
        private ushort ip_id = 0;

        private const uint BUFFER_SIZE = 100;
        private byte[] buffer = new byte[BUFFER_SIZE];

        private long frame_number = long.MinValue;
        private uint header_offset = 0;
        private bool sending = false;

        public InternetOut()
        {
        }

        protected async override Task OnTickAsync()
        {
            frameOutComputeProducerControlBusOut.valid = false;

            // Set/Reset all values
            uint bytes_passed = 0;
            byte protocol = 0;
            uint dst_ip = 0;
            ip_id++;

            // Set ready and wait for first byte
            packetOutBufferConsumerControlBusOut.ready = true;
            do
            {
                await ClockAsync();
            }
            while(packetOutBufferProducerControlBusIn.valid == false);

            // Get primary information about the packet
            protocol = packetOutWriteBus.protocol;
            dst_ip = (uint)packetOutWriteBus.ip_dst_addr_0;

            // Set frame specific data such as framenumber and ethertype
            frameOutWriteBus.frame_number = packetOutWriteBus.frame_number;
            frameOutWriteBus.ethertype = (ushort)EthernetIIFrame.EtherType.IPv4;

            // Pass data while packetOut has valid data
            while(packetOutBufferProducerControlBusIn.valid) {
                frameOutComputeProducerControlBusOut.valid = true;
                frameOutComputeProducerControlBusOut.bytes_left = 1;
                frameOutWriteBus.data = packetOutWriteBus.data;
                frameOutWriteBus.addr = IPv4.HEADER_SIZE + bytes_passed;
        		bytes_passed++;

                if(packetOutBufferProducerControlBusIn.bytes_left == 0)
                {
                    packetOutBufferConsumerControlBusOut.ready = false;
                    await ClockAsync();
                    break;
                } else {
                    await ClockAsync();
                }
            }

            // We do not want to receive more bytes at the moment
            packetOutBufferConsumerControlBusOut.ready = false;

            // Build header and send it
            uint header_size = SetupPacket((ushort)bytes_passed, ip_id, protocol, IP_ADDRESS_0, dst_ip);

            // Send the header
            uint idx = 0;
            while(idx < header_size && bytes_passed > 0)
            {
                frameOutComputeProducerControlBusOut.valid = true;
                frameOutComputeProducerControlBusOut.bytes_left = 1;

                frameOutWriteBus.data = buffer[idx];
                frameOutWriteBus.addr = idx;
                idx++;

                // Indicate if last byte
                if(idx >= header_size) {
                    frameOutComputeProducerControlBusOut.bytes_left = 0;
                }



                await ClockAsync();
            }


            frameOutComputeProducerControlBusOut.valid = false;
        }

        // Creates the packet inside the buffer, and returns its data offset
        private uint SetupPacket(ushort data_length, ushort ip_id, byte protocol, uint src_ip, uint dst_ip)
        {
            // Set the version number
            buffer[IPv4.VERSION_OFFSET] = IPv4.VERSION << 4;
            // Set the IHL part
            buffer[IPv4.IHL_OFFSET] |= 0x05;
            // Set differentiated services(XXX: zero since we do not support it)
            buffer[IPv4.DIFFERENTIATED_SERVICES_OFFSET] = 0x00;
            // Set the packet length
            buffer[IPv4.TOTAL_LENGTH_OFFSET_0] = (byte)(data_length + IPv4.HEADER_SIZE >> 0x08);
            buffer[IPv4.TOTAL_LENGTH_OFFSET_1] = (byte)(data_length + IPv4.HEADER_SIZE & 0xFF);
            // Set the identifier
            buffer[IPv4.ID_OFFSET_0] = (byte)(ip_id & 0xFF);
            buffer[IPv4.ID_OFFSET_1] = (byte)(ip_id >> 0x08);
            //id_identifier++; // Increment identifier for next packet (XXX:No support for ip segmentation yet)
            // Set the flags (and offset) to 0 (XXX: fragmentation support maybe?)
            buffer[IPv4.FLAGS_OFFSET] = 0x00;
            // Set time to live to 64 (XXX: save in config somewhere?)
            buffer[IPv4.TTL_OFFSET] = 64;
            // Set the protocol
            buffer[IPv4.PROTOCOL_OFFSET] = protocol;
            // Set the ip
            SetIP(IPv4.SRC_ADDRESS_OFFSET_0, src_ip);
            SetIP(IPv4.DST_ADDRESS_OFFSET_0, dst_ip);
            // Calculate the checksum, and set it
            ushort checksum = ChecksumBuffer(0, IPv4.HEADER_SIZE, (int)IPv4.CHECKSUM_OFFSET_0);
            buffer[IPv4.CHECKSUM_OFFSET_0] = (byte)(checksum & 0xFF);
            buffer[IPv4.CHECKSUM_OFFSET_1] = (byte)(checksum >> 0x08);

            return IPv4.HEADER_SIZE;
        }

        // Set an IPv4 from an ulong
        private void SetIP(uint offset, ulong ip)
        {
            buffer[offset++] = (byte)((ip & IPv4.ADDRESS_MASK_0) >> 0x18);
            buffer[offset++] = (byte)((ip & IPv4.ADDRESS_MASK_1) >>  0x10);
            buffer[offset++] = (byte)((ip & IPv4.ADDRESS_MASK_2) >>  0x8);
            buffer[offset] = (byte)(ip & IPv4.ADDRESS_MASK_3);
        }


        private ushort ChecksumBuffer(uint offset, uint len, int exclude = -1)
        {
            ulong acc = 0x00;

            // XXX: Odd lengths might cause trouble!!!
            for (uint i = offset; i < len; i = i + 2)
            {
                if (i != exclude)
                {
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
