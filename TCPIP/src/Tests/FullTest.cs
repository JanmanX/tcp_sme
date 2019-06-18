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
                // * implement reverse load and save in the MemorySegmentsRingBufferFiFo
                // * Fix internet_out to use new standards
                // * Make the databuffers ready with 2 bytes(request-respond) so
                //   ready on consumers will give no latency
                // * Make a first in first out ring buffer for the packet out classes
                // * InternetIn should signal if the packet is fragmented

                // Graph simulator
                var simulator = new GraphFileSimulator("data/graphsimulation/udp_test/");
                // Allocate memory blocks
                int packet_out_mem_size = 8192;
                var packet_out_mem = new TrueDualPortMemory<byte>(packet_out_mem_size);
                var packet_out = new PacketOut(packet_out_mem,packet_out_mem_size);

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
                simulator.datagramBusInComputeConsumerControlBusIn = internet_in.datagramBusInComputeConsumerControlBusOut;
                internet_in.datagramBusInComputeProducerControlBusIn = simulator.datagramBusInComputeProducerControlBusOut;
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
                transport.dataOutBufferProducerControlBusIn = data_in.dataOutBufferProducerControlBusOut;
                data_in.dataIn = transport.dataInWriteBus;

                // Wire Transport to Packet_out
                packet_out.packetInComputeProducerControlBusIn = transport.packetOutComputeProducerControlBusOut;
                transport.packetOutComputeConsumerControlBusIn = packet_out.packetInComputeConsumerControlBusOut;
                packet_out.packetIn = transport.packetOutWriteBus;

                // Wire packet_out to internet_out
                internet_out.packetOutBufferProducerControlBusIn = packet_out.packetOutBufferProducerControlBusOut;
                packet_out.packetOutBufferConsumerControlBusIn = internet_out.packetOutBufferConsumerControlBusOut;
                internet_out.packetOutWriteBus = packet_out.packetOut;

                // Wire internet_out to L(Simulator)
                simulator.datagramBusOutComputeProducerControlBusIn = internet_out.linkOutComputeProducerControlBusOut;
                internet_out.linkOutComputeConsumerControlBusIn = simulator.datagramBusOutComputeConsumerControlBusOut;
                simulator.datagramBusOut = internet_out.linkOutWriteBus;


                // var transport = new Transport(internetIn.segmentBusIn,
                //                               packet_out.bus_in_transport,
                //                               packet_out.bus_in_transport_control_consumer,
                //                               packet_out.bus_in_transport_control_producer);

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
