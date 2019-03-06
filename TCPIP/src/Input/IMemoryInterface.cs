using SME;

namespace TCPIP
{
    public interface IMemoryInterface : IBus
    {
        [InitialValue(false)]
        bool WriteEnabled { get; set; }
        [InitialValue(false)]
        bool ReadEnabled { get; set; }

        uint ReadAddr { get; set; }
        uint WriteAddr { get; set; }

        byte WriteValue { get; set; }
        byte ReadValue { get; set; }

    }
}