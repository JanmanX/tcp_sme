#!/bin/bash

tunctl -u jan -t tapsme

ifconfig tapsme 10.0.0.1 up

route add -host 10.0.0.1 dev tapsme



### REMOVE
# tunctl -d tapsme
