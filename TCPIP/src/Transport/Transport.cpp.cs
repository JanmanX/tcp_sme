using System;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    [ClockedProcess]
    public partial class Transport : SimpleProcess
    {
        ////////////////////////////// Busses /////////////////////////////////
        // PacketIn
        [InputBus]
        private readonly ProducerControlBus packetInProducerControlBus;
        [InputBus]
        private readonly PacketInBus packetInBus;
        [OutputBus]
        private readonly ConsumerControlBus packetInConsumerControlBus = Scope.CreateBus<ConsumerControlBus>();

        // DataOut
        [InputBus]
        private readonly DataOutBus dataOutBus;
        [InputBus]
        private readonly ProducerControlBus dataOutProducerControlBus;
        [OutputBus]
        private readonly ConsumerControlBus dataOutConsumerControlBus = Scope.CreateBus<ConsumerControlBus>();

        // Interface
        [InputBus]
        private readonly InterfaceBus interfaceBus;
        [OutputBus]
        private readonly InterfaceControlBus interfaceControlBus = Scope.CreateBus<InterfaceControlBus>();



        // Local variables
        private enum TransportProcessState
        {
            Receive,  // Reading an incoming packet
            Pass,    // Passing data of an incoming packet to a buffer (Data_in)
            Send,    // Sending a data packet out
            Control,    // Control work on connections (handshakes, conn. termination)
            Idle,     // Nothing to do
        }
        private TransportProcessState state = TransportProcessState.Receive;

        private const uint NUM_PCB = 10;
        private PCB[] pcbs = new PCB[NUM_PCB];

        private const int BUFFER_SIZE = 100;
        private byte[] buffer_in = new byte[BUFFER_SIZE];
        private uint idx_in = 0x00;
        private bool read = true; // Indicates whether process is writing from local buffer

        private struct PassData
        {
            public int socket;
            public uint ip_id;
            public uint tcp_seq;
            public uint length;

            // Local info
            public byte high_byte; // High byte for checksum calculation
            public uint bytes_sent; // Number of bytes sent
        }
        private PassData passData;
        private uint ip_id = 0x00; // Current ip_id

        // WRITE
        private byte[] buffer_out = new byte[BUFFER_SIZE];
        private uint idx_out = 0x00;
        private bool write = false; // Inidicates whetehr process is writing from local buffer

        public Transport(Transport.SegmentBusIn segmentBusIn)
        {
            this.segmentBusIn = segmentBusIn ?? throw new ArgumentNullException(nameof(segmentBusIn));
        }


        protected override void OnTick()
        {
            switch (state)
            {
                case TransportProcessState.Idle:
                    Idle();
                    break;
                case TransportProcessState.Receive:
                    Receive();
                    break;
                case TransportProcessState.Pass:
                    Pass();
                    break;
                case TransportProcessState.Send:
                    Send();
                    break;
                case TransportProcessState.Control:
                    Control();
                    break;
            }
        }

        ////////////////////////////// State functions ////////////////////////
        private void StartIdle()
        {
            state = TransportProcessState.Idle;

            // Reset all consumer busses
            dataOutConsumerControlBus.ready = false;
            packetInConsumerControlBus.ready = false;
        }

        private void Idle()
        {
            // Check control busses for work to do
            if (packetInProducerControlBus.available)
            {
                StartReceive();
            }
            else if (interfaceBus.valid)
            {
                StartControl();
            }
            else if (dataOutProducerControlBus.available)
            {
                StartSending();
            }
        }

        private void StartReceive()
        {
            state = TransportProcessState.Receive;

            // Ready
            packetInConsumerControlBus.ready = true;
        }

        void Receive()
        {
            // If invalid, skip
            if (packetInProducerControlBus.valid == false)
            {
                return;
            }

            // If new segment received, reset
            if (packetInBus.ip_id != ip_id)
            {
                ip_id = packetInBus.ip_id;

                // The rest could be done in StartReceive(), but keeping it 
                // here might enable us to resume parsing
                idx_in = 0x00;
                read = true;
            }


            if (read && idx_in < buffer_in.Length)
            {
                buffer_in[idx_in++] = packetInBus.data;

                // Processing
                switch (packetInBus.protocol)
                {
                    case (byte)IPv4.Protocol.TCP:
                        // End of header, start parsing
                        if (idx_in == TCP.HEADER_SIZE)
                        {
                            read = false;
                            ParseTCP();
                        }
                        break;

                    case (byte)IPv4.Protocol.UDP:
                        if (idx_in == UDP.HEADER_SIZE)
                        {
                            read = false;
                            ParseUDP();
                        }

                        break;
                }
            }
        }


        private void StartPass()
        {
            state = TransportProcessState.Pass;

            // Set busses
            packetInConsumerControlBus.ready = true;
            dataOutConsumerControlBus.ready = false;
        }

        void Pass()
        {
            // XXX: If invalid, skip and wait till next clock
            if (packetInProducerControlBus.valid == false)
            {
                return;
            }

            // If new segment received, reset
            if (packetInBus.ip_id != ip_id)
            {
                ip_id = segmentBusIn.ip_id;
                idx_in = 0x00;
                state = TransportProcessState.Receiving;
                read = true;
                dataInBus.valid = false;
                return;
            }


            // Checksum
            if (passData.bytes_sent % 2 == 0)
            {
                pcbs[passData.socket].checksum_acc +=
                    (uint)((passData.high_byte << 8) | dataInBus.data);
            }
            else
            {
                passData.high_byte = dataInBus.data;
            }

            // Set bus values
            dataInBus.valid = true;
            dataInBus.socket = passData.socket;
            dataInBus.ip_id = passData.ip_id;
            dataInBus.tcp_seq = passData.tcp_seq;
            dataInBus.data = segmentBusIn.data;
            dataInBus.finished = false;
            dataInBus.invalidate = false;
            passData.bytes_sent++;

            // If last byte
            if (passData.bytes_sent >= passData.length)
            {
                // Finish checksum
                pcbs[passData.socket].checksum_acc = ((pcbs[passData.socket].checksum_acc & 0xFFFF)
                        + (pcbs[passData.socket].checksum_acc >> 0x10));

                if (pcbs[passData.socket].checksum_acc == 0)
                {
                    dataInBus.finished = true;
                }
                else
                {
                    Console.WriteLine($"Checksum failed: 0x{pcbs[passData.socket].checksum_acc:X}");
                    dataInBus.invalidate = true;
                }
            }

            Console.WriteLine($"Written: {(char)segmentBusIn.data}");
        }

        void Send()
        {
            // TODO
        }

        private void Control()
        {
            if (transportBus.valid)
            {
                if (transportBus.socket < 0 || transportBus.socket > pcbs.Length)
                {
                    ControlReturn(transportBus.interface_function,
                            transportBus.socket,
                            ExitStatus.EINVAL);
                    return;
                }

                switch (transportBus.interfaceFunction)
                {
                    case InterfaceFunction.INVALID:
                    default:
                        ControlReturn(transportBus.interface_function, 0,
                                ExitStatus.EINVAL);
                        LOGGER.DEBUG("Wrong interfaceFunction in Transport!");
                        break;

                    /*
                       case InterfaceFunction.ACCEPT:
                    // TODO
                    break;

                    case InterfaceFunction.BIND: // Ignored?
                    // TODO
                    break;

                    case InterfaceFunction.CONNECT:
                    // TODO
                    break;
                    */

                    case InterfaceFunction.OPEN:
                        uint socket = GetFreePCB();
                        if (socket < 0)
                        {
                            ControlReturn(transportBus.interface_function,
                                    transportBus.socket,
                                    ExitStatus.ENOSPC);
                            return;
                        }

                        ResetPCB(socket);

                        pcbs[socket].state = PCB_STATE.OPEN;
                        pcbs[socket].protocol = transportBus.args.protocol;
                        pcbs[socket].l_port = transportBus.args.port;

                        switch (pcbs[socket].protocol)
                        {
                            case (byte)IPv4.Protocol.TCP:
                                // TODO: Start handshake here
                                break;
                        }

                        ControlReturn(transportBus.interface_function,
                                socket,
                                ExitStatus.OK);
                        break;


                    case InterfaceFunction.CLOSE:
                        switch (pcbs[transportBus.args.socket].protocol)
                        {
                            case (byte)IPv4.Protocol.TCP:
                                // TODO: TCP Finish sequence
                                break;
                        }

                        pcbs[transportBus.socket].state = PCB_STATE.CLOSED;
                        break;

                    case InterfaceFunction.LISTEN:
                        pcbs[transportBus.socket].state = PCB_STATE.LISTENING;
                        break;
                }
            }
        }

        private void ControlReturn(byte interface_function, uint socket, uint exit_status)
        {
            transportControlBus.valid = true;
            transportControlBus.interface_function = interface_function;
            transportControlBus.socket = socket;
            transportControlBus.exit_status = exit_status;
        }


        void StartPass(int socket, uint ip_id, uint tcp_seq, uint length)
        {
            Console.WriteLine("Starting to Pass!");
            passData.socket = socket;
            passData.ip_id = ip_id;
            passData.tcp_seq = tcp_seq;
            passData.length = length;

            state = TransportProcessState.Passing;

        }

        ////////////////////////// Helper functions ///////////////////////////
        private uint GetFreePCB()
        {
            for (uint i = 0; i < pcbs.Length; i++)
            {
                if (pcbs[i] == PCB_STATE.CLOSED)
                {
                    return i;
                }
            }
            return -1;
        }


        private void ResetPCB(uint socket)
        {
            pcbs[socket].bytes_received = 0;
            pcbs[socket].checksum_acc = 0;
            pcbs[socket].f_address = 0;
            pcbs[socket].f_port = 0;
            pcbs[socket].l_address = 0;
            pcbs[socket].l_port = 0;
            pcbs[socket].protocol = 0;
            pcbs[socket].state = 0;
        }
    }
}
