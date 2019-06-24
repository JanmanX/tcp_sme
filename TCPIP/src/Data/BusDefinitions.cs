using System;
using SME;

namespace TCPIP
{
    public interface DataOutReadBus : IBus
    {
        [InitialValue(-1)]
        int socket { get; set; }   // Socket of the belonging connection, or -1 if unknown

        [InitialValue(0x00)]
        byte data { get; set; }
    }


    public interface DataInWriteBus : IBus
    {
        [InitialValue(false)]
        ushort bytes_left { get; set; }    // Marks packet is fully written and valid

        [InitialValue(0)]
        uint tcp_seq { get; set; }

        [InitialValue(-1)]
        int socket { get; set; }   // Socket of the belonging connection, or -1 if unknown

        [InitialValue(0x00)]
        byte data { get; set; }

        [InitialValue(false)]
        bool invalidate { get; set; }
    }


}