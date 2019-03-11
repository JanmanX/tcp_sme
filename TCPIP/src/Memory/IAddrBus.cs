using SME;

namespace TCPIP
{
    public interface IAddrBus: IBus
    {
        uint Addr { get; set; }
    }
}