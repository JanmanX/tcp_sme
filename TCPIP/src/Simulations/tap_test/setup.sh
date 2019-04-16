#!/bin/bash

# Create new TAP device
ip tuntap add mode tap tapsme

# Bring the new TAP device up
ip link tapsme up

# Assign IP address (can be CIDR notation)
ip addr add 10.0.0.10 dev tapsme


