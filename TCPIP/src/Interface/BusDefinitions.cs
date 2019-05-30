using SME;

namespace TCPIP
{
    public partial class Interface
    {
        // Busses to user
        public interface InterfaceBus : IBus
        {
            [InitialValue(false)]
            bool valid { get; set; }

            [InitialValue(0x00)]
            byte interface_function { get; set; }

            InterfaceData request { get; set; }
        }

        public interface InterfaceControlBus : IBus
        {
            [InitialValue(false)]
            bool valid { get; set; }

            [InitialValue(0)]
            byte exit_status { get; set; }

            [InitialValue(0x00)]
            byte interface_function { get; set; }

            InterfaceData request { get; set; }

            InterfaceData response { get; set; }
        }
    }
}