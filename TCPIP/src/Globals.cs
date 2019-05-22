using System;

using SME;
using SME.VHDL;

namespace TCPIP
{
    // General bus used for control between
    public interface ControlBus : IBus
        {
            [InitialValue(true)]
            bool ready { get; set; } // Are we ready to receive data in the pipeline?
        }

    enum LayerProcessState : byte
    {
        Reading,
        Writing,
        Passing,
    };


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

    }

}