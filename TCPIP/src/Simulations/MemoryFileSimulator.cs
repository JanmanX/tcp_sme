using System;
using System.IO;
using System.Threading.Tasks;

using SME;
using SME.VHDL;

namespace TCPIP
{
    public class MemoryFileSimulatior : SimulationProcess
    {
        // ram
        private readonly SME.Components.TrueDualPortMemory<uint> m_ram;

		[OutputBus]
		public readonly Network.FrameBus frameBus = Scope.CreateBus<Network.FrameBus>();

        [OutputBus]
        private readonly SME.Components.TrueDualPortMemory<uint>.IControlA m_controla;

        [InputBus]
        private readonly SME.Components.TrueDualPortMemory<uint>.IReadResultA m_rda;

        [InputBus]
        public readonly SME.Components.TrueDualPortMemory<uint>.IControlB m_controlb;

        [OutputBus]
        public readonly SME.Components.TrueDualPortMemory<uint>.IReadResultB m_rdb;

        [InputBus]
        public readonly Network.NetworkStatusBus networkStatusBus = Scope.CreateBus<Network.NetworkStatusBus>();

        // Getters
		// Reserved!
        // public SME.Components.TrueDualPortMemory<uint>.IControlA GetControlA()
        // {
        //     return m_controla;
        // }
        public SME.Components.TrueDualPortMemory<uint>.IReadResultB getIReadResultB()
        {
            return m_rdb;
        }


        public MemoryFileSimulatior(String memoryFile)
            : base()
        {
            uint[] initial_uints;

            try
            {
                using (var fileStream = File.OpenRead(memoryFile))
                {
                    FileInfo fileInfo = new System.IO.FileInfo(memoryFile);
                    initial_uints = new uint[fileInfo.Length];

                    for (var i = 0; i < initial_uints.Length; i++)
                    {
                        initial_uints[i] = VHDLHelper.CreateIntType<uint>((ulong)fileStream.ReadByte());
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Could not read input memoryFile {memoryFile}");
                return;
            }

            m_ram = new SME.Components.TrueDualPortMemory<uint>(initial_uints.Length, initial_uints);
            m_controla = m_ram.ControlA;
            m_rda = m_ram.ReadResultA;
            m_controlb = m_ram.ControlB;
            m_rdb = m_ram.ReadResultB;

            Console.WriteLine($"Initialized dual-port ram with {initial_uints.Length} uints.");
        }

        public override async Task Run()
        {
			await ClockAsync();

			// Announce new packet
			frameBus.Addr = (uint)0x00;
			m_controlb.Enabled = true;
			m_controlb.IsWriting = false;
			await ClockAsync();

			for(uint i = 0; i < 100; i++) {
				Console.WriteLine("Memory says tick");
				await ClockAsync();
			}
        }

    }
}