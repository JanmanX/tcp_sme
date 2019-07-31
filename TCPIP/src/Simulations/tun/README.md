# HOWTO
Sometimes I dont know ...

```
sudo ip tuntap add mode tun dev tun0
ip addr add 10.0.0.0/24 dev tun0  # give it an ip
ip link set dev tun0 up  # bring the if up
ip route get 10.0.0.2  # check that packets to 10.0.0.x are going through tun0
ping 10.0.0.2  # leave this running in another shell to be able to see the effect of the next exampl
```
