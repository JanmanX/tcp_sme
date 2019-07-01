using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TCPIP
{
    // The packet graph file uses specific files that indicate in what order
    // a packet needs to be received or sent.
    //
    // It is seen as an acrylic graph, where the first node is an "event" (send, receive etc).
    // To propegate to the next node, we need to fulfill the node, and go to the next.
    // If there are a split , these tasks are done independently of each other, in no specific order
    //

    // events:
    //  * receive:
    //      A packet that we should receive from the stack.
    //  * send:
    //      A packet that we should send into the stack.
    //  * datain:
    //      Data from the stack to the user application
    //  * dataout:
    //      Data from the user application that goes into the stack
    //  * command:
    //      Defines a command as sent by the application to the stack
    //  * wait:
    //      wait N clockcycles before the node is ready.
    public class PacketGraph  {
        [Flags]
        public enum PacketInfo{
            Send = 1 << 0, // this packet has to be sent.
            Receive = 1 << 1, // This packet has to be received.
            DataIn = 1 << 2,
            DataOut = 1 << 3,
            Command = 1 << 4,
            Wait = 1 << 5,
            Initial = 1 << 6, // Is this an beginning packet?
            End = 1 << 7, // Is this an end packet?
            Valid = 1 << 8, // Has this packet been sent, received or its action done??
            Active = 1 << 9, //  The packet is currently being worked on
        }

        // class defining the nodes in an directed acyclic graph
        // containing the information of the packets to send and receive.
        // The nodes contain the file we send or validate up against.
        // It will only send or receive correctly when all "dependsOn"
        // is marked as good.
        public class Packet
        {
            public int id;
            public PacketInfo info;
            public string dataPath;
            public byte[] data;
            public ushort type;
            public SortedSet<int> dependsOn;
            public SortedSet<int> requiredBy;
        }

        private Dictionary<int,Packet> packetList = new Dictionary<int,Packet>();
        private SortedSet<int> initPackets = new SortedSet<int>();
        private SortedSet<int> exitPackets = new SortedSet<int>();
        private SortedSet<int> packetPointers; // List of packet pointers waiting

        private int clock = 0;
        private string dir;


        public PacketGraph(string dir){
            string[] filePaths = Directory.GetFiles(dir);
            var simPackets = from c in filePaths
                             select GenerateSimPacket(c);
            this.dir = dir;
            // Find the end points in the graph
            var dependsOn = new HashSet<int>();
            var allNodes = new HashSet<int>();
            foreach(Packet simPacket in simPackets){
                allNodes.Add(simPacket.id);
                dependsOn.UnionWith(new HashSet<int>(simPacket.dependsOn));
                packetList.Add(simPacket.id,simPacket);
            }

            // Append the end packets
            exitPackets = new SortedSet<int>(allNodes.Except(dependsOn));
            foreach(int x in exitPackets)
            {
                packetList[x].info |= PacketInfo.End;
            }
            // Calculate which packets are required
            foreach(var x in packetList.OrderBy(a => a.Key))
            {
                var requiredBy = new HashSet<int>();
                foreach(var y in packetList.OrderBy(a => a.Key))
                {
                    if(y.Value.dependsOn.Contains(x.Key)){
                        requiredBy.Add(y.Key);
                    }
                }
                packetList[x.Key].requiredBy = new SortedSet<int>(requiredBy);
            }

            // Print the current packet information
            int maxDepends = 0;
            foreach(var x in packetList.OrderBy(a => a.Key))
            {
                // Get the max dependency for each node
                if(x.Value.dependsOn.Count > maxDepends){
                    maxDepends = x.Value.dependsOn.Count;
                }
            }

            //Add The current packet pointers
            packetPointers =  new SortedSet<int>(initPackets);

            // Test if we need to guess in the design
            if(maxDepends > 1 || initPackets.Count > 1 || exitPackets.Count > 1){
                Logging.log.Warn("The graph may have to guess which node it is receiving data to");
            }
        }

        private Packet GenerateSimPacket(string filePath){
            Logging.log.Trace("Parsing " + filePath);
            var fileName =  Path.GetFileName(filePath);
            var reg = new Regex(@"^(\d*)_?(.*)\-(\w*)\.bin$");
            var match = reg.Match(fileName);
            PacketInfo info = 0;
            var id = Int32.Parse(match.Groups[1].Value);
            var dependsString = match.Groups[2].Value.Split("_");
            var dependsOn = new SortedSet<int>();
            // test if we have found dependencies, it must be an initial packet
            if(dependsString[0].Equals(""))
            {
                initPackets.Add(id);
                info |= PacketInfo.Initial;
            }
            else
            {
                dependsOn = new SortedSet<int>(dependsString.Select(int.Parse));
            }

            var metainfo = match.Groups[3].Value;

            switch(metainfo)
            {
                case "send":
                    info |= PacketInfo.Send;
                    break;
                case "receive":
                    info |= PacketInfo.Receive;
                    break;
                case "datain":
                    info |= PacketInfo.DataIn;
                    break;
                case "dataout":
                    info |= PacketInfo.DataOut;
                    break;
                case "command":
                    info |= PacketInfo.Command;
                    break;
                case "wait":
                    info |= PacketInfo.Wait;
                    break;
                default:
                    Logging.log.Fatal("Packet metainfo not detected: " + fileName);
                    break;
            }
            // Create the packet
            Packet pack = new Packet();
            pack.id = id;
            pack.info = info;
            // if this packet can be sent or received, load the data
            if((pack.info & (PacketInfo.Send |
                             PacketInfo.Receive |
                             PacketInfo.DataIn |
                             PacketInfo.DataOut)) > 0)
            {
                pack.data = File.ReadAllBytes(filePath);
            }
            if((pack.info & (PacketInfo.Send |
                             PacketInfo.Receive)) > 0)
            {
                // Set the packet type based on the byte value
                pack.type = (ushort)(pack.data[EthernetIIFrame.ETHERTYPE_OFFSET_0] << 0x08);
                pack.type |= (ushort)(pack.data[EthernetIIFrame.ETHERTYPE_OFFSET_1]);
            }

            pack.dataPath = filePath;
            pack.dependsOn = dependsOn;
            return pack;
        }

        // Iterate over packet structure, and detect if it is a valid packet
        private bool isReady(int i, bool start){
            var packet =  packetList[i];
            bool accum = true;
            // If we have an packet as initial, it must be ready always at start
            if((packet.info & PacketInfo.Initial) > 0 && start){
                return true;
            }
            // If this is not the start node of the chain, test if it is valid
            if(start == false){
                accum = (packet.info & PacketInfo.Valid) > 0;
            }
            // If the accumulator is false the packet must not be valid, and we return false ealy
            if(accum == false){
                return false;
            }
            // Iterate over the dependencies, and call them when recursively
            foreach (var x in packet.dependsOn){
                accum &= isReady(x,false);
            }
            return accum;

        }
        // Overloaded isReady, so we do no have to set the root flag
        private bool isReady(int i)
        {
            return isReady(i,true);
        }





        ///////////////////////////////////////////////////
        // Public Functions
        private (ushort type,byte data,uint bytes_left) lastSend;
        public IEnumerable<(ushort type,byte data,uint bytes_left)> IterateOverSend(){
            var send = IterateOver(PacketInfo.Send,(int)EthernetIIFrame.HEADER_SIZE).GetEnumerator();
            while(send.MoveNext())
            {
                lastSend = send.Current;
                yield return send.Current;
            }
            yield break;
        }

        private (ushort type,byte data,uint bytes_left) lastDataOut;
        public IEnumerable<(ushort type,byte data,uint bytes_left)> IterateOverDataOut(){
            var dataOut = IterateOver(PacketInfo.DataOut,0).GetEnumerator();
            while(dataOut.MoveNext())
            {
                lastDataOut = dataOut.Current;
                yield return dataOut.Current;
            }
            yield break;
        }

        private IEnumerator<(ushort type,byte data,uint bytes_left)> dataIn;
        private (ushort type,byte data,uint bytes_left) lastDataIn;
        public bool GatherDataIn(byte compare)
        {
            if (dataIn == null){
                dataIn = IterateOver(PacketInfo.DataIn,0).GetEnumerator();
                Logging.log.Fatal($"New iterator! {dataIn.Current.data}");
            }

            if(dataIn.MoveNext()){
                byte excact = dataIn.Current.data;
                lastDataIn = dataIn.Current;
                if(excact == compare)
                {
                    return true;
                }
                else
                {
                    Logging.log.Error($"Wrong comparison of input data from datain. should be 0x{excact:X2} is 0x{compare:X2}");
                    return false;
                }

            }else
            {
                if(ReadyDataIn()){
                    dataIn = null;
                    return GatherDataIn(compare);
                }
            }
            Logging.log.Warn($"No packet found for GatherDataIn. compared to: 0x{compare:X2}");
            return false;
        }

        public bool ReadySend(){return TestPacketReady(PacketInfo.Send);}
        public bool ReadyReceive(){return TestPacketReady(PacketInfo.Receive);}
        public bool ReadyDataIn(){return TestPacketReady(PacketInfo.DataIn);}
        public bool ReadyDataOut(){return TestPacketReady(PacketInfo.DataOut);}
        public bool ReadyWait(){return TestPacketReady(PacketInfo.Wait);}
        public bool ReadyCommand(){return TestPacketReady(PacketInfo.Command);}

        public void NextClock()
        {
            clock++;
        }



        ///////////////////////////////////////////////////
        // Helper Functions

        private IEnumerable<(ushort type,byte data,uint bytes_left)> IterateOver(PacketInfo info, int offset){
            // Get all packets that we have to send currently, and test if they are ready
            var toIterate = packetPointers.Where(
                x => (packetList[x].info & info) > 0 &&
                     isReady(x)
            ).ToList();

            // If there are no elements in the packets to send
            if(toIterate.Count == 0){
                yield break;
            }

            int pid = toIterate.First();
            Logging.log.Trace("PacketID: " + pid + " iterator started");
            Packet p = packetList[pid];

            // Start after ethernet header
            List<byte> packetBytes = new List<Byte>(p.data.Skip(offset));

            // Mark as active
            packetList[pid].info |= PacketInfo.Active;

            for (int i = 0; i < packetBytes.Count; i++)
            {
                yield return (p.type,packetBytes[i],(uint)(packetBytes.Count-i-1));
            }

            // Mark as valid, and remove it as active
            packetList[pid].info |= PacketInfo.Valid;
            packetList[pid].info &= ~PacketInfo.Active;

            // Remove from queue
            packetPointers.Remove(pid);

            // We add what is up next, so it is valid packet pointers
            packetPointers.UnionWith(packetList[pid].requiredBy);
            Logging.log.Trace("PacketID: " + pid + " iterator ended");
            yield break;
        }

        private bool TestPacketReady(PacketInfo info)
        {
           return packetPointers.Where(x => (packetList[x].info & info) > 0 &&
                     isReady(x) ).Count() > 0;
        }

        public void Info(){
            foreach(var x in packetList.OrderBy(a => a.Key))
            {
                string depends = String.Join(",",x.Value.dependsOn);
                string required = String.Join(",",x.Value.requiredBy);
                Logging.log.Info($"PacketID: {x.Key,5} Depends: {depends,7} Required: {required,7} Flags:" + x.Value.info);
            }
            foreach(var x in packetList.OrderBy(a => a.Key))
            {
                Logging.log.Info("Is ready " + x.Key + " " + isReady(x.Key));
            }
        }
        // Dumps the current state as a graphwiz string
        public string GraphwizState(){
            string ret = "digraph G{\n";
            ret += "labelloc=\"t\";\n";
            ret += $"label=\"clock: {clock}\";\n";

            foreach(KeyValuePair<int,Packet> x in packetList.OrderBy(a => a.Key))
            {
                ret += $"   {x.Key}[";
                // Set the shape
                if((x.Value.info & PacketInfo.Send) > 0)
                {
                    ret += $"shape=triangle";
                }
                else if((x.Value.info & PacketInfo.Receive) > 0)
                {
                    ret += $"shape=invtriangle";
                }
                else if((x.Value.info & PacketInfo.DataIn) > 0)
                {
                    ret += $"shape=house";
                }
                else if((x.Value.info & PacketInfo.DataOut) > 0)
                {
                    ret += $"shape=invhouse";
                }
                // Set the colors

                // If the element is not active, but we can reach it
                if((x.Value.info & PacketInfo.Active) == 0 && isReady(x.Key))
                {
                    ret += $",style=filled,fillcolor=coral2";
                }
                // The element is active
                if((x.Value.info & PacketInfo.Active) > 0 )
                {
                    ret += $",style=filled,fillcolor=gold3";
                }
                // The element is valid
                if((x.Value.info & PacketInfo.Valid) > 0 )
                {
                    ret += $",style=filled,fillcolor=chartreuse2";
                }
                ret +="];\n";

                // Define the paths
                foreach(int y in x.Value.requiredBy){

                    ret += $"   {x.Key} -> {y};\n";
                }

            }
            ret += "}\n";
            return ret;
        }
        public void dumpStateInFile(string dir_inside_current_dir)
        {
            // Get the path and create the folder if needed
            string path = System.IO.Path.Combine(this.dir, dir_inside_current_dir);
            System.IO.Directory.CreateDirectory(path);

            string fullfilepath = System.IO.Path.Combine(path, $"{this.clock:D8}"  +  ".dot");

            Logging.log.Warn($"Adding dot graph to: {fullfilepath}");
            using (StreamWriter writer = new StreamWriter(fullfilepath, true))
            {
                writer.Write(this.GraphwizState());
            }
        }
    }
}