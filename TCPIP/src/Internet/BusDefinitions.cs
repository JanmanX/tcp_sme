using SME;

namespace TCPIP
{
    public partial class Internet
    {
        public interface DatagramBus : IBus
        {
            [InitialValue(long.MaxValue)]
            long frame_number { get; set; }

            [InitialValue(0x00)]
            ushort type { get; set; }

        }
    }
}