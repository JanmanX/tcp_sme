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

        public interface DatagramBusInControl : IBus
        {
            [InitialValue(true)]
            bool ready { get; set; } // Indicates whether the DataGramBusIn can be written to

            [InitialValue(false)]
            bool skip { get; set; } // Whether we want the next frame
        }

        public interface DatagramBusOut : IBus
        {
            [InitialValue(long.MaxValue)]
            long frame_number { get; set; }

            [InitialValue(0x00)]
            byte data { get; set; }

            [InitialValue(0x00)]
            uint addr { get; set; }

            [InitialValue(0x00)]
            ushort ethertype { get; set; }

            [InitialValue(0x00)]
            int data_length { get; set; } // the size we are writing currently
        }
    }
}