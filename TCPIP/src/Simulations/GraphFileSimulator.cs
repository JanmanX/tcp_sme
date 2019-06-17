using System;
using System.IO;
using System.Threading.Tasks;

using SME;
using SME.Components;

namespace TCPIP
{
    public class GraphFileSimulator : SimulationProcess
    {
        //////// INTERNET IN (Sending to thus)
        [OutputBus]
        public Internet.DatagramBusIn datagramBusIn = Scope.CreateBus<Internet.DatagramBusIn>();
        [OutputBus]
        public ComputeProducerControlBus datagramBusInComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [InputBus]
        public ConsumerControlBus datagramBusInComputeConsumerControlBusIn;


        //////// INTERNET OUT (Receiving from this)
        [InputBus]
        public Internet.DatagramBusOut datagramBusOut;
        [InputBus]
        public ComputeProducerControlBus datagramBusOutComputeProducerControlBusIn;
        [OutputBus]
        public ConsumerControlBus datagramBusOutComputeConsumerControlBusOut =  Scope.CreateBus<ConsumerControlBus>();


        // Simulation fields
        private readonly String dir;

        private PacketGraph packetGraph;


        public GraphFileSimulator(String dir)
        {
            this.dir = dir;
            this.packetGraph = new PacketGraph(this.dir);

        }

        public override async Task Run()
        {
            uint frame_number = 0;
            for(int i = 0; i < 200; i++)
            {
                // Wait for the initial reset to propagate
                await ClockAsync();

                datagramBusInComputeProducerControlBusOut.valid = false;
                datagramBusInComputeProducerControlBusOut.available = false;

                while(packetGraph.HasPackagesToSend())
                { // Are there anything to send? if not, spinloop dat shizz
                    Logging.log.Info("At frame number " + frame_number);
                    // Show us as avaliable
                    datagramBusInComputeProducerControlBusOut.available = true;
                    // If the consumer is not ready, skip
                    if (!datagramBusInComputeConsumerControlBusIn.ready){
                        Logging.log.Info("The datagramBusIn was not ready");
                        break;
                    }

                    foreach (var data in packetGraph.IterateOverPacketToSend())
                    {
                        // Data is now valid
                        datagramBusInComputeProducerControlBusOut.valid = true;
                        datagramBusInComputeProducerControlBusOut.bytes_left = data.bytes_left;
                        // Send to Network
                        datagramBusIn.frame_number = frame_number;
                        datagramBusIn.data = data.data;
                        datagramBusIn.type = data.type;
                        //Logging.log.Info($"data:{data.data:X2} bytes_left:{data.bytes_left}");
                        await ClockAsync();

                    }

                    // Next packet
                    frame_number++;
                    Logging.log.Info("End of frame");
                }
            }

            Logging.log.Info($"End of simulation with {frame_number} packets sent");
        }
    }
}
