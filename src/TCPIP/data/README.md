DATA
====

TCPDUMP
=======
To dump data with nc (netcat), run the following 3 commands in separate shells:
$ sudo tcpdump -i lo  port 31337 -c 25 -w dump.bin
$ nc -l 31337
$ nc localhost 31337

This captures 25 packets from localhost on port 31337, and saves it to dump.bin.
