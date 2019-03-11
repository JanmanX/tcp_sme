using SME;

namespace TCPIP
{
    public interface IPacketAddrAnnouncer: IBus
    {
        uint Addr { get; set; }
    }
}