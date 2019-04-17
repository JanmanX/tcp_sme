#!/bin/bash

tunctl -t tap1
ip link set dev tap1 up
ip route add dev tap1 192.168.100.0/24
ip address add dev tap1 local 192.168.100.1




### REMOVE
# tunctl -d tapsme
