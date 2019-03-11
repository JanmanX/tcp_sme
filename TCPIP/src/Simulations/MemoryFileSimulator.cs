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
        private readonly SME.Components.TrueDualPortMemory<byte> m_ram;

		[OutputBus]
		public readonly Network.FrameBus frameBus = Scope.CreateBus<Network.FrameBus>();

        [OutputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IControlA m_controla;

        [InputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IReadResultA m_rda;

        [InputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IControlB m_controlb;

        [OutputBus]
        private readonly SME.Components.TrueDualPortMemory<byte>.IReadResultB m_rdb;


        // Getters
		// Reserved!
        // public SME.Components.TrueDualPortMemory<byte>.IControlA GetControlA()
        // {
        //     return m_controla;
        // }
        public SME.Components.TrueDualPortMemory<byte>.IReadResultB getIReadResultB()
        {
            return m_rdb;
        }


        public MemoryFileSimulatior(String memoryFile)
            : base()
        {
            byte[] initial_bytes;

            try
            {
                using (var fileStream = File.OpenRead(memoryFile))
                {
                    FileInfo fileInfo = new System.IO.FileInfo(memoryFile);
                    initial_bytes = new byte[fileInfo.Length];

                    for (var i = 0; i < initial_bytes.Length; i++)
                    {
                        initial_bytes[i] = VHDLHelper.CreateIntType<byte>((ulong)fileStream.ReadByte());
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Could not read input memoryFile {memoryFile}");
                return;
            }

            m_ram = new SME.Components.TrueDualPortMemory<byte>(initial_bytes.Length, initial_bytes);
            m_controla = m_ram.ControlA;
            m_rda = m_ram.ReadResultA;
            m_controlb = m_ram.ControlB;
            m_rdb = m_ram.ReadResultB;

            Console.WriteLine($"Initialized dual-port ram with ${initial_bytes.Length} bytes.");
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