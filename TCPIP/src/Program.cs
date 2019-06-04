using System;

using SME;
using SME.Components;

namespace TCPIP
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                // Notes:
                // * implement reverse load and save in the MemorySegmentsRingBufferFiFo
                // * Fix internet_out to use new standards
                // * Make the databuffers ready with 2 bytes(request-respond) so
                //   ready on consumers will give no latency
                // * Make a first in first out ring buffer for the packet out classes

                // Graph simulator
                var simulator = new GraphFileSimulator("data/icmp_data/");

                // Allocate memory blocks
                int packet_out_mem_size = 8192;
                var packet_out_mem = new TrueDualPortMemory<byte>(packet_out_mem_size);
                var packet_out = new PacketOut(packet_out_mem,packet_out_mem_size);

                int packet_in_mem_size = 8192;
                var packet_in_mem = new TrueDualPortMemory<byte>(packet_in_mem_size);
                var packet_in = new PacketIn(packet_in_mem,packet_in_mem_size);


                int data_out_mem_size = 8192;
                var data_out_mem = new TrueDualPortMemory<byte>(data_out_mem_size);
                var data_out = new DataIn(data_out_mem,data_out_mem_size);


                int data_in_mem_size = 8192;
                var data_in_mem = new TrueDualPortMemory<byte>(data_in_mem_size);
                var data_in = new DataOut(data_in_mem,data_in_mem_size);




                var internet_in = new InternetIn();
                var internet_out = new InternetOut();

                // Wire L(Simulator) and I_i together with compute control busses and data
                simulator.datagramBusInComputeConsumerControlBusIn = internet_in.datagramBusInComputeConsumerControlBusOut;
                internet_in.datagramBusInComputeProducerControlBusIn = simulator.datagramBusInComputeProducerControlBusOut;
                internet_in.datagramBusIn = simulator.datagramBusIn;

                // Wire I_i and P_i together and data
                // packet_in.



                // var transport = new Transport(internetIn.segmentBusIn,
                //                               packet_out.bus_in_transport,
                //                               packet_out.bus_in_transport_control_consumer,
                //                               packet_out.bus_in_transport_control_producer);

                // Use fluent syntax to configure the simulator.
                // The order does not matter, but `Run()` must be
                // the last method called.

                // The top-level input and outputs are exposed
                // for interfacing with other VHDL code or board pins

                sim
//                    .AddTopLevelOutputs(dataInReader.)
                    .AddTopLevelInputs(simulator.datagramBusIn)
                    .BuildCSVFile()
                    //.BuildVHDL()
                    .Run();

                // After `Run()` has been invoked the folder
                // `output/vhdl` contains a Makefile that can
                // be used for testing the generated design
            }
        }
    }

}
