using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;

using SME;
using SME.Components;

namespace TCPIP
{
    public class DataOutSimulator : SimulationProcess
    {
        [OutputBus]
        public BufferProducerControlBus bufferProducerControlBus = Scope.CreateBus<BufferProducerControlBus>();

        [OutputBus]
        public DataOut.ReadBus dataOutReadBus = Scope.CreateBus<DataOut.ReadBus>();

        [InputBus]
        public ConsumerControlBus consumerControlBus;


        private byte[] bytes;

        public DataOutSimulator()
        {
            bytes = System.Text.Encoding.ASCII.GetBytes("AAAA\nBBBB\nCCCC\nDDDD\nEEEE\nFFFF\nGGGG\nHHHH\n");
        }

        public override async Task Run()
        {
            // Init
            uint i = 0;
            await ClockAsync();
            await ClockAsync();

            //bufferProducerControlBus.available = true;


            while (i < bytes.Length)
            {
                bufferProducerControlBus.valid = true;
                dataOutReadBus.socket = 0;
                dataOutReadBus.data = bytes[i];

                await ClockAsync();
                if(consumerControlBus.ready == true)
                {
                    i++;
                }
            }

            await ClockAsync();

            //bufferProducerControlBus.available = false;
            bufferProducerControlBus.valid = false;


            for(int j = 0; j< 100; j++)
            {
                await ClockAsync();
            }
        }
    }
}