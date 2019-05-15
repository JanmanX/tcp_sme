using SME;

namespace TCPIP
{
    public partial class PacketOut
    {
        public interface BufferIn: IBus
        {
            [InitialValue(false)]
            bool active { get; set; }
            long number { get; set; } // Increments so we can distinguish between new packages
            ulong ip_dst_addr_0 { get; set; }
            ulong ip_dst_addr_1 { get; set; }
            ulong ip_src_addr_0 { get; set; }
            ulong ip_src_addr_1 { get; set; }
            int total_len { get; set; } // the size we are writing currently
            byte data { get; set; } // The data needed
        }
        public interface BufferOut : IBus
        {
            [InitialValue(long.MaxValue)]
            long frame_number { get; set; }

            bool active { get; set; }
            
            [InitialValue(0x00)]
            byte data { get; set; }

            [InitialValue(0x00)]
            ushort type { get; set; }
        }
        public interface BufferSignalTransport : IBus
        {
            [InitialValue(0)]
            uint spaceLeft { get; set; }
            [InitialValue(0)]
            bool ready { get; set; }

        }
    }
}