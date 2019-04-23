using System;

using SME;
using SME.VHDL;

namespace TCPIP
{
    enum LayerProcessState : byte
    {
        Reading,
        Writing,
        Passing,
    };
    // Indicates whether the data in the bus is to be sent
    // 0 - not for sending
    // 1 - send to Internet layer
    // 2 - send to Network layer 
    // 3 - send raw to network device (debugging also?)
    public enum DataMode : byte
    {
        NO_SEND = 0,
        SEND_INTERNET = 1,
        SEND_NETWORK = 2,
        SEND_RAW = 3
    }


    // Structure containing all necessary information about a packet for sending
    struct PacketInfo
    {
        byte protocol;  // Transport layer protocol
        byte type;      // Internet layer type
        uint ip_addr;   // IP address
    }


}