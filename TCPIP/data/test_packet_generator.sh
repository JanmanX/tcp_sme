#!/bin/bash

# tcpdump -i lo port $CORRECT_PORT or port $INVALID_PORT -w dump.bin -U & PIDTCPDUMP=$!

# CONFIG
NUM_PACKETS=32

SOURCE_PORT=6666
CONNECTION1_PORT=6543
CONNECTION2_PORT=6789
INVALID_PORT=3456


# Start listeners
# UDP
nc -u -l $CONNECTION1_PORT & PIDNC1=$!
nc -u -l $CONNECTION2_PORT & PIDNC2=$!
nc -u -l $INVALID_PORT & PIDNC3=$!

# TCP
nc -k -l $CONNECTION1_PORT & PIDNC4=$!
nc -k -l $CONNECTION2_PORT & PIDNC5=$!
nc -k -l $INVALID_PORT & PIDNC6=$!


# send data
for i in {1..64}
do
	echo "Iteration $i"

	# IPv4/UDP
	echo $(python3 -c 'import string;print(string.ascii_uppercase*1000)') | nc -4 -u -p $SOURCE_PORT localhost $CONNECTION1_PORT & PIDTMP1=$!
	echo $(python3 -c 'print("!"*26*1000)') | nc -4 -u -p $SOURCE_PORT localhost $INVALID_PORT & PIDTMP2=$!
	echo $(python3 -c 'print("0123456789"*1000)') | nc -4 -u -p $SOURCE_PORT localhost $CONNECTION2_PORT & PIDTMP3=$!

	# IPv4/TCP
	echo $(python3 -c 'print("!"*26*1000)') | nc -4 -p $SOURCE_PORT localhost $CONNECTION1_PORT& PIDTMP4=$!
	echo $(python3 -c 'print("!"*26*1000)') | nc -4 -p $SOURCE_PORT localhost $INVALID_PORT & PIDTMP5=$!
	echo $(python3 -c 'print("!"*26*1000)') | nc -4 -p $SOURCE_PORT localhost $CONNECTION2_PORT& PIDTMP6=$!


	# IPv6/UDP. Should not matter that these throw error
#	echo $(python3 -c 'print("!"*26*1000)') | nc -u -6 -p $SOURCE_PORT localhost $CORRECT_PORT & PIDTMP7=$!
#	echo $(python3 -c 'print("!"*26*1000)') | nc -u -6 -p $SOURCE_PORT localhost $INVALID_PORT & PIDTMP8=$!

	# Wait for all processes
	wait $PIDTMP1 $PIDTMP2 $PIDTMP3 $PIDTMP4 $PIDTMP5 $PIDTMP6

done


# Shutdown
echo $PIDNC1
echo $PIDNC2
echo $PIDNC3
echo $PIDNC4
echo $PIDNC5
echo $PIDNC6



kill $PIDNC1
kill $PIDNC2
kill $PIDNC3
kill $PIDNC4
kill $PIDNC5
kill $PIDNC6
