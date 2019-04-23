using SME;

namespace TCPIP
{
    public partial class NetworkDataBuffer
    {
        public interface NetworkDataBufferBus : IBus 
        {
            [InitialValue(0x00)]
            byte data { get; set; }

            [InitialValue(0x00)]
            uint ip_id { get; set; }

            [InitialValue(0x00)]
            ushort fragment_offset { get; set; }

            [InitialValue(0x00)]
            uint sequence_number { get; set; }

       }
    }
}