using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SME;
using SME.Components;

namespace TCPIP
{
    ///////////////////// BLOCK INFORMATION
    // Information about the system

    public class SystemGraph  {
        struct SystemInformation
        {
            public string Name;
            public (double x,double y) Position;
            public (double h,double w) Dimension;
            public SystemType SystemType;

            public SystemInformation(string name, (double x, double y) position, (double h, double w) dimension, SystemType systemType)
            {
                Name = name;
                Position = position;
                Dimension = dimension;
                SystemType = systemType;
            }
        }
        public enum SystemType{
            MEMORY,
            PRODUCER,
            SIMULATOR
        }
        public enum SystemName{
            FRAME_OUT,
            INTERNET_IN,
            INTERNET_OUT,
            SEGMENT_IN,
            SEGMENT_OUT,
            TRANSPORT,
            DATA_IN,
            DATA_OUT,
            LINK_INTERFACE,
            INTERFACE
        }
        public enum ConnectionType {
            CONTROL_BUFFER_PRODUCER,
            CONTROL_COMPUTE_PRODUCER,
            CONTROL_CONSUMER,
            DATA
        }


        private Dictionary<SystemName,Process> systemNameProcess = new Dictionary<SystemName,Process>();
        private Dictionary<SystemName,Dictionary<SystemName,Dictionary<ConnectionType,IBus>>> systemPaths =
            new Dictionary<SystemName,Dictionary<SystemName,Dictionary<ConnectionType,IBus>>>();

        private Dictionary<SystemName,SystemInformation> systemInfo = new Dictionary<SystemName, SystemInformation>();
        private Dictionary<ConnectionType,(double x,double y)> pathOffset = new Dictionary<ConnectionType, (double x, double y)>();
        private bool debug = false;
        private int tempCount = 0;
        public SystemGraph(bool debug = false)
        {
            this.debug = debug;
            // Fill positional arguments for each block, making it easier to pretty print
            double s = 1; // The spread factor
            (double,double) cylDim = (1,2);
            (double,double) proDimSmall = (0.7,2);
            (double,double) proDimBig = (0.7,7);
            systemInfo.Add(SystemName.LINK_INTERFACE, new SystemInformation("Link Interface",(s*0,2*s),proDimBig,SystemType.SIMULATOR));
            systemInfo.Add(SystemName.INTERNET_IN, new SystemInformation("Internet In",(s*-2,-2*s),proDimSmall,SystemType.PRODUCER));
            systemInfo.Add(SystemName.SEGMENT_IN, new SystemInformation("Segment In",(s*-2,-4*s),cylDim,SystemType.MEMORY));
            systemInfo.Add(SystemName.TRANSPORT, new SystemInformation("Transport",(s*0,-6*s),proDimBig,SystemType.PRODUCER));
            systemInfo.Add(SystemName.DATA_OUT, new SystemInformation("Data Out",(s*-2,-8*s),cylDim,SystemType.MEMORY));
            systemInfo.Add(SystemName.INTERFACE, new SystemInformation("Interface",(s*0,-10*s),proDimBig,SystemType.SIMULATOR));
            systemInfo.Add(SystemName.DATA_IN, new SystemInformation("Data In",(s*2,-8*s),cylDim,SystemType.MEMORY));
            systemInfo.Add(SystemName.SEGMENT_OUT, new SystemInformation("Segment Out",(s*2,-4*s),cylDim,SystemType.MEMORY));
            systemInfo.Add(SystemName.INTERNET_OUT, new SystemInformation("Internet Out",(s*2,-2*s),proDimSmall,SystemType.PRODUCER));
            systemInfo.Add(SystemName.FRAME_OUT, new SystemInformation("Frame Out",(s*2,0*s),cylDim,SystemType.MEMORY));
            // Define path offsets for different paths
            pathOffset.Add(ConnectionType.DATA,(0.5,0));
            pathOffset.Add(ConnectionType.CONTROL_CONSUMER,(-0.5,0));
            pathOffset.Add(ConnectionType.CONTROL_BUFFER_PRODUCER,(0,0));
            pathOffset.Add(ConnectionType.CONTROL_COMPUTE_PRODUCER,(0,0));

        }
        public void AddSystem(SystemName block_info,Process process)
        {
            Logging.log.Warn($"Adding system: {block_info}");
            systemNameProcess.Add(block_info,process);
        }

        public void AddConnection(SystemName sender, SystemName receiver, ConnectionType type, IBus pipe)
        {
            var x = pipe.GetType().GetProperties();
            Logging.log.Trace($"Connection: {sender,15} -> {receiver,15} type:{type,30} pipe:{pipe}");
            foreach (var y in x)
            {
                Logging.log.Trace($"    Entries: {y}");
            }

            if(!systemPaths.ContainsKey(sender))
            {
                var a = new Dictionary<ConnectionType,IBus>{{type,pipe}};
                var b = new Dictionary<SystemName, Dictionary<ConnectionType,IBus>>{{receiver,a}};
                systemPaths.Add(sender,b);
            }
            if(!systemPaths[sender].ContainsKey(receiver)){
                systemPaths[sender].Add(receiver,new Dictionary<ConnectionType,IBus>{{type,pipe}});
            }
            if(!systemPaths[sender][receiver].ContainsKey(type))
            {
                systemPaths[sender][receiver].Add(type,pipe);
            }

        }
        public string GraphwizState()
        {
            tempCount = 0;
            string ret = "digraph G{\n";
            ret += "splines=ortho;\n" +
                   "graph [fontname = Courier];\n" +
                   "node [fontname = Courier,style=filled,fontsize=20];\n" +
                   "edge [fontname = Courier,arrowsize=2];\n";

            // Insert Definitions of the nodes
            foreach(var sys in systemNameProcess)
            {
                var info = systemInfo[sys.Key];
                // The label
                ret += sys.Key.ToString() + $"[fixedsize = true,";
                // The type
                switch (info.SystemType)
                {
                    case SystemType.SIMULATOR:
                        ret += $"shape=rect,label=\"{info.Name}\\n";
                        break;
                    case SystemType.MEMORY:
                        ret += $"shape=cylinder,label=\"{info.Name}\\n";
                        break;
                    case SystemType.PRODUCER:
                        ret += $"shape=rect,label=\"{info.Name}\\n";
                        break;
                    default:
                        Logging.log.Error("Should not be possible!");
                        break;
                }
                // Additional information
                switch (sys.Key)
                {
                    case SystemName.TRANSPORT:
                        Transport trans = (Transport)sys.Value;
                        ret += $"{trans.state}\",";
                        break;
                    // The memory cases
                     case SystemName.DATA_IN:
                        DataIn buf_data_in = (DataIn)sys.Value;
                        var buf_data_in_write = buf_data_in.dataInComputeProducerControlBusIn.valid ? "Write": "";
                        var buf_data_in_read = buf_data_in.dataOutBufferConsumerControlBusIn.ready ? "Read": "";
                        ret += $"{buf_data_in_write,5}|{buf_data_in_read,4}\",";
                        break;
                    case SystemName.DATA_OUT:
                        DataOut buf_data_out = (DataOut)sys.Value;
                        var buf_data_out_write = buf_data_out.dataInComputeProducerControlBusIn.valid ? "Write": "";
                        var buf_data_out_read = buf_data_out.dataOutBufferConsumerControlBusIn.ready ? "Read": "";
                        ret += $"{buf_data_out_write,5}|{buf_data_out_read,5}\",";
                        break;
                    case SystemName.SEGMENT_IN:
                        PacketIn buf_segment_in = (PacketIn)sys.Value;
                        var buf_segment_in_write = buf_segment_in.packetInComputeProducerControlBusIn.valid ? "Write": "";
                        var buf_segment_in_read = buf_segment_in.packetOutBufferConsumerControlBusIn.ready ? "Read": "";
                        ret += $"{buf_segment_in_write,5}|{buf_segment_in_read,5}\",";
                        break;
                    case SystemName.SEGMENT_OUT:
                        PacketOut buf_segment_out = (PacketOut)sys.Value;
                        var buf_segment_out_write = buf_segment_out.packetInComputeProducerControlBusIn.valid ? "Write": "";
                        var buf_segment_out_read = buf_segment_out.packetOutBufferConsumerControlBusIn.ready ? "Read": "";
                        ret += $"{buf_segment_out_write,5}|{buf_segment_out_read,5}\",";
                        break;

                    case SystemName.FRAME_OUT:
                        FrameOut buf_frame_out = (FrameOut)sys.Value;
                        var buf_frame_out_write = buf_frame_out.packetInComputeProducerControlBusIn.valid ? "Write": "";
                        var buf_frame_out_read = buf_frame_out.datagramBusOutBufferConsumerControlBusIn.ready ? "Read": "";
                        ret += $"{buf_frame_out_write,5}|{buf_frame_out_read,5}\",";
                        break;
                    default:
                        ret += $" \",";
                        break;
                }
                // Get width if set
                if(info.Dimension.w > -1)
                {
                    ret += $"width = {info.Dimension.w.ToString(CultureInfo.InvariantCulture)},";
                }
                if(info.Dimension.h > -1)
                {
                    ret += $"height = {info.Dimension.h.ToString(CultureInfo.InvariantCulture)},";
                }
                // The position
                ret += $"pos=\"{info.Position.x.ToString(CultureInfo.InvariantCulture)},{info.Position.y.ToString(CultureInfo.InvariantCulture)}!\"";
                ret += "];\n";

            }

            //Insert the vertices
            // foreach(var x in pathOrder.OrderBy(p => p.Key)){
            //     Logging.log.Warn($"{x}");
            //     var senderValue = systemPaths[x.Value.sender];
            //     var senderKey = x.Value.sender;
            //     var receiverValue = systemPaths[x.Value.sender][x.Value.receiver];
            //     var receiverKey = x.Value.receiver;
            //     var typeValue = systemPaths[x.Value.sender][x.Value.receiver][x.Value.type];
            //     var typeKey = x.Value.type;
            // }
            foreach(var sender in systemPaths)
            {
                foreach(var receiver in sender.Value.OrderBy(p => p.Key))
                {
                    foreach(var values in receiver.Value.OrderBy(p => p.Key))
                    {
                        var senderInfo = systemInfo[sender.Key];
                        var receiverInfo = systemInfo[receiver.Key];
                        // Connection type specific data
                        string sameTag = "";
                        switch (values.Key)
                        {
                            case ConnectionType.CONTROL_BUFFER_PRODUCER:
                                BufferProducerControlBus bcb = (BufferProducerControlBus)values.Value;
                                sameTag += $"style=bold,color=cyan1";
                                break;
                            case ConnectionType.CONTROL_COMPUTE_PRODUCER:
                                ComputeProducerControlBus pcb = (ComputeProducerControlBus)values.Value;
                                sameTag += $"style=bold,color=green";
                                break;
                            case ConnectionType.CONTROL_CONSUMER:
                                ConsumerControlBus ccb = (ConsumerControlBus)values.Value;
                                sameTag += $"style=bold,color=orange";
                                break;
                            case ConnectionType.DATA:
                                sameTag += "style=bold";
                                break;
                            default:
                                Logging.log.Error("Should not be possible!");
                                break;
                        }
                        string tempNode =$"Node{tempCount++}";
                        // Calculate the pos
                        // If the block is wide, we do not want to use that as the x axis target
                        double xval = 0;
                        if (senderInfo.Dimension.w > 2)
                        {
                            xval = receiverInfo.Position.x + pathOffset[values.Key].x;
                        }else
                        {
                            xval = senderInfo.Position.x + pathOffset[values.Key].x;
                        }

                        var yval = senderInfo.Position.y + ((receiverInfo.Position.y - senderInfo.Position.y) / 2) + pathOffset[values.Key].y;

                        ret += $"{tempNode}[style=invis,fixedsize=true,width=0,height=0,pos=\"{xval.ToString(CultureInfo.InvariantCulture)},{yval.ToString(CultureInfo.InvariantCulture)}!\"];\n";
                        ret += $"{sender.Key} -> {tempNode}[{sameTag},arrowhead=none];\n";
                        sameTag += ",labeldistance=5,labelfontsize=20,labelangle=0";
                        switch (values.Key)
                        {
                            case ConnectionType.CONTROL_BUFFER_PRODUCER:
                                BufferProducerControlBus bcb = (BufferProducerControlBus)values.Value;
                                sameTag += $",{Tick(bcb.valid)}\\n{bcb.bytes_left}\"";
                                break;
                            case ConnectionType.CONTROL_COMPUTE_PRODUCER:
                                ComputeProducerControlBus pcb = (ComputeProducerControlBus)values.Value;
                                sameTag += $",{Tick(pcb.valid)}\\n{pcb.bytes_left}\"";
                                break;
                            case ConnectionType.CONTROL_CONSUMER:
                                ConsumerControlBus ccb = (ConsumerControlBus)values.Value;
                                sameTag += $",{Tick(ccb.ready)}\"";
                                break;
                            case ConnectionType.DATA:
                                switch (sender.Key)
                                {
                                    case SystemName.TRANSPORT:
                                        break;
                                    // The memory cases
                                    case SystemName.DATA_IN:
                                        break;
                                    case SystemName.DATA_OUT:
                                        break;
                                    case SystemName.SEGMENT_IN:
                                        break;
                                    case SystemName.SEGMENT_OUT:
                                        break;

                                    case SystemName.FRAME_OUT:
                                        break;
                                    default:
                                        //ret += $",";
                                        break;
                                }
                                break;
                            default:
                                Logging.log.Error("Should not be possible!");
                                break;
                        }
                        ret += $"{tempNode} -> {receiver.Key}[{sameTag}];\n";


                    }
                }
            }

            ret += "}\n";
            return ret;
        }


        private string Tick(bool value){
            return value ? "fontcolor=green,headlabel=\"✓" : "fontcolor=red,headlabel=\"✕";
        }


        public void Test()
        {

        }
    }
}

// Logging.log.Error($"Connection: {sender.Key,15} -> {receiver.Key,15} type:{values.Key,30} pipe:{values.Value}");
// foreach (var y in values.Value.GetType().GetProperties())
// {
//     Logging.log.Error($"    Entries: {y}");

//     if(y.PropertyType == typeof(byte)){
//         byte g = (byte)values.Value.GetType().GetProperty(y.Name).GetValue(values.Value);
//         Logging.log.Error($"XXXXXXXXXXXXXXXXXXXXXXxx {g}");

//     }
//     var z = y.GetValue(values.Value);
// }
//ret += "];\n";