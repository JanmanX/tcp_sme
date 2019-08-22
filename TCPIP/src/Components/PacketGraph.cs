using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            public int id; // Ihe identifier
            public PacketInfo info;
            public string dataPath;
            public byte[] data;
            public ushort ether_type;
            public string additional_data; // Contains the additional data from the filename
            public int last_clock_active; // The last clockcycle the packet was "active"
            public SortedSet<int> dependsOn;
            public SortedSet<int> requiredBy;
        }

        private Dictionary<int,Packet> packetList = new Dictionary<int,Packet>();
        private SortedSet<int> initPackets = new SortedSet<int>();
        private SortedSet<int> exitPackets = new SortedSet<int>();
        private SortedSet<int> packetPointers; // List of packet pointers waiting
        private List<List<int>> clusterPackets = new List<List<int>>(); // List of best custer settings for the graphviz dump

        private int clock = -1;
        private string dir;

        private bool debug = false;

        public PacketGraph(string dir, bool debug){
            string[] filePaths = Directory.GetFiles(dir);
            var simPackets = from c in filePaths
                             select GenerateSimPacket(c);
            this.dir = dir;
            this.debug = debug;
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
                Logging.log.Warn("The graph may have cases where the the current receiving node is dependent of ID, beware!");
            }

            // Calculate the clusters
            CalculateClusters();
        }

        private Packet GenerateSimPacket(string filePath){
            Logging.log.Trace("Parsing " + filePath);
            var fileName =  Path.GetFileName(filePath);
            var reg = new Regex(@"^(\d*)_?(.*)\-(\w*)(?:\.(.*)|)\.bin$");
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
            // Get the additional data
            string additional_data = match.Groups[4].Value;

            // Create the packet
            Packet pack = new Packet();
            pack.id = id;
            pack.info = info;
            pack.additional_data = additional_data;
            // if this packet can be sent or received, load the data
            if((pack.info & (PacketInfo.Send |
                             PacketInfo.Receive |
                             PacketInfo.DataIn |
                             PacketInfo.DataOut |
                             PacketInfo.Wait)) > 0)
            {
                pack.data = File.ReadAllBytes(filePath);
            }
            if((pack.info & (PacketInfo.Send |
                             PacketInfo.Receive)) > 0)
            {
                // Set the packet type based on the byte value
                pack.ether_type = (ushort)(pack.data[EthernetIIFrame.ETHERTYPE_OFFSET_0] << 0x08);
                pack.ether_type |= (ushort)(pack.data[EthernetIIFrame.ETHERTYPE_OFFSET_1]);
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
        private (ushort type,byte data,uint bytes_left,Packet packet) lastSend;
        public IEnumerable<(ushort type,byte data,uint bytes_left,Packet packet)> IterateOverSend(){
            var send = IterateOver(PacketInfo.Send,(int)EthernetIIFrame.HEADER_SIZE).GetEnumerator();
            while(send.MoveNext())
            {
                lastSend = send.Current;
                Logging.log.Trace($"Current iterator{send.Current} data: 0x{send.Current.data:X2}");
                yield return send.Current;
            }
            yield break;
        }

        private (ushort type,byte data,uint bytes_left,Packet packet) lastDataOut;
        public IEnumerable<(ushort type,byte data,uint bytes_left,Packet packet)> IterateOverDataOut(){
            var dataOut = IterateOver(PacketInfo.DataOut,0).GetEnumerator();
            while(dataOut.MoveNext())
            {
                lastDataOut = dataOut.Current;
                yield return dataOut.Current;
            }
            yield break;
        }

        private IEnumerator<(ushort type,byte data,uint bytes_left,Packet packet)> receive;
        private (ushort type,byte data,uint bytes_left,Packet packet) lastReceive;
        public bool GatherReceive(byte compare, int byte_number)
        {
            if (receive == null){
                receive = IterateOver(PacketInfo.Receive,0).GetEnumerator();
            }

            if(receive.MoveNext()){
                byte excact = receive.Current.data;
                lastReceive = receive.Current;
                if(excact == compare && byte_number == receive.Current.bytes_left)
                {
                    Logging.log.Info($"Good comparison of input data from receive. " +
                                     $"correct: 0x{excact:X2} index: {receive.Current.bytes_left} " +
                                     $"observed: 0x{compare:X2} index: {byte_number}");
                    if(byte_number == 0)
                    {
                        receive.MoveNext();
                    }
                    return true;
                }
                else
                {
                    // Logging.log.Error($"Wrong comparison of input data from receive. " +
                    //                   $"correct: 0x{excact:X2} index: {receive.Current.bytes_left} " +
                    //                   $"observed: 0x{compare:X2} index: {byte_number}");
                    return false;
                }

            }else
            {
                if(ReadyReceive()){
                    receive.MoveNext();
                    receive = null;
                    return GatherReceive(compare, byte_number);
                }
            }
            Logging.log.Error($"No packet found for GatherReceive. compared to: 0x{compare:X2}");
            return false;
        }
        public (ushort type,byte data,uint bytes_left,Packet packet) PeekReceive()
        {
            return lastReceive;
        }


        private IEnumerator<(ushort type,byte data,uint bytes_left,Packet packet)> dataIn;
        private (ushort type,byte data,uint bytes_left,Packet packet) lastDataIn;
        public bool GatherDataIn(byte compare, int byte_number, int socket)
        {
            if (dataIn == null){
                dataIn = IterateOver(PacketInfo.DataIn,0).GetEnumerator();
            }

            if(dataIn.MoveNext()){
                byte excact = dataIn.Current.data;
                lastDataIn = dataIn.Current;
                if(excact == compare &&
                   byte_number == dataIn.Current.bytes_left &&
                   socket.ToString() == dataIn.Current.packet.additional_data)
                {
                    Logging.log.Info($"Good comparison of input data from datain. " +
                                     $"data correct: 0x{excact:X2} index: {dataIn.Current.bytes_left} " +
                                     $"data observed: 0x{compare:X2} index: {byte_number} " +
                                     $"socket correct: 0x{dataIn.Current.packet.additional_data} " +
                                     $"socket observed: 0x{socket} ");
                    if(byte_number == 0)
                    {
                        dataIn.MoveNext();
                    }
                    return true;
                }
                else
                {
                    Logging.log.Fatal($"Wrong comparison of input data from datain. " +
                                      $"data correct: 0x{excact:X2} index: {dataIn.Current.bytes_left} " +
                                      $"data observed: 0x{compare:X2} index: {byte_number} "+
                                     $"socket correct: 0x{dataIn.Current.packet.additional_data} " +
                                     $"socket observed: 0x{socket} " );
                    return false;
                }

            }else
            {
                if(ReadyDataIn()){
                    dataIn.MoveNext();
                    dataIn = null;
                    return GatherDataIn(compare, byte_number,socket);
                }
            }
            Logging.log.Warn($"No packet found for GatherDataIn. compared to: 0x{compare:X2}");
            return false;
        }
        public (ushort type,byte data,uint bytes_left,Packet packet) PeekDataIn()
        {
            return lastDataIn;
        }

        public bool StepWait()
        {
            var toWait = packetPointers.Where(
                x => (packetList[x].info & PacketInfo.Wait) > 0 &&
                     isReady(x)
            ).ToList();

            // If there are no elements in the packets to wait
            if(toWait.Count == 0){
                return false;
            }

            foreach(var pid in toWait)
            {
                // Mark as active,  and update clock
                packetList[pid].info |= PacketInfo.Active;
                packetList[pid].last_clock_active = clock;
                // Get the amount of wait time, and decrement and write back
                int waitfor = Int32.Parse(Encoding.UTF8.GetString(packetList[pid].data, 0, packetList[pid].data.Length));


                if(waitfor == 0)
                {
                    // Mark as not active anymore
                    packetList[pid].info |= PacketInfo.Valid;
                    packetList[pid].info &= ~PacketInfo.Active;
                    // Remove from queue
                    packetPointers.Remove(pid);

                    // We add what is up next, so it is valid packet pointers
                    packetPointers.UnionWith(packetList[pid].requiredBy);
                }else{
                    waitfor--;
                }
                packetList[pid].data = Encoding.UTF8.GetBytes(waitfor.ToString());

                //Logging.log.Trace($"PacketID: {pid} waiting for {waitfor} info: {packetList[pid].info}");
            }
            return true;
        }

        public bool ReadySend(){return TestPacketContains(PacketInfo.Send);}
        public bool ReadyReceive(){return TestPacketContains(PacketInfo.Receive);}
        public bool ReadyDataIn(){return TestPacketContains(PacketInfo.DataIn);}
        public bool ReadyDataOut(){return TestPacketContains(PacketInfo.DataOut);}
        public bool ReadyWait(){return TestPacketContains(PacketInfo.Wait);}
        public bool ReadyCommand(){return TestPacketContains(PacketInfo.Command);}

        public void NextClock()
        {
            clock++;
        }

        public int GetClock()
        {
            return clock;
        }

        public bool Finished()
        {
            return packetList.Where(a => (a.Value.info & PacketInfo.Valid) > 0).Count() == packetList.ToList().Count;
        }


        ///////////////////////////////////////////////////
        // Helper Functions

        private IEnumerable<(ushort type,byte data,uint bytes_left,Packet packet)> IterateOver(PacketInfo info, int offset){
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
            Logging.log.Warn("PacketID: " + pid + " iterator started");
            Packet pack = packetList[pid];

            // Start after ethernet header
            List<byte> packetBytes = new List<Byte>(pack.data.Skip(offset));

            // Mark as active
            packetList[pid].info |= PacketInfo.Active;

            for (int i = 0; i < packetBytes.Count; i++)
            {
                yield return (pack.ether_type,packetBytes[i],(uint)(packetBytes.Count-i-1),pack);
                packetList[pid].last_clock_active = clock;
            }

            // Mark as not active anymore
            packetList[pid].info |= PacketInfo.Valid;
            packetList[pid].info &= ~PacketInfo.Active;

            // Remove from queue
            packetPointers.Remove(pid);

            // We add what is up next, so it is valid packet pointers
            packetPointers.UnionWith(packetList[pid].requiredBy);
            Logging.log.Trace("PacketID: " + pid + " iterator ended");
        }

        private bool TestPacketContains(PacketInfo info)
        {
           return packetPointers.Where(x => (packetList[x].info & info) > 0 &&
                     isReady(x) ).Count() > 0;
        }

        private bool TestPacketExact(PacketInfo info)
        {
           return packetPointers.Where(x => (packetList[x].info & info) == info &&
                     isReady(x) ).Count() > 0;
        }

        // Calculate which nodes to cluster together in case of long 1 to 1 paths
        private void CalculateClusters(){
            List<List<int>> clusterList = new List<List<int>>();
            // Get a list of the keys to traverse
            SortedSet<int> keysToTraverse = new SortedSet<int>(this.packetList.Keys);

            // Iterate over all keys, and put in their respective bucket
            foreach (int key in this.packetList.Keys)
            {
                var pack = this.packetList[key];
                // Test if there is at most one out and one in, and it is not already traverse
                if(pack.dependsOn.Count <= 1 && pack.requiredBy.Count <= 1 && keysToTraverse.Contains(key))
                {
                    List<int> observed = new List<int>();
                    // Search down
                    var downpack = pack;
                    while(downpack.dependsOn.Count <= 1 && downpack.requiredBy.Count <= 1 && keysToTraverse.Contains(downpack.id)){
                        if(downpack.id != pack.id)
                        {
                            observed.Reverse();
                            observed.Add(downpack.id);
                            observed.Reverse();
                        }
                        if(downpack.requiredBy.Count == 0){
                            break;
                        }
                        downpack = packetList[downpack.requiredBy.First()];
                    }
                    // Search up
                    var uppack = pack;
                    while(uppack.dependsOn.Count <= 1 && uppack.requiredBy.Count <= 1 && keysToTraverse.Contains(uppack.id)){
                        if(downpack.id != pack.id)
                        {
                            observed.Add(uppack.id);
                        }
                        if(uppack.dependsOn.Count == 0){
                            break;
                        }
                        uppack = packetList[uppack.dependsOn.First()];
                    }
                    // Remove observe keys from list, and add to the cluster list
                    keysToTraverse.RemoveWhere((int x) => { return observed.Contains(x);});
                    observed.Reverse();
                    clusterList.Add(observed);
                }
            }
            foreach (var cluster in clusterList)
            {
                List<int> group = new List<int>();
                int delimiter = (int)Math.Ceiling(Math.Sqrt(cluster.Count));
                for (int i = 0; i < cluster.Count; i++)
                {
                    if(i%delimiter == 0){
                        group.Add(cluster[i]);
                    }
                }
                clusterPackets.Add(group);
            }
        }


        //////////////////////////// Debug information ////////////////////////////////
        public void Info(){
            foreach(var x in packetList.OrderBy(a => a.Key))
            {
                string depends = String.Join(",",x.Value.dependsOn);
                string required = String.Join(",",x.Value.requiredBy);
                Logging.log.Warn($"PacketID: {x.Key} " +
                                 $"Ready: {isReady(x.Key)} " +
                                 $"Depends: {depends} " +
                                 $"Required: {required} " +
                                 $"Extra: {x.Value.additional_data} " +
                                 $"Flags: " + x.Value.info);
            }
            foreach (int i in Enum.GetValues(typeof(PacketInfo)))
            {
                PacketInfo t = (PacketInfo)i;
                int c = packetList.Where(a => (a.Value.info & t) > 0).Count();
                Logging.log.Warn($"Type: {t,10} count: {c}");
            }
        }
        // Dumps the current state as a graphwiz string
        public string GraphwizState(){
            string ret = "digraph G{\n";

            ret += "    graph [fontname = \"Courier\"];\n";
            ret += "    node [fontname = \"Courier\",fixedsize = true,width = 1,height = 1];\n";
            ret += "    edge [fontname = \"Courier\"];\n";
            ret += "    labelloc=\"t\";\n";
            ret += "    fontsize=35;\n";
            ret +=$"    label=\"clock: {clock}\";\n";

            foreach(KeyValuePair<int,Packet> x in packetList.OrderBy(a => a.Key))
            {
                ret += $"    {x.Key}[label=\"{x.Key}";
                // Set the data
                // If the block is sending
                if((x.Value.info & PacketInfo.Send) > 0)
                {
                    if((x.Value.info & (PacketInfo.Active)) > 0)
                    {
                        string tmp = $"{lastSend.bytes_left} 0x{lastSend.data:X2}";
                        ret += $"\\n{tmp,7}";
                    }
                    else if((x.Value.info & (PacketInfo.Valid)) > 0)
                    {
                        ret += $"\\nDone";
                    }
                    else{
                        ret += $"\\nWaiting";
                    }
                }
                if((x.Value.info & PacketInfo.Receive) > 0)
                {
                    if((x.Value.info & (PacketInfo.Active)) > 0)
                    {
                        string tmp = $"{lastReceive.bytes_left} 0x{lastReceive.data:X2}";
                        ret += $"\\n{tmp,7}";
                    }
                    else if((x.Value.info & (PacketInfo.Valid)) > 0)
                    {
                        ret += $"\\nDone";
                    }
                    else{
                        ret += $"\\nWaiting";
                    }
                }
                if((x.Value.info & PacketInfo.Wait) > 0)
                {
                    string waitstr = Encoding.UTF8.GetString(x.Value.data, 0, x.Value.data.Length);

                    if((x.Value.info & (PacketInfo.Valid)) > 0)
                    {
                        ret += $"\\nDone";
                    }
                    else{
                        ret += $"\\nval:{waitstr,3}";
                    }
                }
                if((x.Value.info & PacketInfo.DataIn) > 0)
                {
                    if((x.Value.info & (PacketInfo.Active)) > 0)
                    {
                        string tmp = $"{lastDataIn.bytes_left} 0x{lastDataIn.data:X2}";
                        ret += $"\\n{tmp,7}";
                    }
                    else if((x.Value.info & (PacketInfo.Valid)) > 0)
                    {
                        ret += $"\\nDone";
                    }
                    else{
                        ret += $"\\nWaiting";
                    }
                }
                if((x.Value.info & PacketInfo.DataOut) > 0)
                {
                    if((x.Value.info & (PacketInfo.Active)) > 0)
                    {
                        string tmp = $"{lastDataOut.bytes_left} 0x{lastDataOut.data:X2}";
                        ret += $"\\n{tmp,7}";
                    }
                    else if((x.Value.info & (PacketInfo.Valid)) > 0)
                    {
                        ret += $"\\nDone";
                    }
                    else{
                        ret += $"\\nWaiting";
                    }
                }

                ret += "\",";



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
                else if((x.Value.info & PacketInfo.Wait) > 0)
                {
                    ret += $"shape=octagon";
                }
                // Set the colors

                // If the element is not active, but we can reach it
                if((x.Value.info & PacketInfo.Active) == 0 && isReady(x.Key))
                {
                    ret += $",style=filled,fillcolor=cyan";
                }
                // The element is active
                if((x.Value.info & PacketInfo.Active) > 0 )
                {
                    // If the element has not been used since last clock
                    if(x.Value.last_clock_active == clock){
                        ret += $",style=filled,fillcolor=gold3";
                    }else
                    {
                        ret += $",style=filled,fillcolor=coral2";
                    }

                }
                // The element is valid
                if((x.Value.info & PacketInfo.Valid) > 0 )
                {
                    ret += $",style=filled,fillcolor=chartreuse2";
                }
                ret +="];\n";

                // Define the paths
                foreach(int y in x.Value.requiredBy){
                    ret += $"    {x.Key} -> {y};\n";
                }

            }
            // add the clusters
            foreach (var cluster in clusterPackets)
            {
                ret += "    {rank = same;";
                foreach (var element in cluster)
                {
                    ret += $" {element};";
                }
                ret += "}\n";
            }

            ret += "}\n";
            return ret;
        }
        public void DumpStateInFile(string dir_inside_current_dir)
        {
            // Get the path and create the folder if needed
            string path = System.IO.Path.Combine(this.dir, dir_inside_current_dir);
            System.IO.Directory.CreateDirectory(path);

            string fullfilepath = System.IO.Path.Combine(path, $"{this.clock:D8}"  +  ".dot");

            //Logging.log.Trace($"Adding dot graph to: {fullfilepath}");
            using (StreamWriter writer = new StreamWriter(fullfilepath, true))
            {
                writer.Write(this.GraphwizState());
            }
        }
    }
}