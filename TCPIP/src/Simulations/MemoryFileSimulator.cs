using System;
using System.IO;
using System.Threading.Tasks;

using SME;
using SME.VHDL;

namespace TCPIP
{
    public class MemoryFileSimulatior : SimulationProcess
    {
        private readonly EightPortMemory<uint> ram;

		[OutputBus]
		public readonly Network.FrameBus frameBus = Scope.CreateBus<Network.FrameBus>();

#region PRIVATE BECAUSE RESERVED
        [OutputBus]
        private readonly EightPortMemory<uint>.IControlA controlA;

        [InputBus]
        private readonly EightPortMemory<uint>.IReadResultA readResultA;
#endregion


        [InputBus]
        public readonly EightPortMemory<uint>.IControlB controlB;

        [OutputBus]
        public readonly EightPortMemory<uint>.IReadResultB readResultB;

        [InputBus]
        public readonly EightPortMemory<uint>.IControlC controlC;

        [OutputBus]
        public readonly EightPortMemory<uint>.IReadResultC readResultC;

        [InputBus]
        public readonly EightPortMemory<uint>.IControlD controlD;

        [OutputBus]
        public readonly EightPortMemory<uint>.IReadResultD readResultD;

        [InputBus]
        public readonly EightPortMemory<uint>.IControlE controlE;

        [OutputBus]
        public readonly EightPortMemory<uint>.IReadResultE readResultE;

        [InputBus]
        public readonly EightPortMemory<uint>.IControlF controlF;

        [OutputBus]
        public readonly EightPortMemory<uint>.IReadResultF readResultF;

        [InputBus]
        public readonly EightPortMemory<uint>.IControlH controlH;

        [OutputBus]
        public readonly EightPortMemory<uint>.IReadResultH readResultH;


        [InputBus]
        public readonly Network.NetworkStatusBus networkStatusBus = Scope.CreateBus<Network.NetworkStatusBus>();


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

            ram = new EightPortMemory<uint>(initial_uints.Length, initial_uints);
            controlA = ram.ControlA;
            readResultA = ram.ReadResultA;
            controlB = ram.ControlB;
            readResultB = ram.ReadResultB;
            controlC = ram.ControlC;
            readResultC = ram.ReadResultC;
            controlD = ram.ControlD;
            readResultD = ram.ReadResultD;
            controlE = ram.ControlE;
            readResultE = ram.ReadResultE;
            controlF = ram.ControlF;
            readResultF = ram.ReadResultF;
            controlH = ram.ControlH;
            readResultH = ram.ReadResultH;

            Console.WriteLine($"Initialized dual-port ram with {initial_uints.Length} uints.");
        }

        public override async Task Run()
        {
			await ClockAsync();

			// Announce new packet
			frameBus.Addr = (uint)0x00;
			controlB.Enabled = true;
			controlB.IsWriting = false;
			await ClockAsync();

			for(uint i = 0; i < 100; i++) {
				Console.WriteLine("Memory says tick");
				await ClockAsync();
			}
        }

    }
}