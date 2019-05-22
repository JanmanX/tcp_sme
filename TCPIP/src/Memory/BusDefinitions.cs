using SME;

namespace TCPIP
{
    public partial class PacketOut
    {
        public interface PacketOutBus: IBus
        {
            [InitialValue(false)]
            bool active { get; set; }
            long frame_number { get; set; } // Increments so we can distinguish between new packages
            byte ip_protocol { get; set; }
            ulong ip_dst_addr_0 { get; set; }
            ulong ip_dst_addr_1 { get; set; }
            ulong ip_src_addr_0 { get; set; }
            ulong ip_src_addr_1 { get; set; }
            int data_length { get; set; } // the size we are writing currently
            byte data { get; set; } // The data needed
        }
        // public interface BufferOut : IBus
        // {
        //     [InitialValue(false)]
        //     bool active { get; set; }
        //     [InitialValue(long.MaxValue)]
        //     long frame_number { get; set; }

        //     [InitialValue(0x00)]
        //     byte data { get; set; }

        //     [InitialValue(0x00)]
        //     ushort type { get; set; }
        // }
        public interface BufferOutControl : IBus
        {
            [InitialValue(false)]
            bool ready { get; set; }

        }
    }
}