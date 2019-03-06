using System;
using System.IO;
using System.Threading.Tasks;

using SME;

namespace TCPIP
{
	public class NetworkMemory: SimulationProcess
	{
		// Consts
		public const int SIZE = 8196; 

		[InputBus, OutputBus]
        public IMemoryInterface Interface = Scope.CreateBus<IMemoryInterface>();

		private byte[] data;
		private int cycle = 0;

		public NetworkMemory(String memoryFile)
			: base()
		{
			data = new byte[SIZE];

			try 
			{
				using(var fileStream = File.OpenRead(memoryFile)) 
				{
					int bytesRead = fileStream.Read(data, 0, NetworkMemory.SIZE);
					Console.WriteLine($"Initialized NetworkMemory with {bytesRead} bytes");
				}
			} 
			catch(Exception ex) {
				Console.WriteLine($"Could not read input memoryFile {memoryFile}");
				return;				
			}
		}

		public async override Task Run()
		{
			while (true)
			{
				await ClockAsync();

                PrintDebug("Phase: {0}", ++cycle);

				if (Interface.ReadEnabled)
				{
					PrintDebug("Setting readvalue to {0}", data[Interface.ReadAddr]);
					Interface.ReadValue = data[Interface.ReadAddr];
				}

				if (Interface.WriteEnabled)
				{
					data[Interface.WriteAddr] = Interface.WriteValue;
				}
			}
		}
	}
}