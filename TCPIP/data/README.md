DATA
====

TCPDUMP
=======
To dump data with nc (netcat), run the following 3 commands in separate shells:
$ sudo tcpdump -i lo  port 31337 -c 25 -w dump.bin
$ nc -l 31337
$ nc -4 localhost 31337 # notice the '4' flag to force IPv4. Use -u for UDP

This captures 25 packets from localhost on port 31337, and saves it to dump.bin as a pcap file.

Now convert the pcap file to multiple files containing the raw packet data. Use pcapToRaw.py
