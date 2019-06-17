using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;

using SME;
using SME.Components;

namespace TCPIP
{
    public class DataOutWriter : Process
    {
        [InputBus]
        public ConsumerControlBus consumerControlBus;

        [OutputBus]
        public readonly BufferProducerControlBus bufferProducerControlBus = Scope.CreateBus<BufferProducerControlBus>();

        [OutputBus]
        public readonly DataOut.ReadBus dataOut = Scope.CreateBus<DataOut.ReadBus>();

        // Simulation fields
        private String data = "AAAA\nBBBB\nCCCC\nDDDD\nEEEE\nFFFF\nXXXX\nYYYY\nZZZZ\n";
        private byte[] buffer;
        private int socket = 1;

        public DataOutWriter(int socket = 1)
        {
            this.socket = socket;
            this.buffer = Encoding.ASCII.GetBytes(data);
        }

        public override async Task Run()
        {
            // while(true)
            // {
            //     await ClockAsync();
            // }

            // Init
            uint idx = 0;

            while (idx < buffer.Length)
            {
                do
                {
                    // Control bus
                    bufferProducerControlBus.bytes_left = 10;
                    bufferProducerControlBus.available = true;
                    bufferProducerControlBus.valid = true;
                    if (buffer[idx] == (byte)'\n')
                    {
                        bufferProducerControlBus.bytes_left = 0;
                    }

                    // Data bus
                    dataOut.data = buffer[idx];
                    dataOut.socket = this.socket;

                    await ClockAsync();
                } while (consumerControlBus.ready == false); // resend previous byte if consumer is not ready this cycle

                idx++;
            }

        }
    }
}
