using System;
using SME;
using SME.VHDL;
using SME.Components;

namespace TCPIP
{
    [ClockedProcess]
    public partial class Transport : SimpleProcess
    {
        [InputBus]
        private readonly Transport.SegmentBusIn segmentBusIn;

        [OutputBus]
        public readonly SegmentBusInControl segmentBusInControl
                    = Scope.CreateBus<SegmentBusInControl>();

        [InputBus]
        public TransportBus transportBus;

        [OutputBus]
        public readonly TransportControlBus transportControlBus = Scope.CreateBus<TransportControlBus>();


        [OutputBus]
        public readonly DataInBus dataInBus = Scope.CreateBus<DataInBus>();


        // Local variables
        // READING AND PASSING
        private enum TransportProcessState {
            Reading,  // Reading incoming data into internal buffer (reading headers)
            Passing,    // Passing data (mostly data-section of packets) to the underlying buffer-processes
        }
        private TransportProcessState state = TransportProcessState.Reading;

        private const uint NUM_PCB = 10;
        private PCB[] pcbs = new PCB[NUM_PCB];

        private const int BUFFER_SIZE = 100;
        private byte[] buffer_in = new byte[BUFFER_SIZE];
        private uint idx_in = 0x00;
        private bool read = true; // Inidicates whetehr process is writing from local buffer

        private struct PassData {
            public int socket;
            public uint ip_id;
            public uint tcp_seq;
            public uint length;

            // Local info
            public byte high_byte; // High byte for checksum calculation
            public uint bytes_sent; // Number of bytes sent
        }
        private PassData passData;
        private uint ip_id = 0x00; 

        // WRITE
        private byte[] buffer_out = new byte[BUFFER_SIZE];
        private uint idx_out = 0x00;
        private bool write = false; // Inidicates whetehr process is writing from local buffer

        public Transport(Transport.SegmentBusIn segmentBusIn)
        {
            this.segmentBusIn = segmentBusIn ?? throw new ArgumentNullException(nameof(segmentBusIn));
        }


        void Read()
        {
            if (read && idx_in < BUFFER_SIZE)
            {
                buffer_in[idx_in++] = segmentBusIn.data;

                // Processing
                switch (segmentBusIn.protocol)
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

        void Pass()
        {
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

                if(pcbs[passData.socket].checksum_acc == 0) {
                    dataInBus.finished = true;
                } else {
                    Console.WriteLine($"Checksum failed: 0x{pcbs[passData.socket].checksum_acc:X}");
                    dataInBus.invalidate = true;
                }
            } 

            Console.WriteLine($"Written: {(char)segmentBusIn.data}");
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

        void Receive()
        {
            // If invalid, skip
            if(segmentBusIn.valid == false) {
                dataInBus.valid = false;
                return;
            }

            // If new segment received, reset
            if (segmentBusIn.ip_id != ip_id)
            {
                LOGGER.INFO("New segment!");
                ip_id = segmentBusIn.ip_id;
                idx_in = 0x00;
                state = TransportProcessState.Reading;
                read = true;
                dataInBus.valid = false;
            }

            switch (state)
            {
                case TransportProcessState.Reading:
                    Read();
                    break;

                case TransportProcessState.Passing:
                    Pass();
                    break;
            }
        }

        void Send()
        {
            // TODO
        }

        private void Control()
        {
            if(transportBus.valid)  {
                if(transportBus.socket < 0 || transportBus.socket > pcbs.Length) {
                    ControlReturn(transportBus.interface_function, 
                                    transportBus.socket,
                                    ExitStatus.EINVAL);
                    return;
                }

                switch(transportBus.interfaceFunction) 
                {
                    case InterfaceFunction.INVALID:
                    default:
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

                        switch(pcbs[socket].protocol) {
                            case (byte)IPv4.Protocol.TCP:
                                // TODO: Start handshake here
                                break;
                        }

                        ControlReturn(transportBus.interface_function,
                                        socket,
                                        ExitStatus.OK);
                        break;


                    case InterfaceFunction.CLOSE:
                        switch(pcbs[transportBus.args.socket].protocol) {
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



        protected override void OnTick()
        {
            Receive();
            Send();
            Control();
        }


        // Helper functions
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
