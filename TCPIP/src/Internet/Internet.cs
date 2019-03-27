using System;
using System.Threading.Tasks;
using SME;

namespace TCPIP
{
    [ClockedProcess]
    public partial class Internet : Process
    {
        [InputBus]
        private readonly Internet.DatagramBus datagramBus;

        [OutputBus]
        public readonly Transport.SegmentBus segmentBus = Scope.CreateBus<Transport.SegmentBus>();


        public Internet(Internet.DatagramBus datagramBus)
        {
            this.datagramBus = datagramBus ?? throw new ArgumentNullException(nameof(datagramBus));
        }

        public override async Task Run()
        {
            while (true)
            {
                while (datagramBus.Ready == false)
                {
                    await ClockAsync();
                }

                switch (datagramBus.Type)
                {
                    case (ushort)EtherType.IPv4:
                        SimulationOnly(() =>
                        {
                            Console.WriteLine("IPv4 packet received");
                        });

                        parseIPv4();
                        break;

                    case (ushort)EtherType.IPv6:
                    case (ushort)EtherType.ARP:
                        SimulationOnly(() =>
                        {
                            Console.WriteLine("packet type not supported");
                        });
                        break;


                    default:
                        SimulationOnly(() =>
                        {
                            Console.WriteLine("unknown packet type:");
                            Console.WriteLine(datagramBus.Type.ToString());
                        });

                        break;
                }
                segmentBus.Addr = datagramBus.Addr;

                return;

            }
        }

        protected void parseIPv4()
        {

        }
    }

}