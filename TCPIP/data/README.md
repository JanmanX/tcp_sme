DATA
====

TCPDUMP
=======
To dump data with nc (netcat), run the following 3 commands in separate shells:
$ sudo tcpdump -i lo  port 31337 -c 25 -w dump.bin
$ nc -l 31337
$ nc -4 localhost 31337 # notice the '4' flag to force IPv4. Use -u for UDP

This captures 25 packets from localhost on port 31337, and saves it to dump.bin as a pcap file.


Extracting raw packets using pcapToRaw.py
=========================================
pcapToRaw.py extracts the raw bytes from a Pcap file, and separates it into
individual files in a given directory.

To use pcapToRaw.py:
$ ./pcapToRaw.py <input_pcap> <output_folder>

