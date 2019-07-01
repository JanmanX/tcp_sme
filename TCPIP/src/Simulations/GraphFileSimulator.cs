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
        }

        public override async Task Run()
        {
            // Get initial conditions
            packetGraph.dumpStateInFile("Test");
            packetGraph.NextClock();
            for(int i = 0; i < 1000; i++){
                //Warning! this will fill up your disk fast!

                PacketSend();
                PacketReceive();
                PacketDataIn();
                PacketDataOut();
                PacketWait();
                PacketCommand();
                packetGraph.dumpStateInFile("Test");
                Logging.log.Warn($"---------------------------------------------^^^^^-CLOCK {packetGraph.GetClock()}-^^^^^---------------------------------------");
                packetGraph.NextClock();
                await ClockAsync();


            }
            Logging.log.Info($"End of simulation with {frame_number} packets sent");
        }


        private int frame_number = 0;
        private System.Collections.Generic.IEnumerator<(ushort type,byte data,uint bytes_left)> send_enumerator = null;
        private bool dataExists = false;
        private void PacketSend()
        {

            // There are no current enumerator, get it
            if(send_enumerator == null)
            {
                send_enumerator = packetGraph.IterateOverSend().GetEnumerator();
                dataExists = send_enumerator.MoveNext();
                if(dataExists){
                    frame_number++;
                }
            }
            // If we are ready to send a packet
            if(packetGraph.ReadySend())
            {
                // If there exist data, and the consumer are ready, we load new data
                if(datagramBusInBufferConsumerControlBusIn.ready && dataExists)
                {
                    dataExists = send_enumerator.MoveNext();
                }

                // if there exist data we insert it
                if(dataExists){

                    // Set the busses
                    datagramBusInBufferProducerControlBusOut.valid = true;
                    datagramBusInBufferProducerControlBusOut.bytes_left = send_enumerator.Current.bytes_left;
                    // Set the data
                    datagramBusIn.frame_number = frame_number;
                    datagramBusIn.data = send_enumerator.Current.data;
                    datagramBusIn.type = send_enumerator.Current.type;
                }
                else
                {
                    datagramBusInBufferProducerControlBusOut.valid = false;
                    send_enumerator = null;
                }
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
                    if(!packetGraph.GatherDataIn(dataIn.data,(int)dataInBufferProducerControlBusIn.bytes_left))
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
            if(packetGraph.ReadyWait()){
                packetGraph.StepWait();
            }
        }
    }
}
