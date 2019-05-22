using System;
using SME;


namespace TCPIP
{
    class DataInReader  : SimpleProcess
    {
        [InputBus]
        private readonly Transport.DataInBus dataInBus;


        private byte[] buffer = new byte[256];
        private uint idx = 0x00; 


        public DataInReader(Transport.DataInBus dataInBus) {
            this.dataInBus = dataInBus ?? throw new ArgumentNullException(nameof(dataInBus));
        }


        protected override void OnTick()
        {
            if(dataInBus.finished || dataInBus.invalidate)
            {
                idx = 0;
                Console.WriteLine("Data received: ");
                for (int i = 0; i < 256; i++) {
                    Console.Write($"{(char)buffer[i]}");
                }
            } else {
                if(dataInBus.valid && idx < buffer.Length) {
                    buffer[idx++] = dataInBus.data;
                }
            }
        }
    }

}