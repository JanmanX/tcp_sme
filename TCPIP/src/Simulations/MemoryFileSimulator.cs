using System;
using System.IO;
using System.Threading.Tasks;

using SME;
using SME.VHDL;

namespace TCPIP
{
    public class MemoryFileSimulatior<T> : SimulationProcess
    {
        private readonly EightPortMemory<T> ram;

		[OutputBus]
		public readonly Network.FrameBus frameBus = Scope.CreateBus<Network.FrameBus>();

#region PRIVATE BECAUSE RESERVED
        [OutputBus]
        private readonly EightPortMemory<T>.IControlA controlA;

        [InputBus]
        private readonly EightPortMemory<T>.IReadResultA readResultA;
#endregion

        /* Used by Network layer */
        [InputBus]
        public readonly EightPortMemory<T>.IControlB controlB;

        [OutputBus]
        public readonly EightPortMemory<T>.IReadResultB readResultB;

        /* Used by Internet Layer */
        [InputBus]
        public readonly EightPortMemory<T>.IControlC controlC;

        [OutputBus]
        public readonly EightPortMemory<T>.IReadResultC readResultC;

        /* Used by Transport layer (?) */
        [InputBus]
        public readonly EightPortMemory<T>.IControlD controlD;

        [OutputBus]
        public readonly EightPortMemory<T>.IReadResultD readResultD;

        [InputBus]
        public readonly EightPortMemory<T>.IControlE controlE;

        [OutputBus]
        public readonly EightPortMemory<T>.IReadResultE readResultE;

        [InputBus]
        public readonly EightPortMemory<T>.IControlF controlF;

        [OutputBus]
        public readonly EightPortMemory<T>.IReadResultF readResultF;

        [InputBus]
        public readonly EightPortMemory<T>.IControlH controlH;

        [OutputBus]
        public readonly EightPortMemory<T>.IReadResultH readResultH;

        [InputBus]
        public readonly Network.NetworkStatusBus networkStatusBus = Scope.CreateBus<Network.NetworkStatusBus>();


        public MemoryFileSimulatior(String memoryFile)
            : base()
        {
            T[] initial_Ts;

            try
            {
                using (var fileStream = File.OpenRead(memoryFile))
                {
                    FileInfo fileInfo = new System.IO.FileInfo(memoryFile);
                    initial_Ts = new T[fileInfo.Length];

                    for (var i = 0; i < initial_Ts.Length; i++)
                    {
                        initial_Ts[i] = VHDLHelper.CreateIntType<T>((ulong)fileStream.ReadByte());
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Could not read input memoryFile {memoryFile}");
                return;
            }

            ram = new EightPortMemory<T>(5000, initial_Ts);
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

            Console.WriteLine($"Initialized dual-port ram with {initial_Ts.Length} Ts.");


            for(uint i = 0; i < initial_Ts.Length; i++) {
                if (i % 8 == 0)  {
                    Console.WriteLine();
                }
 
                Console.Write(String.Format("0x{0:X} ", initial_Ts[i]));
           }
           Console.WriteLine();

        }

        public override async Task Run()
        {
            while (true)
            {
                await ClockAsync();


                // Announce new packet
                frameBus.Addr = 0x00;
                frameBus.Ready = true;
                await ClockAsync();

                for (uint i = 0; i < 10; i++)
                {
                    Console.WriteLine("Memory says tick");
                    await ClockAsync();
                }
            }

        }
    }
}