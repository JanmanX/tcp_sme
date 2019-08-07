#!/usr/bin/env python3
from scapy.all import *
import argparse, os, tqdm

# Get the arguments
parser = argparse.ArgumentParser("pcapToRaw")
parser.add_argument('inputfile',
                     help='pcap file to convert')
parser.add_argument('outputfolder',
                     help='folder to dump packets')
parser.add_argument('dumptype',
                     default='raw',
                     const='raw',
                     nargs='?',
                     help='mode to dump the packets',
                     choices=['raw', 'icmp_echo', 'send_all' , 'send_all_save_data'])
args = parser.parse_args()

# rdpcap comes from scapy and loads in our pcap file
packets = rdpcap(args.inputfile)
print("Doing mode:" + args.dumptype )
last_end_packet = 0

# Let's iterate through every packet
for counter, packet in tqdm.tqdm(enumerate(packets)):
    data = bytes(packet)

    if (args.dumptype == "raw"):
        filename = args.outputfolder + "/" + f"{counter:05d}" +"-send.bin"
        os.makedirs(os.path.dirname(filename), exist_ok=True)
        with open(filename,"wb") as f:
                f.write(data)

    # Analyses icmp_echo requests
    if (args.dumptype == "icmp_echo"):
        if (packet.haslayer(ICMP)):
            icmp = packet.getlayer(ICMP)
            if counter == 0:
                endstr = ""
            else:
                endstr = "_" + str(counter-1)

            if(icmp.type == 8):
                filename = args.outputfolder + "/" + f"{counter}"+ endstr +"-send.bin"
            if(icmp.type == 0):
                prevCounter = counter - 1
                filename = args.outputfolder + "/" + f"{counter}" + endstr + "-receive.bin"
            os.makedirs(os.path.dirname(filename), exist_ok=True)
            with open(filename,"wb") as f:
                    f.write(data)

    # Sends all packets in the pcap file give some leeway for other commands
    # By using ids in the hundreds
    if (args.dumptype == "send_all"):

        if counter == 0:
            endstr = ""
        else:
            endstr = "_" + str(counter-1) + "00"
        filename = args.outputfolder + "/" + f"{counter}00"+ endstr +"-send.bin"
        os.makedirs(os.path.dirname(filename), exist_ok=True)
        with open(filename,"wb") as f:
                f.write(data)

    if (args.dumptype == "send_all_save_data"):
        # zero extending
        z = "00000"

        # save the packet files
        if counter == 0:
            endstr = ""
        else:
            endstr = "_" + str(counter-1) + z
        filename = args.outputfolder + "/" + f"{counter}{z}"+ endstr +"-send.bin"
        os.makedirs(os.path.dirname(filename), exist_ok=True)
        with open(filename,"wb") as f:
                f.write(data)
        # save data from the packet files
        if Raw in packet:
            if last_end_packet != 0:
                filename = args.outputfolder + "/" + f"{counter}_" + str(last_end_packet) +"-datain.bin"
            else:
                filename = args.outputfolder + "/" + f"{counter}-datain.bin"

            if not packet[Raw].load.startswith(b"!"):
                last_end_packet = counter
                os.makedirs(os.path.dirname(filename), exist_ok=True)
                with open(filename,"wb") as f:
                    f.write(packet[Raw].load)
