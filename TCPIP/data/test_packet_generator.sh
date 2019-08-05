#!/bin/bash

# RUN THIS IN ANOTHER TERMINAL
# tcpdump -i lo port $CORRECT_PORT or port $WRONG_PORT -w dump.bin -U & PIDTCPDUMP=$!


# CONFIG
ORRECT_PORT=6543
WRONG_PORT=3456


# Start listeners
# UDP
nc -u -l $CORRECT_PORT & PIDNC1=$!
nc -u -l $WRONG_PORT & PIDNC2=$!
# TCP
nc -k -l $CORRECT_PORT & PIDNC3=$!
nc -k -l $WRONG_PORT & PIDNC4=$!


# send data
for i in {1..64}
do
	echo "Iteration $i"

	# IPv4/UDP
	echo $(python3 -c 'import string;print("!"*26*1000)') | nc -4 -u -p 6666 localhost $WRONG_PORT & PIDTMP2=$!
	echo $(python3 -c 'import string;print(string.ascii_uppercase*1000)') | nc -4 -u -p 6666 localhost $CORRECT_PORT & PIDTMP1=$!

	# IPv4/TCP
	echo $(python3 -c 'import string;print("!"*26*1000)') | nc -4 -p 6666 localhost $CORRECT_PORT & PIDTMP3=$!
	echo $(python3 -c 'import string;print("!"*26*1000)') | nc -4 -p 6666 localhost $WRONG_PORT & PIDTMP4=$!

	# IPv6/UDP
	echo $(python3 -c 'import string;print("!"*26*1000)') | nc -u -6 -p 6666 localhost $CORRECT_PORT & PIDTMP5=$!
	echo $(python3 -c 'import string;print("!"*26*1000)') | nc -u -6 -p 6666 localhost $WRONG_PORT & PIDTMP6=$!

	# Wait for all processes
	wait $PIDTMP1 $PIDTMP2 $PIDTMP3 $PIDTMP4 $PIDTMP5 $PIDTMP6

done


# Shutdown
echo $PIDNC1
echo $PIDNC2
echo $PIDNC3
echo $PIDNC4

kill $PIDNC1
kill $PIDNC2
kill $PIDNC3
kill $PIDNC4

