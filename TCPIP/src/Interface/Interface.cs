using System;
using SME;

namespace TCPIP
{
    public enum InterfaceFunction : byte
    {
        INVALID = 0,
        BIND = 1,
        LISTEN = 2,
        CONNECT = 3,
        ACCEPT = 4,
        SEND = 5,
        RECV = 6,
        CLOSE = 7,
        // ...
        OPEN = 255,
    }

    public struct InterfaceData
    {
        public int socket;
        public uint ip;
        public byte protocol;
        public ushort port;
    }
}