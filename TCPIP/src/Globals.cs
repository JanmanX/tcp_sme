using System;

using SME;
using SME.VHDL;

namespace TCPIP
{
    // General bus used for control between
    public interface ConsumerControlBus : IBus
    {
        [InitialValue(false)]
        bool ready { get; set; }
    }

    public interface ComputeProducerControlBus : IBus
    {
        [InitialValue(false)]
        bool available { get; set; }

        [InitialValue(false)]
        bool valid { get; set; }

        // Optional
        [InitialValue(0)]
        uint bytes_left { get; set; }
    }

    public interface BufferProducerControlBus : IBus
    {
        [InitialValue(false)]
        bool available { get; set; }

        [InitialValue(false)]
        bool valid { get; set; }

        [InitialValue(0)]
        uint bytes_left { get; set; }
    }



    // Structure containing all necessary information about a packet for sending
    struct PacketInfo
    {
        byte protocol;  // Transport layer protocol
        byte type;      // Internet layer type
        uint ip_addr;   // IP address
    }


    // Return codes used in communications between processes
    enum ExitStatus : byte
    {
        OK = 0,
        EOK = 0, // however you prefer

        EINVAL = 22,

        ENOSPC = 28,

        EPROTONOSUPPORT = 93,
    }

}