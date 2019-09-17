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
        public BufferProducerControlBus packetInBufferProducerControlBusIn;
        [InputBus]
        public PacketIn.ReadBus packetInBus;
        [OutputBus]
        public ConsumerControlBus packetInBufferConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();



        // PacketOut
        [OutputBus]
        public ComputeProducerControlBus packetOutComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [OutputBus]
        public PacketOut.WriteBus packetOutWriteBus = Scope.CreateBus<PacketOut.WriteBus>();
        [InputBus]
        public ConsumerControlBus packetOutComputeConsumerControlBusIn;

        // DataOut
        [InputBus]
        public DataOut.ReadBus dataOutReadBus;
        [InputBus]
        public BufferProducerControlBus dataOutBufferProducerControlBusIn;
        [OutputBus]
        public readonly ConsumerControlBus dataOutBufferConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();

        // DataIn
        [OutputBus]
        public readonly DataIn.WriteBus dataInWriteBus = Scope.CreateBus<DataIn.WriteBus>();
        [OutputBus]
        public readonly ComputeProducerControlBus dataInComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [InputBus]
        public ConsumerControlBus dataInComputeConsumerControlBusIn;

        // Interface
        [InputBus]
        public Interface.InterfaceBus interfaceBus;
        [OutputBus]
        public readonly Interface.InterfaceControlBus interfaceControlBus = Scope.CreateBus<Interface.InterfaceControlBus>();

        // Local variables
        public enum TransportProcessState
        {
            Receive,  // Reading an incoming packet
            Pass,    // Passing data of an incoming packet to a buffer (Data_in)
            Send,    // Sending a data of a packet out
            Control,    // Control work on connections (handshakes, conn. termination)
            Finish,     // Intermediate transition state to idle (we do not want to reset busses in _this_ cycle)
            Idle,     // Nothing to do
        }
        public TransportProcessState state = TransportProcessState.Idle;

        private const uint NUM_SOCKETS = 10;
        private PCB[] pcbs = new PCB[NUM_SOCKETS];

        private const int BUFFER_IN_SIZE = 100;
        private byte[] buffer_in = new byte[BUFFER_IN_SIZE];
        private uint idx_in = 0x00;
        private bool read = true; // Indicates whether process is writing from local buffer

        // Structure to hold information about the data being passed and additional state data
        // Does not necessarily use all fields
        private struct StateData
        {
            public int socket;
            public uint sequence;
            public uint length;

            // Local info
            public byte high_byte; // High byte for checksum calculation
            public uint bytes_passed; // Number of bytes passed
            public uint checksum_acc;
            public long frame_number;
        }
        private StateData stateData;
        private uint ip_id = 0x00; // Current ip_id

        // WRITE
        private readonly int MAX_PACKET_DATA_SIZE;
        private const int BUFFER_OUT_SIZE = 100;
        private byte[] buffer_out = new byte[BUFFER_OUT_SIZE];
        private uint idx_out = 0x00;
        private bool sending_header = false;

        private bool offset_received = false;

        public Transport(int max_packet_size = 8)
        {
            // ...
            this.MAX_PACKET_DATA_SIZE = max_packet_size;
            // DEBUG: Debug sockets
            pcbs[0].state = (byte)PCB_STATE.CONNECTED;
            pcbs[0].protocol = (byte)IPv4.Protocol.UDP;
            pcbs[0].f_address = 0x12345678; //0x0A000002; // 10.0.0.1
            pcbs[0].f_port = 6666;
            pcbs[0].l_address = 0xBCDE0123; // 10.0.0.1
            pcbs[0].l_port = 6789;

            pcbs[1].state = (byte)PCB_STATE.CONNECTED;
            pcbs[1].protocol = (byte)IPv4.Protocol.UDP;
            pcbs[1].f_address = 0x0A000002; // 10.0.0.1
            pcbs[1].f_port = 6666;
            pcbs[1].l_address = 0x0A000001; // 10.0.0.1
            pcbs[1].l_port = 6543;
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
                case TransportProcessState.Finish:
                    StartIdle();
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
            //Logging.log.Info($"packet in valid? {packetInBufferProducerControlBusIn.valid}");
            if (packetInBufferProducerControlBusIn.valid)
            {
                StartReceive();
            }
            //     else if (interfaceBus.valid)
            //     {
            //         StartControl();
            //     }
            else if (dataOutBufferProducerControlBusIn.valid)
            {
                Logging.log.Trace("dataOutProducerControlBus.available!");
                StartSend();
            }

        }

        // Reverts to idle in the next clock, so as to not loose (valid) data in the busses
        private void Finish()
        {
            state = TransportProcessState.Finish;
        }

        private void StartReceive()
        {
            ResetAllBusses();

            state = TransportProcessState.Receive;
            Logging.log.Info("Start receive");
            // Ready
            packetInBufferConsumerControlBusOut.ready = true;

            // Internal variables
            read = true;
            idx_in = 0;
        }

        void Receive()
        {
            // If invalid, reset
            if (packetInBufferProducerControlBusIn.valid == false)
            {
                StartIdle();
                return;
            }

            // If we are receiving a new packet
            if (packetInBus.ip_id != ip_id)
            {
                Logging.log.Info($"Reset! old ip_id: 0x{ip_id:X2} new: 0x{packetInBus.ip_id:X2}");
                ip_id = packetInBus.ip_id;
                read = true;
                idx_in = 0;
            }

            Logging.log.Info($"Got receive data: 0x{packetInBus.data:X2} ip_id: 0x{packetInBus.ip_id:X2}");

            if (idx_in < buffer_in.Length)
            {
                packetInBufferConsumerControlBusOut.ready = true;
                buffer_in[idx_in++] = packetInBus.data;

            }
            // Processing
            switch (packetInBus.protocol)
            {
                case (byte)IPv4.Protocol.TCP:
                    // End of header, start parsing
                    if (idx_in == TCP.HEADER_SIZE)
                    {
                        Logging.log.Info("TCP CURRENTLY NOT SUPPORTED!");
                        // ParseTCP();
                    }
                    break;

                case (byte)IPv4.Protocol.UDP:
                    if (idx_in == UDP.HEADER_SIZE)
                    {
                        Logging.log.Info("Parsing udp");
                        ParseUDP();
                    }
                    break;

                case (byte)IPv4.Protocol.ICMP:
                    if (idx_in == packetInBus.data_length)
                    {
                        Logging.log.Info("Parsing icmp");
//                        ParseICMP();
                        Logging.log.Warn("ICMP CURRENTLY NOT SUPPORTED!");
                    }
                    break;
            }
        }

        private void StartPass(int pcb_idx, uint ip_id, uint sequence, uint length)
        {
            state = TransportProcessState.Pass;

            stateData.socket = pcb_idx;
            stateData.sequence = sequence;
            stateData.length = length;
            ///// Hack to get the framenumber in the state
            stateData.frame_number = packetInBus.frame_number;
            stateData.bytes_passed = 0;

            // Set busses
            ResetAllBusses();
            packetInBufferConsumerControlBusOut.ready = true;
        }

        void Pass()
        {
            Logging.log.Info($"Passing packetIn valid: {packetInBufferProducerControlBusIn.valid} " +
                             $"dataIn ready: {dataInComputeConsumerControlBusIn.ready} " +
                             $"bytes left packetIn: {packetInBufferProducerControlBusIn.bytes_left} " +
                             $"data in bus: 0x{packetInBus.data:X2} " +
                             $"frame_number: {packetInBus.frame_number}");
            // If packetIn suddenly invalid, start idle
            if (packetInBufferProducerControlBusIn.valid == false)
            {
                StartIdle();
                return;
            }

            // if DataIn not ready, abort and start idle
            if (dataInComputeConsumerControlBusIn.ready == false)
            {
                StartIdle();
                return;
            }

            // First ready value is ignored, since we are getting data from
            // an buffer.

            // calculate partial checksum
            if (stateData.bytes_passed % 2 == 0)
            {
                pcbs[stateData.socket].checksum_acc +=
                    (uint)((stateData.high_byte << 8) | packetInBus.data);
            }
            else
            {
                stateData.high_byte = packetInBus.data;
            }

            // Set control bus values
            dataInComputeProducerControlBusOut.valid = true;
            dataInComputeProducerControlBusOut.bytes_left = stateData.length - stateData.bytes_passed;

            // data bus values
            dataInWriteBus.socket = stateData.socket;
            dataInWriteBus.sequence = stateData.sequence;
            dataInWriteBus.data = packetInBus.data;
            dataInWriteBus.data_length = (int)stateData.length;
            dataInWriteBus.invalidate = false;
            dataInWriteBus.frame_number = stateData.frame_number;
            // XXX Should look up in the PCB for the last sequence we can use
            dataInWriteBus.highest_sequence_ready = stateData.sequence;
            stateData.bytes_passed++;


            // If last byte
            if (packetInBufferProducerControlBusIn.bytes_left == 0)
            {
                // Finish checksum
                pcbs[stateData.socket].checksum_acc = ((pcbs[stateData.socket].checksum_acc & 0xFFFF)
                        + (pcbs[stateData.socket].checksum_acc >> 0x10));

                dataInComputeProducerControlBusOut.bytes_left = 0;
                //                if (pcbs[passData.socket].checksum_acc != 0)
                //                {
                //                    Console.WriteLine($"Checksum failed: 0x{pcbs[passData.socket].checksum_acc:X}");
                //                    dataInWriteBus.invalidate = true;
                //                }



                // Go to idle
                Logging.log.Trace("Passing done");
                packetInBufferConsumerControlBusOut.ready = false;
                Finish();
            }
        }

        private void StartSend()
        {
            // Set busses
            ResetAllBusses();
            dataOutBufferConsumerControlBusOut.ready = true;

            state = TransportProcessState.Send;

            stateData.bytes_passed = 0;
            stateData.checksum_acc = 0;

            idx_out = 0;
            sending_header = false;
        }

        private void Send()
        {
            Logging.log.Debug("Sending data!");
            if (dataOutBufferProducerControlBusIn.valid
                && stateData.bytes_passed < MAX_PACKET_DATA_SIZE
                && sending_header == false)
            {
                SendData();

                if(stateData.bytes_passed >= MAX_PACKET_DATA_SIZE)
                {
                    dataOutBufferConsumerControlBusOut.ready = false;
                }
            }
            else
            {
                if (sending_header == false)
                { // SendData() finished. Build the header
                    BuildHeader();

                    dataOutBufferConsumerControlBusOut.ready = false;
                }

                sending_header = true;

                SendHeader();
            }

        }

        private void SendData()
        {
            packetOutComputeProducerControlBusOut.valid = true;
            packetOutComputeProducerControlBusOut.bytes_left = 1; // at least one more

            // XXXX Should index what pcb to gather data from, and stuff like the address offset based on that

            packetOutWriteBus.data = dataOutReadBus.data;
            packetOutWriteBus.addr = (int)(UDP.HEADER_SIZE + stateData.bytes_passed++); // XXX: hardcoded for UDP fixed size header

            // Update accumulated checksum
            stateData.checksum_acc += dataOutReadBus.data;

            // Local info
            stateData.socket = dataOutReadBus.socket;

        }

        private void BuildHeader()
        {
            if (stateData.socket < 0 || stateData.socket > pcbs.Length)
            {
                // XXX
                Console.WriteLine("Trying to send from invalid socket");

                StartIdle();
                return;
            }
            switch (pcbs[stateData.socket].protocol)
            {
                case (byte)IPv4.Protocol.UDP:
                    BuildHeaderUDP(stateData);
                    break;
                case (byte)IPv4.Protocol.ICMP:
                    BuildHeaderUDP(stateData);
                    break;

                default:
                    Console.WriteLine($"Unsupported protocol for sending: {pcbs[stateData.socket].protocol}");
                    StartIdle();
                    break;
            }
        }

        private void SendHeader()
        {
            packetOutComputeProducerControlBusOut.valid = true;
            packetOutComputeProducerControlBusOut.bytes_left = 1;

            packetOutWriteBus.data = buffer_out[idx_out];
            packetOutWriteBus.addr = (int)idx_out;
            idx_out++;

            if (idx_out == UDP.HEADER_SIZE)
            {
                packetOutComputeProducerControlBusOut.bytes_left = 0; // this is the last byte

		        Logging.log.Info($"last byte: 0x{buffer_out[idx_out -1 ]:X2}");
		        dataOutBufferConsumerControlBusOut.ready = false;
                Finish();
            }
        }

        private void StartControl()
        {
            ResetAllBusses();

            state = TransportProcessState.Control;
        }

        private void Control()
        {
            // Variables
            InterfaceData response;
            response.ip = 0;
            response.port = 0;
            response.protocol = 0;
            response.socket = 0;

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
                        (byte)ExitStatus.EINVAL,
                        response,
                        interfaceBus.request);
                return;
            }

            // Go idle if request invalid
            if (interfaceBus.valid == false)
            {
                StartIdle();
                return;
            }

            switch (interfaceBus.interface_function)
            {
                case (byte)InterfaceFunction.INVALID:
                default:
                    ControlReturn(interfaceBus.interface_function,
                            (byte)ExitStatus.EINVAL,
                            response,
                            interfaceBus.request);
                    return;

                case (byte)InterfaceFunction.LISTEN:
                    {
                        int socket = GetFreePCB();

                        // no socket available
                        if (socket < 0)
                        {
                            ControlReturn(interfaceBus.interface_function,
                                    (byte)ExitStatus.ENOSPC,
                                    response,
                                    interfaceBus.request);
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
                                    (byte)ExitStatus.EPROTONOSUPPORT,
                                    response,
                                    interfaceBus.request);
                                return;
                        }

                        ControlReturn(interfaceBus.interface_function,
                                (byte)ExitStatus.OK,
                                response,
                                interfaceBus.request);
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
                                    (byte)ExitStatus.ENOSPC,
                                    response,
                                    interfaceBus.request);
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
                                    (byte)ExitStatus.EPROTONOSUPPORT,
                                    response,
                                    interfaceBus.request);
                                return;
                        }

                        ResetPCB(socket);

                        response.socket = socket;
                        ControlReturn(interfaceBus.interface_function,
                                (byte)ExitStatus.OK,
                                response,
                                interfaceBus.request);
                        break;
                    }

                case (byte)InterfaceFunction.CLOSE:
                    {
                        switch (pcbs[interfaceBus.request.socket].protocol)
                        {
                            case (byte)IPv4.Protocol.TCP:
                                // TODO: TCP Finish sequence
                                break;
                        }

                        pcbs[interfaceBus.request.socket].state = (byte)PCB_STATE.CLOSED;
                        break;

                        pcbs[interfaceBus.request.socket].state = (byte)PCB_STATE.CLOSED;
                        break;


                    }
            }
        }

        private void ControlReturn(byte interface_function, byte exit_status,
                                    InterfaceData response,
                                    InterfaceData request)
        {
            interfaceControlBus.valid = true;
            interfaceControlBus.interface_function = interface_function;
            interfaceControlBus.response = response;
            interfaceControlBus.request = request;
            interfaceControlBus.exit_status = exit_status;

            Finish();
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
            packetInBufferConsumerControlBusOut.ready = false;

            // DataOut
            dataOutBufferConsumerControlBusOut.ready = false;

            // DataIn
            dataInComputeProducerControlBusOut.valid = false;

            // Interface
            interfaceControlBus.valid = false;

            // PacketOut
            packetOutComputeProducerControlBusOut.valid = false;
        }
    }
}
