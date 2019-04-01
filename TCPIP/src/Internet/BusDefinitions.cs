using SME;

namespace TCPIP
{
    public partial class Internet
    {
        public interface DatagramBus : IBus
        {
            [InitialValue(0x00)]
            long frame_number { get; set; }

            [InitialValue(0x00)]
            ushort type { get; set; }

        }


    }
}