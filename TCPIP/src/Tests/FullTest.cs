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
                //var simulator = new GraphFileSimulator("data/graphsimulation/udp_out_test/",450,true);
                //var simulator = new GraphFileSimulator("data/graphsimulation/advanced_udp_test/",1000000,true);
                //var simulator = new GraphFileSimulator("data/graphsimulation/big_advanced_udp_test/",1000000000,true);
                var simulator = new GraphFileSimulator("data/graphsimulation/small_advanced_udp_test/",1000000000,true);

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
                //simulator.AddBlock(GraphFileSimulator.BlockInfo.INTERNET_IN,internet_in);
                var internet_out = new InternetOut();
                var transport = new Transport(128);
                //var interface = new Interface();

                // Add debug information for the system blocks
                simulator.AddSystem(SystemGraph.SystemName.INTERNET_IN,internet_in);
                simulator.AddSystem(SystemGraph.SystemName.INTERNET_OUT,internet_out);
                simulator.AddSystem(SystemGraph.SystemName.DATA_IN,data_in);
                simulator.AddSystem(SystemGraph.SystemName.DATA_OUT,data_out);
                simulator.AddSystem(SystemGraph.SystemName.FRAME_OUT,frame_out);
                simulator.AddSystem(SystemGraph.SystemName.SEGMENT_IN,packet_in);
                simulator.AddSystem(SystemGraph.SystemName.SEGMENT_OUT,packet_out);
                simulator.AddSystem(SystemGraph.SystemName.TRANSPORT,transport);
                simulator.AddSystem(SystemGraph.SystemName.LINK_INTERFACE,simulator);
                simulator.AddSystem(SystemGraph.SystemName.INTERFACE,simulator);

                // Wire L(Simulator) to Internet_in
                simulator.datagramBusInBufferConsumerControlBusIn = internet_in.datagramBusInBufferConsumerControlBusOut;
                internet_in.datagramBusInBufferProducerControlBusIn = simulator.datagramBusInBufferProducerControlBusOut;
                internet_in.datagramInBus = simulator.datagramBusIn;
                simulator.AddConnection(SystemGraph.SystemName.INTERNET_IN,
                                        SystemGraph.SystemName.LINK_INTERFACE,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        internet_in.datagramBusInBufferConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.LINK_INTERFACE,
                                        SystemGraph.SystemName.INTERNET_IN,
                                        SystemGraph.ConnectionType.CONTROL_BUFFER_PRODUCER,
                                        simulator.datagramBusInBufferProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.LINK_INTERFACE,
                                        SystemGraph.SystemName.INTERNET_IN,
                                        SystemGraph.ConnectionType.DATA,
                                        simulator.datagramBusIn);



                // Wire Internet_in to packet_in
                internet_in.packetInComputeConsumerControlBusIn = packet_in.packetInComputeConsumerControlBusOut;
                packet_in.packetInComputeProducerControlBusIn = internet_in.packetInComputeProducerControlBusOut;
                packet_in.packetInBus = internet_in.packetInBus;
                simulator.AddConnection(SystemGraph.SystemName.SEGMENT_IN,
                                        SystemGraph.SystemName.INTERNET_IN,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        packet_in.packetInComputeConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.INTERNET_IN,
                                        SystemGraph.SystemName.SEGMENT_IN,
                                        SystemGraph.ConnectionType.CONTROL_COMPUTE_PRODUCER,
                                        internet_in.packetInComputeProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.INTERNET_IN,
                                        SystemGraph.SystemName.SEGMENT_IN,
                                        SystemGraph.ConnectionType.DATA,
                                        internet_in.packetInBus);

                // Wire packet_in to Transport
                packet_in.packetOutBufferConsumerControlBusIn = transport.packetInBufferConsumerControlBusOut;
                transport.packetInBufferProducerControlBusIn = packet_in.packetOutBufferProducerControlBusOut;
                transport.packetInBus = packet_in.packetOutBus;
                simulator.AddConnection(SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.SystemName.SEGMENT_IN,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        transport.packetInBufferConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.SEGMENT_IN,
                                        SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.ConnectionType.CONTROL_BUFFER_PRODUCER,
                                        packet_in.packetOutBufferProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.SEGMENT_IN,
                                        SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.ConnectionType.DATA,
                                        packet_in.packetOutBus);

                // Wire Data_out to Transport
                data_out.dataOutBufferConsumerControlBusIn = transport.dataOutBufferConsumerControlBusOut;
                transport.dataOutBufferProducerControlBusIn = data_out.dataOutBufferProducerControlBusOut;
                transport.dataOutReadBus = data_out.dataOut;
                simulator.AddConnection(SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.SystemName.DATA_OUT,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        transport.dataOutBufferConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.DATA_OUT,
                                        SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.ConnectionType.CONTROL_BUFFER_PRODUCER,
                                        data_out.dataOutBufferProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.DATA_OUT,
                                        SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.ConnectionType.DATA,
                                        data_out.dataOut);

                // Wire Transport to Data_in
                transport.dataInComputeConsumerControlBusIn = data_in.dataInComputeConsumerControlBusOut;
                data_in.dataInComputeProducerControlBusIn = transport.dataInComputeProducerControlBusOut;
                data_in.dataIn = transport.dataInWriteBus;
                simulator.AddConnection(SystemGraph.SystemName.DATA_IN,
                                        SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        data_in.dataInComputeConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.SystemName.DATA_IN,
                                        SystemGraph.ConnectionType.CONTROL_COMPUTE_PRODUCER,
                                        transport.dataInComputeProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.SystemName.DATA_IN,
                                        SystemGraph.ConnectionType.DATA,
                                        transport.dataInWriteBus);

                // Wire Transport to Packet_out
                transport.packetOutComputeConsumerControlBusIn = packet_out.packetInComputeConsumerControlBusOut;
                packet_out.packetInComputeProducerControlBusIn = transport.packetOutComputeProducerControlBusOut;
                packet_out.packetIn = transport.packetOutWriteBus;
                simulator.AddConnection(SystemGraph.SystemName.SEGMENT_OUT,
                                        SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        packet_out.packetInComputeConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.SystemName.SEGMENT_OUT,
                                        SystemGraph.ConnectionType.CONTROL_COMPUTE_PRODUCER,
                                        transport.packetOutComputeProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.TRANSPORT,
                                        SystemGraph.SystemName.SEGMENT_OUT,
                                        SystemGraph.ConnectionType.DATA,
                                        transport.packetOutWriteBus);

                // Wire packet_out to internet_out
                packet_out.packetOutBufferConsumerControlBusIn = internet_out.packetOutBufferConsumerControlBusOut;
                internet_out.packetOutBufferProducerControlBusIn = packet_out.packetOutBufferProducerControlBusOut;
                internet_out.packetOutWriteBus = packet_out.packetOut;
                simulator.AddConnection(SystemGraph.SystemName.INTERNET_OUT,
                                        SystemGraph.SystemName.SEGMENT_OUT,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        internet_out.packetOutBufferConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.SEGMENT_OUT,
                                        SystemGraph.SystemName.INTERNET_OUT,
                                        SystemGraph.ConnectionType.CONTROL_BUFFER_PRODUCER,
                                        packet_out.packetOutBufferProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.SEGMENT_OUT,
                                        SystemGraph.SystemName.INTERNET_OUT,
                                        SystemGraph.ConnectionType.DATA,
                                        packet_out.packetOut);

                // Wire internet_out to frame_out
                internet_out.frameOutComputeConsumerControlBusIn = frame_out.packetInComputeConsumerControlBusOut;
                frame_out.packetInComputeProducerControlBusIn = internet_out.frameOutComputeProducerControlBusOut;
                frame_out.packetIn = internet_out.frameOutWriteBus;
                simulator.AddConnection(SystemGraph.SystemName.FRAME_OUT,
                                        SystemGraph.SystemName.INTERNET_OUT,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        frame_out.packetInComputeConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.INTERNET_OUT,
                                        SystemGraph.SystemName.FRAME_OUT,
                                        SystemGraph.ConnectionType.CONTROL_COMPUTE_PRODUCER,
                                        internet_out.frameOutComputeProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.INTERNET_OUT,
                                        SystemGraph.SystemName.FRAME_OUT,
                                        SystemGraph.ConnectionType.DATA,
                                        internet_out.frameOutWriteBus);

                // Wire frame_out to L(Simulator)
                frame_out.datagramBusOutBufferConsumerControlBusIn = simulator.datagramBusOutBufferConsumerControlBusOut;
                simulator.datagramBusOutBufferProducerControlBusIn = frame_out.datagramBusOutBufferProducerControlBusOut;
                simulator.datagramBusOut = frame_out.datagramBusOut;
                simulator.AddConnection(SystemGraph.SystemName.LINK_INTERFACE,
                                        SystemGraph.SystemName.FRAME_OUT,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        simulator.datagramBusOutBufferConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.FRAME_OUT,
                                        SystemGraph.SystemName.LINK_INTERFACE,
                                        SystemGraph.ConnectionType.CONTROL_BUFFER_PRODUCER,
                                        frame_out.datagramBusOutBufferProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.FRAME_OUT,
                                        SystemGraph.SystemName.LINK_INTERFACE,
                                        SystemGraph.ConnectionType.DATA,
                                        frame_out.datagramBusOut);

                // Wire DataIn to L(Simulator)
                data_in.dataOutBufferConsumerControlBusIn = simulator.dataInBufferConsumerControlBusOut;
                simulator.dataInBufferProducerControlBusIn = data_in.dataOutBufferProducerControlBusOut;
                simulator.dataIn = data_in.dataOut;
                simulator.AddConnection(SystemGraph.SystemName.INTERFACE,
                                        SystemGraph.SystemName.DATA_IN,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        simulator.dataInBufferConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.DATA_IN,
                                        SystemGraph.SystemName.INTERFACE,
                                        SystemGraph.ConnectionType.CONTROL_BUFFER_PRODUCER,
                                        data_in.dataOutBufferProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.DATA_IN,
                                        SystemGraph.SystemName.INTERFACE,
                                        SystemGraph.ConnectionType.DATA,
                                        data_in.dataOut);

                // Wire DataOut to L(Simulator)
                simulator.dataOutComputeConsumerControlBusIn = data_out.dataInComputeConsumerControlBusOut;
                data_out.dataInComputeProducerControlBusIn = simulator.dataOutComputeProducerControlBusOut;
                data_out.dataIn = simulator.dataOut;
                simulator.AddConnection(SystemGraph.SystemName.DATA_OUT,
                                        SystemGraph.SystemName.INTERFACE,
                                        SystemGraph.ConnectionType.CONTROL_CONSUMER,
                                        data_out.dataInComputeConsumerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.INTERFACE,
                                        SystemGraph.SystemName.DATA_OUT,
                                        SystemGraph.ConnectionType.CONTROL_COMPUTE_PRODUCER,
                                        simulator.dataOutComputeProducerControlBusOut);
                simulator.AddConnection(SystemGraph.SystemName.INTERFACE,
                                        SystemGraph.SystemName.DATA_OUT,
                                        SystemGraph.ConnectionType.DATA,
                                        simulator.dataOut);

                // Use fluent syntax to configure the simulator.
                // The order does not matter, but `Run()` must be
                // the last method called.

                // The top-level input and outputs are exposed
                // for interfacing with other VHDL code or board pins

                sim.AddTopLevelInputs(simulator.datagramBusIn)
                    //.BuildCSVFile()
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
