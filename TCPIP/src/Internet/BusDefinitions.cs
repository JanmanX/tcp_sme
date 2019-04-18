using SME;

namespace TCPIP
{
    public partial class Internet
    {
        public interface DatagramBusIn : IBus
        {
            [InitialValue(long.MaxValue)]
            long frame_number { get; set; }

            [InitialValue(0x00)]
            byte data { get; set; }

            [InitialValue(0x00)]
            ushort type { get; set; }

        }
    }
}