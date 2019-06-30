using System;
using System.IO;
using System.Threading.Tasks;

using SME;
using SME.Components;

namespace TCPIP
{
    public class GraphFileSimulator : SimulationProcess
    {
        //////// INTERNET IN (Sending to this)
        [OutputBus]
        public Internet.DatagramBusIn datagramBusIn = Scope.CreateBus<Internet.DatagramBusIn>();
        [OutputBus]
        public BufferProducerControlBus datagramBusInBufferProducerControlBusOut = Scope.CreateBus<BufferProducerControlBus>();
        [InputBus]
        public ConsumerControlBus datagramBusInBufferConsumerControlBusIn;


        //////// INTERNET OUT (Receiving from this)
        [InputBus]
        public Internet.DatagramBusOut datagramBusOut;
        [InputBus]
        public ComputeProducerControlBus datagramBusOutComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus datagramBusOutComputeConsumerControlBusOut =  Scope.CreateBus<ConsumerControlBus>();


        //////// DATA IN (Receiving from this)
        [InputBus]
        public DataIn.ReadBus dataIn;
        [InputBus]
        public BufferProducerControlBus dataInBufferProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus dataInBufferConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();




        // Simulation fields
        private readonly String dir;

        private PacketGraph packetGraph;


        public GraphFileSimulator(String dir)
        {
            this.dir = dir;
            this.packetGraph = new PacketGraph(this.dir);

            this.packetGraph.Info();
            Console.WriteLine(this.packetGraph.GraphwizState());

            send_enumerator = packetGraph.IterateOverSend().GetEnumerator();
            send_enumerator.MoveNext();
        }

        public override async Task Run()
        {
            //return;
            for(int i = 0; i < 1000; i++){

                PacketSend();
                PacketReceive();
                PacketDataIn();
                PacketDataOut();
                PacketWait();
                PacketCommand();
                await ClockAsync();
                Logging.log.Warn("-----------------------CLOCK--------------------");

            }
            Logging.log.Info($"End of simulation with {frame_number} packets sent");
        }


        private int frame_number = 0;
        private System.Collections.Generic.IEnumerator<(ushort type,byte data,uint bytes_left)>  send_enumerator;
        private void PacketSend()
        {
            if(packetGraph.ReadySend())
            {
                var block = send_enumerator.Current;
                // Set the busses
                // If we get the ready signal, we can move to the next data
                if(datagramBusInBufferConsumerControlBusIn.ready){
                    if(!send_enumerator.MoveNext() || block.bytes_left == 0)
                    {
                        frame_number++;
                        send_enumerator = packetGraph.IterateOverSend().GetEnumerator();
                        send_enumerator.MoveNext();
                    }
                }
                block = send_enumerator.Current;
                // Set the busses
                datagramBusInBufferProducerControlBusOut.valid = true;
                datagramBusInBufferProducerControlBusOut.bytes_left = block.bytes_left;

                datagramBusIn.frame_number = frame_number;
                datagramBusIn.data = block.data;
                datagramBusIn.type = block.type;
                Logging.log.Trace($"GraphSimulator sending: data: 0x{block.data:X2} bytes_left: {block.bytes_left} frame: {frame_number}  ready: {datagramBusInBufferConsumerControlBusIn.ready}");

            }
            else
            {
                // There are no data , set valid false
                datagramBusInBufferProducerControlBusOut.valid = false;
            }
        }
        private void PacketReceive()
        {

        }
        private bool dataInWait = true;
        private void PacketDataIn()
        {
            dataInBufferConsumerControlBusOut.ready = false;
            // if we got data ready to read
            if(packetGraph.ReadyDataIn()){
                // If we do not have to wait one clock
                if(!dataInWait && dataInBufferProducerControlBusIn.valid)
                {
                    if(!packetGraph.GatherDataIn(dataIn.data))
                    {
                        throw new Exception("Wrong data, see log");
                    }
                    dataInWait = true;
                }
                if(dataInBufferProducerControlBusIn.valid)
                {
                    dataInBufferConsumerControlBusOut.ready = true;
                    dataInWait = false;
                }
                else
                {
                    dataInBufferConsumerControlBusOut.ready = false;
                    dataInWait = true;
                }
            }
        }
        private void PacketDataOut()
        {

        }
        private void PacketCommand()
        {

        }
        private void PacketWait()
        {

        }
    }
}
