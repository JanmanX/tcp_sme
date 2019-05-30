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
        public BufferProducerControlBus packetInProducerControlBus;
        [InputBus]
        public PacketIn.PacketInBus packetInBus;
        [OutputBus]
        public ConsumerControlBus packetInConsumerControlBus = Scope.CreateBus<ConsumerControlBus>();

        // DataOut
        [InputBus]
        public readonly DataOutReadBus dataOutReadBus;
        [InputBus]
        public readonly BufferProducerControlBus dataOutProducerControlBus;
        [OutputBus]
        public readonly ConsumerControlBus dataOutConsumerControlBus = Scope.CreateBus<ConsumerControlBus>();

        // DataIn
        [OutputBus]
        public readonly DataInWriteBus dataInWriteBus = Scope.CreateBus<DataInWriteBus>();
        [OutputBus]
        public readonly ComputeProducerControlBus dataInProducerControlBus = Scope.CreateBus<ComputeProducerControlBus>();
        [InputBus]
        public ConsumerControlBus dataInConsumerControlBus;

        // Interface
        [InputBus]
        public readonly Interface.InterfaceBus interfaceBus;
        [OutputBus]
        public readonly Interface.InterfaceControlBus interfaceControlBus = Scope.CreateBus<Interface.InterfaceControlBus>();

        // Local variables
        private enum TransportProcessState
        {
            Receive,  // Reading an incoming packet
            Pass,    // Passing data of an incoming packet to a buffer (Data_in)
            Send,    // Sending a data packet out
            Control,    // Control work on connections (handshakes, conn. termination)
            Idle,     // Nothing to do
        }
        private TransportProcessState state = TransportProcessState.Idle;

        private const uint NUM_SOCKETS = 10;
        private PCB[] pcbs = new PCB[NUM_SOCKETS];

        private const int BUFFER_SIZE = 100;
        private byte[] buffer_in = new byte[BUFFER_SIZE];
        private uint idx_in = 0x00;
        private bool read = true; // Indicates whether process is writing from local buffer

        private struct PassData
        {
            public int socket;
            public uint tcp_seq;
            public uint length;

            // Local info
            public byte high_byte; // High byte for checksum calculation
            public uint bytes_passed; // Number of bytes passed
        }
        private PassData passData;
        private uint ip_id = 0x00; // Current ip_id

        // WRITE
        private byte[] buffer_out = new byte[BUFFER_SIZE];
        private uint idx_out = 0x00;
        private bool write = false; // Inidicates whetehr process is writing from local buffer

        public Transport()
        {
            // ... 
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
            ResetAllBusses();

            state = TransportProcessState.Idle;
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
            /*
                        else if (dataOutProducerControlBus.available)
                        {
                            StartSend();
                        }

                        */
        }

        private void StartReceive()
        {
            ResetAllBusses();

            state = TransportProcessState.Receive;

            // Ready
            packetInConsumerControlBus.ready = true;

            // Internal variables
            read = true;
            idx_in = 0;
        }

        void Receive()
        {
            // If invalid, reset
            if (packetInProducerControlBus.valid == false)
            {
                StartIdle();
                return;
            }

            // If we are receiving a new packet
            if (packetInBus.ip_id != ip_id)
            {
                ip_id = packetInBus.ip_id;
                read = true;
                idx_in = 0;
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
                            LOGGER.WARN("TCP CURRENTLY NOT SUPPORTED!");
                            // read = false;
                            // ParseTCP();
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


        private void StartPass(int pcb_idx, uint ip_id, uint tcp_seq, uint length)
        {
            state = TransportProcessState.Pass;

            passData.socket = pcb_idx;
            passData.tcp_seq = tcp_seq;
            passData.length = length;
            passData.bytes_passed = 0;

            // Set busses
            ResetAllBusses();
            packetInConsumerControlBus.ready = true;
        }

        void Pass()
        {
            // If packetIn suddenly invalid, start idle
            if (packetInProducerControlBus.valid == false)
            {
                StartIdle();
                return;
            }

            // if DataIn not ready, abort and start idle
            if (dataInConsumerControlBus.ready == false)
            {
                StartIdle();
                return;
            }

            // calculate partial checksum
            if (passData.bytes_passed % 2 == 0)
            {
                pcbs[passData.socket].checksum_acc +=
                    (uint)((passData.high_byte << 8) | dataInWriteBus.data);
            }
            else
            {
                passData.high_byte = packetInBus.data;
            }

            // Set control bus values
            dataInProducerControlBus.valid = true;
            dataInProducerControlBus.bytes_left = passData.length - passData.bytes_passed;

            // data bus values
            dataInWriteBus.socket = passData.socket;
            dataInWriteBus.tcp_seq = passData.tcp_seq;
            dataInWriteBus.data = packetInBus.data;
            dataInWriteBus.invalidate = false;
            passData.bytes_passed++;


            // If last byte
            if (packetInProducerControlBus.bytes_left == 0)
            {
                // Finish checksum
                pcbs[passData.socket].checksum_acc = ((pcbs[passData.socket].checksum_acc & 0xFFFF)
                        + (pcbs[passData.socket].checksum_acc >> 0x10));

                if (pcbs[passData.socket].checksum_acc != 0)
                {
                    Console.WriteLine($"Checksum failed: 0x{pcbs[passData.socket].checksum_acc:X}");
                    dataInWriteBus.invalidate = true;
                }

                Console.WriteLine("Ending packet");
                // Go to idle
                StartIdle();
            }
        }

        private void StartSend()
        {

        }

        private void Send()
        {
            // TODO
        }

        private void StartControl()
        {
            ResetAllBusses();

            state = TransportProcessState.Control;
        }

        private void Control()
        {
            // Go idle if request invalid
            if (interfaceBus.valid == false)
            {
                StartIdle();
                return;
            }

            // Check for valid socket number
            if (interfaceBus.request.socket < 0 || interfaceBus.request.socket > pcbs.Length)
            {
                ControlReturn(interfaceControlBus.interface_function,
                        (byte)ExitStatus.EINVAL);
                return;
            }


            switch (interfaceBus.interface_function)
            {
                case (byte)InterfaceFunction.INVALID:
                default:
                    ControlReturn(interfaceBus.interface_function,
                            (byte)ExitStatus.EINVAL);
                    return;

                case (byte)InterfaceFunction.LISTEN:
                    {
                        int socket = GetFreePCB();

                        // no socket available
                        if (socket < 0)
                        {
                            ControlReturn(interfaceBus.interface_function,
                                    (byte)ExitStatus.ENOSPC);
                            return;
                        }

                        ResetPCB(socket);

                        pcbs[socket].state = (byte)PCB_STATE.LISTENING;
                        pcbs[socket].protocol = interfaceBus.request.protocol;
                        pcbs[socket].l_port = interfaceBus.request.port;

                        // Do protocol-based operations here
                        switch (pcbs[socket].protocol)
                        {
                            case (byte)IPv4.Protocol.UDP:
                            default: // Protocol not supported. Error
                                ControlReturn(interfaceBus.interface_function,
                                    (byte)ExitStatus.EPROTONOSUPPORT);
                                return;
                        }

                        ControlReturn(interfaceBus.interface_function,
                                (byte)ExitStatus.OK);
                        break;
                    }

                case (byte)InterfaceFunction.ACCEPT:
                    // TODO
                    break;

                case (byte)InterfaceFunction.CONNECT:
                    {
                        int socket = GetFreePCB();

                        // no socket available
                        if (socket < 0)
                        {
                            ControlReturn(interfaceBus.interface_function,
                                    (byte)ExitStatus.ENOSPC);
                            return;
                        }

                        ResetPCB(socket);

                        pcbs[socket].state = (byte)PCB_STATE.CONNECTING;
                        pcbs[socket].protocol = interfaceBus.request.protocol;
                        pcbs[socket].l_port = interfaceBus.request.port;
                        pcbs[socket].f_address = interfaceBus.request.ip;

                        // Do protocol-based operations here
                        switch (pcbs[socket].protocol)
                        {
                            case (byte)IPv4.Protocol.UDP:
                                pcbs[socket].state = (byte)PCB_STATE.CONNECTED;
                                break;


                            case (byte)IPv4.Protocol.TCP:
                            default: // Protocol not supported. Error
                                ControlReturn(interfaceBus.interface_function,
                                    (byte)ExitStatus.EPROTONOSUPPORT);
                                return;
                        }

                        ControlReturn(interfaceBus.interface_function,
                                (byte)ExitStatus.OK);
                        break;
                    }

                case (byte)InterfaceFunction.CLOSE:
                    switch (pcbs[interfaceBus.request.socket].protocol)
                    {
                        case (byte)IPv4.Protocol.TCP:
                            // TODO: TCP Finish sequence
                            break;
                    }

                    pcbs[interfaceBus.request.socket].state = (byte)PCB_STATE.CLOSED;
                    break;


            }
        }

        private void ControlReturn(byte interface_function, byte exit_status,
                                    InterfaceData response = default(InterfaceData),
                                    InterfaceData request = default(InterfaceData))
        {
            interfaceControlBus.valid = true;
            interfaceControlBus.interface_function = interface_function;
            interfaceControlBus.response = response;
            interfaceControlBus.request = request;
            interfaceControlBus.exit_status = exit_status;

            StartIdle();
        }


        ////////////////////////// Helper functions ///////////////////////////
        private int GetFreePCB()
        {
            for (int i = 0; i < pcbs.Length; i++)
            {
                if (pcbs[i].state == (byte)PCB_STATE.CLOSED)
                {
                    return i;
                }
            }
            return -1;
        }


        private void ResetPCB(int socket)
        {
            if (socket < 0 || socket > pcbs.Length)
            {
                return;
            }

            pcbs[socket].bytes_received = 0;
            pcbs[socket].checksum_acc = 0;
            pcbs[socket].f_address = 0;
            pcbs[socket].f_port = 0;
            pcbs[socket].l_address = 0;
            pcbs[socket].l_port = 0;
            pcbs[socket].protocol = 0;
            pcbs[socket].state = 0;
        }

        private void ResetAllBusses()
        {
            // PacketIn
            packetInConsumerControlBus.ready = false;

            // DataOut
            dataOutConsumerControlBus.ready = false;

            // DataIn
            dataInProducerControlBus.valid = false;

            // Interface
            interfaceControlBus.valid = false;

            // TODO: PacketOut
        }
    }
}
