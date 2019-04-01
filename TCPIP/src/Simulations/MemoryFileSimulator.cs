using System;
using System.IO;
using System.Threading.Tasks;

using SME;
using SME.Components;
using SME.VHDL;

namespace TCPIP
{
    public class MemoryFileSimulatior<T> : SimulationProcess
    {
        private readonly TrueDualPortMemory<T> ram;

		[OutputBus]
		public readonly Network.FrameBus frameBus = Scope.CreateBus<Network.FrameBus>();

#region PRIVATE BECAUSE RESERVED
        [OutputBus]
        private readonly TrueDualPortMemory<T>.IControlA controlA;

        [InputBus]
        private readonly TrueDualPortMemory<T>.IReadResultA readResultA;
#endregion

        [OutputBus]
        private readonly TrueDualPortMemory<T>.IControlB controlb;

        [InputBus]
        private readonly TrueDualPortMemory<T>.IReadResultB readResultB;


        /*
        public TrueDualPortMemory<T>.IControlB GetControlB() {
            return ram.ControlB;
        }
        public TrueDualPortMemory<T>.IReadResultB GetReadResultB() {
            return ram.ReadResultB;
        }
        */
   
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

            ram = new TrueDualPortMemory<T>(5000, initial_Ts);

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
        }
    }
}