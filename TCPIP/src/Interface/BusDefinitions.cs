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

            InterfaceData request { get; set; }

            /* 
                        [InitialValue(0x00)]
                        byte interfaceFunction { get; set; }

                        [InitialValue(-1)]
                        int socket { get; set; }

                        [InitialValue(0x00)]
                        byte data { get; set; }

            */
        }

        public interface InterfaceBusControl : IBus
        {
            [InitialValue(false)]
            bool valid { get; set; }

            [InitialValue(0)]
            byte exit_status { get; set; }


            InterfaceData request { get; set; }

            InterfaceData response { get; set; }
        }
    }
}