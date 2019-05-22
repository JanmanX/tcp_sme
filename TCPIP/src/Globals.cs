using System;

using SME;
using SME.VHDL;

namespace TCPIP
{
    // General bus used for control between
    public interface ControlConsumer : IBus
        {
            [InitialValue(false)]
            bool ready { get; set; } // Are we ready to receive data in the pipeline?
        }

  public interface ControlProducer: IBus
        {
            [InitialValue(true)]
            bool available { get; set; } // Stuff is ready to be sent, possibly waiting for ControlConsumer ready
            bool valid { get; set; } // The current data on the data bus is ready to be read now

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