using System;
using SME;

namespace TCPIP
{
    partial class Interface : SimpleProcess
    {
        public enum InterfaceFunction : byte
        {
            INVALID = 0,
            BIND = 1,
            LISTEN = 2,
            CONNECT = 3,
            ACCEPT = 4,
            SEND = 5,
            RECV = 6,
            CLOSE = 7,
            // ...
            OPEN = 255,
        }

        public struct InterfaceData
        {
            public byte interface_function;
            public int socket;
            public byte data;
        }

        public struct InterfaceArgs
        {
            public uint socket;
            public uint ip;
            public byte protocol;
            public ushort port;
        }


        [InputBus]
        public readonly InterfaceBus interfaceBus;

        [OutputBus]
        public readonly InterfaceBus interfaceBusOut = Scope.CreateBus<InterfaceBus>();

        [InputBus]
        public readonly Transport.TransportControlBus transportControlBus;

        [OutputBus]
        public readonly Transport.TransportBus transportBus = Scope.CreateBus<Transport.TransportBus>();

        public Interface(InterfaceBus interfaceBus, Transport.TransportControlBus transportControlBus)
        {
            this.interfaceBus = interfaceBus ?? throw new ArgumentNullException(nameof(interfaceBus));
            this.transportControlBus = transportControlBus ?? throw new ArgumentNullException(nameof(transportControlBus));
        }

        protected override void OnTick()
        {
            if (interfaceBus.valid)
            {
                switch (interfaceBus.interfaceFunction)
                {
                    case (byte)InterfaceFunction.INVALID:
                        // TODO
                        break;

                    case (byte)InterfaceFunction.ACCEPT:
                        // TODO
                        break;

                    case (byte)InterfaceFunction.BIND:
                        // TODO
                        break;

                    case (byte)InterfaceFunction.CONNECT:
                        // TODO
                        break;

                    case (byte)InterfaceFunction.CLOSE:
                        // TODO
                        break;

                    case (byte)InterfaceFunction.LISTEN:
                        // TODO
                        break;
                    case (byte)InterfaceFunction.RECV:
                        // TODO
                        break;
                    case (byte)InterfaceFunction.SEND:
                        // TODO
                        break;
                }
            }

        }
    }
}