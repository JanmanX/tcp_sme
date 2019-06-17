#!/usr/bin/env python3
from scapy.all import rdpcap, ICMP
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
                     choices=['raw', 'icmp_echo', 'send_all'])
args = parser.parse_args()

# rdpcap comes from scapy and loads in our pcap file
packets = rdpcap(args.inputfile)
print("Doing mode:" + args.dumptype )
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

    # Sends all packets in the pcap file
    if (args.dumptype == "send_all"):

        if counter == 0:
            endstr = ""
        else:
            endstr = "_" + str(counter-1)
        filename = args.outputfolder + "/" + f"{counter}"+ endstr +"-send.bin"
        os.makedirs(os.path.dirname(filename), exist_ok=True)
        with open(filename,"wb") as f:
                f.write(data)
