#include <stdio.h>
#include <unistd.h>
#include <sys/ioctl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <linux/if.h>
#include <linux/if_tun.h>
#include <stdarg.h>
#include <errno.h>

#define PIPE_NAME "/tmp/tap_sme_pipe"

static int tun_fd;
static char* dev;

char *tapaddr = "10.0.0.5";
char *taproute = "10.0.0.0/24";

int run_cmd(char *cmd, ...)
{
    va_list ap;

    #define CMDBUFLEN 100
    char buf[CMDBUFLEN];
    va_start(ap, cmd);
    vsnprintf(buf, CMDBUFLEN, cmd, ap);

    va_end(ap);

    return system(buf);
}


static int set_if_route(char *dev, char *cidr)
{
    return run_cmd("ip route add dev %s %s", dev, cidr);
}

static int set_if_address(char *dev, char *cidr)
{
    return run_cmd("ip address add dev %s local %s", dev, cidr);
}

static int set_if_up(char *dev)
{
    return run_cmd("ip link set dev %s up", dev);
}

/*
 * Taken from Kernel Documentation/networking/tuntap.txt
 */
static int tun_alloc(char *dev)
{
    struct ifreq ifr;
    int fd, err;

    if( (fd = open("/dev/net/tap", O_RDWR)) < 0 ) {
        perror("Cannot open TUN/TAP dev\n"
                    "Make sure one exists with "
                    "'$ mknod /dev/net/tap c 10 200'");
        exit(1);
    }

#define CLEAR(x) memset(&(x), 0, sizeof(x))
    CLEAR(ifr);

    /* Flags: IFF_TUN   - TUN device (no Ethernet headers)
     *        IFF_TAP   - TAP device
     *
     *        IFF_NO_PI - Do not provide packet information
     */
    ifr.ifr_flags = IFF_TAP | IFF_NO_PI;
    if( *dev ) {
        strncpy(ifr.ifr_name, dev, IFNAMSIZ);
    }

    if( (err = ioctl(fd, TUNSETIFF, (void *) &ifr)) < 0 ){
        perror("ERR: Could not ioctl tun");
        close(fd);
        return err;
    }

    strcpy(dev, ifr.ifr_name);
    return fd;
}

int tun_read(char *buf, int len)
{
    return read(tun_fd, buf, len);
}

int tun_write(char *buf, int len)
{
    return write(tun_fd, buf, len);
}

void tun_init()
{
    dev = calloc(10, 1);
    tun_fd = tun_alloc(dev);

    if (set_if_up(dev) != 0) {
        printf("ERROR when setting up if\n");
    }

    if (set_if_route(dev, taproute) != 0) {
        printf("ERROR when setting route for if\n");
    }

    if (set_if_address(dev, tapaddr) != 0) {
        printf("ERROR when setting addr for if\n");
    }
}

void free_tun()
{
    free(dev);
}

int main(int argc, char **argv) {
    tun_init();

    if( setuid(1000) != 0) {
        printf("Could not change UID");
    }

    if( mkfifo(PIPE_NAME, 0666) != 0) {
        printf("Could not create FIFO %s\n", PIPE_NAME);
        return -1;
    }

    int fd;
    if( (fd = open(PIPE_NAME, O_RDWR)) == -1) {
        printf("Could not open FIFO\n");
        return -1;
    }

#define BUFSZ 256
    char buf[BUFSZ] = {0};
    while(1) {
        ssize_t bytes_read;
        if((bytes_read = read(tun_fd, buf, BUFSZ-1)) > 0) {
            write(fd, buf, bytes_read);
            printf("%s", buf);
       }
    }
}
