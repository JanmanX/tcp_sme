using System;
using System.IO;
using System.Threading.Tasks;

using SME;
using SME.Components;

namespace TCPIP
{
    public class GraphFileSimulator : SimulationProcess
    {
        [OutputBus]
        public readonly Internet.DatagramBusIn datagramBusIn = Scope.CreateBus<Internet.DatagramBusIn>();

        //[OutputBus]
        //public readonly TrueDualPortMemory<byte>.IControlA controlA;


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
            uint frame_number = 0x00;
            for(int i = 0; i < 10000; i++)
            {
                    // Wait for the initial reset to propagate
                await ClockAsync();


                while(packetGraph.HasPackagesToSend())
                { // Are there anything to send? if not, spinloop dat shizz
                    Logging.log.Info("At frame number " + frame_number);
                    foreach (var data in packetGraph.IterateOverPacketToSend())
                    {
                        // Send to Network
                        datagramBusIn.frame_number = frame_number;
                        datagramBusIn.data = data.b;
                        datagramBusIn.type = data.type;
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
