#!/usr/bin/env python3
from scapy.all import *
import argparse

# Get the arguments
parser = argparse.ArgumentParser("pcapToRaw")
parser.add_argument('inputfile',
                     help='pcap file to convert')
parser.add_argument('outputfolder',
                     help='folder to dump packets')
args = parser.parse_args()

# rdpcap comes from scapy and loads in our pcap file
packets = rdpcap(args.inputfile)

# Let's iterate through every packet
for counter, packet in enumerate(packets):
    data = bytes(packet)
    filename = args.outputfolder + "/" + f"{counter:05d}" +"packet.bin"
    os.makedirs(os.path.dirname(filename), exist_ok=True) 
    with open(filename,"wb") as f:
        f.write(data)