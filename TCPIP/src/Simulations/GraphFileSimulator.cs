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
        public BufferProducerControlBus datagramBusOutBufferProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus datagramBusOutBufferConsumerControlBusOut =  Scope.CreateBus<ConsumerControlBus>();


        //////// DATA IN (Receiving from this)
        [InputBus]
        public DataIn.ReadBus dataIn;
        [InputBus]
        public BufferProducerControlBus dataInBufferProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus dataInBufferConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();

        //////// DATA OUT (Sending from this)
        [OutputBus]
        public DataOut.WriteBus dataOut = Scope.CreateBus<DataOut.WriteBus>();
        [OutputBus]
        public ComputeProducerControlBus dataOutComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [InputBus]
        public ConsumerControlBus dataOutComputeConsumerControlBusIn;



        // Simulation fields
        private readonly String dir;

        private readonly int max_clocks;
        private readonly bool debug;
        private PacketGraph packetGraph;


        public GraphFileSimulator(String dir, int max_clocks, bool debug = false)
        {
            this.dir = dir;
            this.max_clocks = max_clocks;
            this.debug = debug;
            this.packetGraph = new PacketGraph(this.dir);
            if(this.debug){
                this.packetGraph.Info();
            }

        }

        public override async Task Run()
        {
            if(debug){
                //Console.WriteLine(this.packetGraph.GraphwizState());
                //return;
                packetGraph.DumpStateInFile("Test");
            }
            //return;
            // Get initial conditions
            packetGraph.NextClock();
            await ClockAsync();

            for(int i = 0; i < this.max_clocks; i++){
                //Warning! this will fill up your disk fast!
                Logging.log.Warn($"---------------------------------------------vvvvv-CLOCK {packetGraph.GetClock()}-vvvvv---------------------------------------");

                PacketSend();
                PacketReceive();
                PacketDataIn();
                PacketDataOut();
                PacketWait();
                PacketCommand();
                if(debug){
                    packetGraph.DumpStateInFile("Test");
                }
                //Logging.log.Warn($"---------------------------------------------^^^^^-CLOCK {packetGraph.GetClock()}-^^^^^---------------------------------------");
                packetGraph.NextClock();
                await ClockAsync();



            }
            Logging.log.Warn($"---------------------------------------------vvvvv-CLOCK {packetGraph.GetClock()}-vvvvv---------------------------------------");
            Logging.log.Info($"End of simulation with {frame_number_send} packets sent");
            //Console.WriteLine("---------------------------------------------END-------------------------------------------");
        }


        private int frame_number_send = 0;
        private System.Collections.Generic.IEnumerator<(ushort type,byte data,uint bytes_left,PacketGraph.Packet packet)> send_enumerator = null;
        private bool dataExistSend = false;
        private void PacketSend()
        {

            // There are no current enumerator, get it
            if(send_enumerator == null)
            {
                send_enumerator = packetGraph.IterateOverSend().GetEnumerator();
                dataExistSend = send_enumerator.MoveNext();
                if(dataExistSend){
                    frame_number_send++;
                }
            }
            // If we are ready to send a packet
            if(packetGraph.ReadySend())
            {
                // If there exist data, and the consumer are ready, we load new data
                if(datagramBusInBufferConsumerControlBusIn.ready && dataExistSend)
                {
                    dataExistSend = send_enumerator.MoveNext();
                }

                // if there exist data we insert it
                if(dataExistSend){

                    // Set the busses
                    datagramBusInBufferProducerControlBusOut.valid = true;
                    datagramBusInBufferProducerControlBusOut.bytes_left = send_enumerator.Current.bytes_left;
                    // Set the data
                    datagramBusIn.frame_number = send_enumerator.Current.packet.id; //frame_number;
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
        private bool receiveWaitNextClock = true;

        private void PacketReceive()
        {
            datagramBusOutBufferConsumerControlBusOut.ready = false;
            // if we got data ready to read
            if(packetGraph.ReadyReceive()){
                // If we do not have to wait one clock
                if(!receiveWaitNextClock && datagramBusOutBufferProducerControlBusIn.valid)
                {
                    if(!packetGraph.GatherReceive(datagramBusOut.data,(int)datagramBusOutBufferProducerControlBusIn.bytes_left))
                    {
                        //Logging.log.Error("Wrong data, see log");
                        //throw new Exception("Wrong data, see log");
                    }
                    receiveWaitNextClock = true;
                }
                if(datagramBusOutBufferProducerControlBusIn.valid)
                {
                    datagramBusOutBufferConsumerControlBusOut.ready = true;
                    receiveWaitNextClock = false;
                }
                else
                {
                    datagramBusOutBufferConsumerControlBusOut.ready = false;
                    receiveWaitNextClock = true;
                }
            }else{
                receiveWaitNextClock = true;
            }
        }
        private bool dataInWaitNextClock = true;
        private void PacketDataIn()
        {
            dataInBufferConsumerControlBusOut.ready = false;
            // if we got data ready to read
            if(packetGraph.ReadyDataIn()){
                // If we do not have to wait one clock
                if(!dataInWaitNextClock && dataInBufferProducerControlBusIn.valid)
                {
                    if(!packetGraph.GatherDataIn(dataIn.data,(int)dataInBufferProducerControlBusIn.bytes_left))
                    {
                        Logging.log.Error("Wrong data, see log");
                        //throw new Exception("Wrong data, see log");
                    }
                    dataInWaitNextClock = true;
                }
                if(dataInBufferProducerControlBusIn.valid)
                {
                    dataInBufferConsumerControlBusOut.ready = true;
                    dataInWaitNextClock = false;
                }
                else
                {
                    dataInBufferConsumerControlBusOut.ready = false;
                    dataInWaitNextClock = true;
                }
            }else{
                dataInWaitNextClock = true;
            }
        }

        private System.Collections.Generic.IEnumerator<(ushort type,byte data,uint bytes_left,PacketGraph.Packet packet)> dataout_enumerator = null;
        private bool dataExistDataout = false;
        private void PacketDataOut()
        {
            //dataOutComputeProducerControlBusOut.valid = false;
            // There are no current enumerator, get it
            if(dataout_enumerator == null)
            {
                dataout_enumerator = packetGraph.IterateOverDataOut().GetEnumerator();
            }
            // If we are ready to send a packet
            if(packetGraph.ReadyDataOut())
            {
                // If there exist data, and the consumer are ready, we load new data
                dataExistDataout = dataout_enumerator.MoveNext();
                if(dataExistDataout)
                {
                    // Set the busses
                    dataOutComputeProducerControlBusOut.valid = true;
                    dataOutComputeProducerControlBusOut.bytes_left = dataout_enumerator.Current.bytes_left;
                    // Set the data
                    dataOut.frame_number = dataout_enumerator.Current.packet.id; //frame_number;
                    dataOut.data = dataout_enumerator.Current.data;
                    int socket = Convert.ToInt32(dataout_enumerator.Current.packet.additional_data);
                    dataOut.socket = socket;
                    //Logging.log.Error($"Submitting: 0x{dataout_enumerator.Current.data:X2} socket: {socket}");

                }
                else
                {
                    dataOutComputeProducerControlBusOut.valid = false;
                    dataout_enumerator = null;
                }
            }
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
