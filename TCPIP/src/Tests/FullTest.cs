using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;

using SME;
using SME.Components;

namespace TCPIP
{
    public class FullTest
    {
        public static bool Run()
        {
           using (var sim = new Simulation())
            {
                // Notes:
                // * InternetIn should signal if the packet is fragmented

                // Graph simulator
                var simulator = new GraphFileSimulator("data/graphsimulation/udp_out_test/",2000,true);
                // Allocate memory blocks
                int packet_out_mem_size = 8192;
                var packet_out_mem = new TrueDualPortMemory<byte>(packet_out_mem_size);
                var packet_out = new PacketOut(packet_out_mem,packet_out_mem_size);

                int frame_out_mem_size = 8192;
                var frame_out_mem = new TrueDualPortMemory<byte>(frame_out_mem_size);
                var frame_out = new FrameOut(frame_out_mem,frame_out_mem_size);

                int packet_in_mem_size = 8192;
                var packet_in_mem = new TrueDualPortMemory<byte>(packet_in_mem_size);
                var packet_in = new PacketIn(packet_in_mem,packet_in_mem_size);

                int data_out_mem_size = 8192;
                var data_out_mem = new TrueDualPortMemory<byte>(data_out_mem_size);
                var data_out = new DataOut(data_out_mem,data_out_mem_size);

                int data_in_mem_size = 8192;
                var data_in_mem = new TrueDualPortMemory<byte>(data_in_mem_size);
                var data_in = new DataIn(data_in_mem,data_in_mem_size);




                var internet_in = new InternetIn();
                var internet_out = new InternetOut();
                var transport = new Transport();
                //var interface = new Interface();

                // Wire L(Simulator) to Internet_in
                simulator.datagramBusInBufferConsumerControlBusIn = internet_in.datagramBusInBufferConsumerControlBusOut;
                internet_in.datagramBusInBufferProducerControlBusIn = simulator.datagramBusInBufferProducerControlBusOut;
                internet_in.datagramInBus = simulator.datagramBusIn;

                // Wire Internet_in to packet_in
                packet_in.packetInComputeProducerControlBusIn = internet_in.packetInComputeProducerControlBusOut;
                internet_in.packetInComputeConsumerControlBusIn = packet_in.packetInComputeConsumerControlBusOut;
                packet_in.packetInBus = internet_in.packetInBus;

                // Wire packet_in to Transport
                packet_in.packetOutBufferConsumerControlBusIn = transport.packetInBufferConsumerControlBusOut;
                transport.packetInBufferProducerControlBusIn = packet_in.packetOutBufferProducerControlBusOut;
                transport.packetInBus = packet_in.packetOutBus;

                // Wire Data_out to Transport
                data_out.dataOutBufferConsumerControlBusIn = transport.dataOutBufferConsumerControlBusOut;
                transport.dataOutBufferProducerControlBusIn = data_out.dataOutBufferProducerControlBusOut;
                transport.dataOutReadBus = data_out.dataOut;

                // Wire Transport to Data_in
                data_in.dataInComputeProducerControlBusIn = transport.dataInComputeProducerControlBusOut;
                transport.dataInComputeConsumerControlBusIn = data_in.dataInComputeConsumerControlBusOut;
                data_in.dataIn = transport.dataInWriteBus;

                // Wire Transport to Packet_out
                packet_out.packetInComputeProducerControlBusIn = transport.packetOutComputeProducerControlBusOut;
                transport.packetOutComputeConsumerControlBusIn = packet_out.packetInComputeConsumerControlBusOut;
                packet_out.packetIn = transport.packetOutWriteBus;

                // Wire packet_out to internet_out
                internet_out.packetOutBufferProducerControlBusIn = packet_out.packetOutBufferProducerControlBusOut;
                packet_out.packetOutBufferConsumerControlBusIn = internet_out.packetOutBufferConsumerControlBusOut;
                internet_out.packetOutWriteBus = packet_out.packetOut;

                // Wire internet_out to frame_out
                frame_out.packetInComputeProducerControlBusIn = internet_out.frameOutComputeProducerControlBusOut;
                internet_out.frameOutComputeConsumerControlBusIn = frame_out.packetInComputeConsumerControlBusOut;
                frame_out.packetIn = internet_out.frameOutWriteBus;

                // Wire frame_out to L(Simulator)
                simulator.datagramBusOutBufferProducerControlBusIn = frame_out.datagramBusOutBufferProducerControlBusOut;
                frame_out.datagramBusOutBufferConsumerControlBusIn = simulator.datagramBusOutBufferConsumerControlBusOut;
                simulator.datagramBusOut = frame_out.datagramBusOut;

                // Wire DataIn to L(Simulator)
                simulator.dataIn = data_in.dataOut;
                data_in.dataOutBufferConsumerControlBusIn = simulator.dataInBufferConsumerControlBusOut;
                simulator.dataInBufferProducerControlBusIn = data_in.dataOutBufferProducerControlBusOut;

                // Wire DataOut to L(Simulator)
                data_out.dataIn = simulator.dataOut;
                data_out.dataInComputeProducerControlBusIn = simulator.dataOutComputeProducerControlBusOut;
                simulator.dataOutComputeConsumerControlBusIn = data_out.dataInComputeConsumerControlBusOut;

                // Use fluent syntax to configure the simulator.
                // The order does not matter, but `Run()` must be
                // the last method called.

                // The top-level input and outputs are exposed
                // for interfacing with other VHDL code or board pins

                sim.AddTopLevelInputs(simulator.datagramBusIn)
                    .BuildCSVFile()
                    //.BuildVHDL()
                    .Run();

                // After `Run()` has been invoked the folder
                // `output/vhdl` contains a Makefile that can
                // be used for testing the generated design
            }
           return true;
        }
    }


}
