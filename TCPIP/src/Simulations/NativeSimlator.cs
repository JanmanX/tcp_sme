using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.Sockets;


using SME;
using SME.Components;


using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Threading;
using System.IO.Pipes;
using Newtonsoft.Json;

namespace TCPIP
{

	internal class IPCData
	{
		public byte function;
		public byte socket;
		public uint ip;
		public ushort protocol;
		public ushort port;
	}

	public class NativeStackSimulator : SimulationProcess
	{
		// Network
		//////// INTERNET IN (Sending to this)
		[OutputBus]
			public Internet.DatagramBusIn datagramBusIn = Scope.CreateBus<Internet.DatagramBusIn>();
		[OutputBus]
			public BufferProducerControlBus datagramBusInBufferProducerControlBusOut = Scope.CreateBus<BufferProducerControlBus>();
		[InputBus]
			public ConsumerControlBus datagramBusInBufferConsumerControlBusIn;


		//////// INTERNET OUT (Receiving from this)
		[InputBus]
			public Internet.DatagramBusOut datagramBusOut;
		[InputBus]
			public ComputeProducerControlBus datagramBusOutComputeProducerControlBusIn;
		[OutputBus]
			public ConsumerControlBus datagramBusOutComputeConsumerControlBusOut =  Scope.CreateBus<ConsumerControlBus>();


		//////// DATA IN (Receiving from this)
		[InputBus]
			public DataIn.ReadBus dataIn;
		[InputBus]
			public BufferProducerControlBus dataInBufferProducerControlBusIn;
		[OutputBus]
			public ConsumerControlBus dataInBufferConsumerControlBusOut = Scope.CreateBus<ConsumerControlBus>();


        //////// DATA OUT (Sending from this)
        [OutputBus]
        public DataOut.WriteBus dataOut = Scope.CreateBus<DataOut.WriteBus>();
        [OutputBus]
        public ComputeProducerControlBus dataOutComputeProducerControlBusOut = Scope.CreateBus<ComputeProducerControlBus>();
        [InputBus]
        public ConsumerControlBus dataOutComputeConsumerControlBusIn;



		// Simulator specific
		private const string TCP_PIPE_NAME = "tcp_sme_pipe";
		NamedPipeServerStream pipeServer;

		// static
		private const string TUN_PIPE_NAME = "/tmp/tun_sme_pipe";
		private const int PORT = 8888;
		private const int RAW_PACKET_OFFSET = 0x04;
		uint ip_id = 0;
		uint frame_number;

		private const int BUFFER_SIZE = 1024;
		byte[] buffer = new byte[BUFFER_SIZE];

		UdpClient udpClient;
		IPEndPoint localEndPoint;



		public NativeStackSimulator()
		{
			Console.WriteLine("=== NATIVE SIMULATOR ===");

			// Connection to the shared library
			pipeServer = new NamedPipeServerStream(TCP_PIPE_NAME, PipeDirection.InOut);

			// Connection the native network stack
			udpClient = new UdpClient(PORT);
			localEndPoint = new IPEndPoint(IPAddress.Any, PORT);
		}



		public override async Task Run()
		{
			// Init
			await ClockAsync();
			await ClockAsync();
			await ClockAsync();
			await ClockAsync();

			// Setup listeners
			pipeServer.BeginWaitForConnection(new AsyncCallback(RequestCallback), this);

			udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), this);


			// Main loop
			while(true)
			{

				await ClockAsync();
			}


			// For good measure
			pipeServer.Close();
		}


		public async void ReceiveCallback(IAsyncResult ar)
		{
			byte[] recv_buffer = udpClient.EndReceive(ar, ref localEndPoint);

			Console.WriteLine($"Received data");
			ushort proto = (ushort)(recv_buffer[2] << 8 | buffer[3]);

			await ClockAsync();

			for (int i = RAW_PACKET_OFFSET; i < recv_buffer.Length; i++)
			{
				Console.Write($"{(char)recv_buffer[i]} ");
				datagramBusIn.frame_number = frame_number;
				datagramBusIn.type = proto;
				datagramBusIn.data = recv_buffer[i];

				await ClockAsync();
			}
			Console.WriteLine();
	
			frame_number++;

			udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), this);
		}

		private int recv_buffer_idx = 0;  // Keeps track of current idx written to the stack


		private async void RecvFromNative()
		{

			do
			{
				buffer = udpClient.Receive(ref localEndPoint);
			} while (buffer.Length == 0);

			ushort proto = (ushort)(buffer[2] << 8 | buffer[3]);


			for (int i = RAW_PACKET_OFFSET; i < buffer.Length; i++)
			{
				datagramBusIn.frame_number = frame_number;
				datagramBusIn.type = proto;
				datagramBusIn.data = buffer[i];

				Console.WriteLine($"{buffer[i]}");

				await ClockAsync();
			}
			frame_number++;


		}

		private async void RequestCallback(IAsyncResult result)
		{
			try
			{
				pipeServer.EndWaitForConnection(result);

				// Parse request
				byte[] buffer = new byte[2048];
				pipeServer.Read(buffer, 0, buffer.Length);
				string str = System.Text.Encoding.UTF8.GetString(buffer);
				IPCData request = JsonConvert.DeserializeObject<IPCData>(str);

				// Do work
				// ...
				Console.WriteLine($"Got request: {request.ip}, {request.socket}");
				Console.WriteLine(str);

				// Response
				IPCData response = new IPCData();
				response.ip = 7331;
				str = JsonConvert.SerializeObject(response);
				buffer = Encoding.UTF8.GetBytes(str);
				pipeServer.Write(buffer, 0, buffer.Length);


                // Recursively wait for the connection again and again....
				pipeServer.Close();
        	    pipeServer = null;
    			pipeServer = new NamedPipeServerStream(TCP_PIPE_NAME, PipeDirection.InOut);
	 			pipeServer.BeginWaitForConnection(new AsyncCallback(RequestCallback), this);
            }
            catch
            {
                Console.WriteLine("Exception occured");
                return;
            }
        }
    }

}
