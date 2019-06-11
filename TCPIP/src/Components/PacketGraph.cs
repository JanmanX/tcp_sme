using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TCPIP
{
    public class PacketGraph  {
        public enum PacketType{
            ICMP
        }
        [Flags]
        public enum PacketInfo{
            ToSend = 1 << 0, // this packet has to be sent.
            ToReceive= 1 << 1, // This packet has to be received.
            Initial = 1 << 2, // Is this an beginning packet?
            End = 1 << 3, // Is this an end packet?
            Valid = 1 << 4, // Has this packet been sent or received?
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
        private const int bufferSize = 100000;
        private byte[] receiveBuffer = new byte[bufferSize];
        private int receiveBufferSize = 0;


        public PacketGraph(string dir){
            string[] filePaths = Directory.GetFiles(dir);
            var simPackets = from c in filePaths
                             select GenerateSimPacket(c);

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
                string depends = String.Join(",",x.Value.dependsOn);
                string required = String.Join(",",x.Value.requiredBy);
                Logging.log.Info($"PacketID: {x.Key,5} Depends: {depends,7} Required: {required,7} Flags:" + x.Value.info);
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
                    info |= PacketInfo.ToSend;
                    break;
                case "receive":
                    info |= PacketInfo.ToReceive;
                    break;
                default:
                    Logging.log.Fatal("Packet metainfo not detected: " + fileName);
                    break;
            }
            // Create the packet
            Packet pack = new Packet();
            pack.id = id;
            pack.info = info;
            pack.data = File.ReadAllBytes(filePath);
            // Set the packet type based on the byte value
            pack.type = (ushort)(pack.data[EthernetIIFrame.ETHERTYPE_OFFSET_0] << 0x08);
            pack.type |= (ushort)(pack.data[EthernetIIFrame.ETHERTYPE_OFFSET_1]);
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


        public IEnumerable<(ushort type,byte b)> IterateOverPacketToSend(){
            // Get all packets that we have to send currently, and test if they are ready
            var toSend = packetPointers.Where(
                x => (packetList[x].info & PacketInfo.ToSend) > 0 &&
                     isReady(x)
            ).ToList();

            int pid = packetPointers.First();
            Logging.log.Trace("PacketID: " + pid + " iterator started");
            Packet p = packetList[pid];

            // Start after ethernet header
            foreach(byte b in p.data.Skip((int)EthernetIIFrame.HEADER_SIZE)){
                //Logging.log.Trace($"Yielding {(p.type,b)}");
                yield return (p.type,b);
            }

            // Mark as valid
            packetList[pid].info |= PacketInfo.Valid;

            // Remove from queue
            packetPointers.Remove(pid);

            // We add what is up next, so it is valid packet pointers
            packetPointers.UnionWith(packetList[pid].requiredBy);
            Logging.log.Trace("PacketID: " + pid + " iterator ended");
            yield break;
        }

        public bool HasPackagesToSend(){
            bool ret = packetPointers.Where(x => (packetList[x].info & PacketInfo.ToSend) > 0 ).Count() > 0;
            return ret;
        }

        private void Debug(){
            foreach(var x in packetList.OrderBy(a => a.Key))
            {
                Logging.log.Warn("Is ready " + x.Key + " " + isReady(x.Key));
            }
        }
    }
}