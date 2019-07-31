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
#include <stdint.h>
#include <arpa/inet.h>
#include <sys/socket.h>
#include <string.h>

#define PIPE_NAME "/tmp/tun_sme_pipe"
#define BUFFER_SIZE 2048

static int tun_fd;
static char *dev;

char *tapaddr = "10.0.0.4";
char *taproute = "10.0.0.0/24";

int run_cmd(char *cmd, ...)
{
    va_list ap;

#define CMDBUFLEN 100
    char buf[CMDBUFLEN];
    va_start(ap, cmd);
    vsnprintf(buf, CMDBUFLEN, cmd, ap);

    va_end(ap);

    printf("Executing: %s\n", buf);

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

    if ((fd = open("/dev/net/tun", O_RDWR)) < 0)
    {
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
    ifr.ifr_flags = IFF_TUN; // | IFF_NO_PI;

    if (*dev)
    {
        strncpy(ifr.ifr_name, dev, IFNAMSIZ);
    }

    if ((err = ioctl(fd, TUNSETIFF, (void *)&ifr)) < 0)
    {
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

    if (set_if_up(dev) != 0)
    {
        printf("ERROR when setting up if\n");
    }

    if (set_if_route(dev, taproute) != 0)
    {
        printf("ERROR when setting route for if\n");
    }

    if (set_if_address(dev, tapaddr) != 0)
    {
        printf("ERROR when setting addr for if\n");
    }
}

void free_tun()
{
    free(dev);
}

int main(int argc, char **argv)
{
    int fd;
    char buffer[BUFFER_SIZE];

    // Initialize tunnel
    tun_init();

    // Change to user so that FIFO gets created with user permissions
    if (setuid(1000) != 0)
    {
        printf("Could not change UID\n");
        return -1;
    }

    if (access(PIPE_NAME, F_OK) != 0)
    {
        printf("FIFO not found. Trying to create one");

        if (mkfifo(PIPE_NAME, 0666) != 0)
        {
            printf("Could not create FIFO %s\n", PIPE_NAME);
            return -1;
        }
    }

    if ((fd = open(PIPE_NAME, O_RDWR)) == -1)
    {
        printf("Could not open FIFO\n");
        return -1;
    }

    /* Sockets... Dont ask why ... */
#define SERVER "127.0.0.1"
#define BUFLEN 512 //Max length of buffer
#define PORT 8888  //The port on which to send data

    struct sockaddr_in si_other;
    int s, i, slen = sizeof(si_other);
    char buf[BUFLEN];
    char message[BUFLEN];

    if ((s = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP)) == -1)
    {
        printf("socket is no\n");
        return -1;
    }

    memset((char *)&si_other, 0, sizeof(si_other));
    si_other.sin_family = AF_INET;
    si_other.sin_port = htons(PORT);

    if (inet_aton(SERVER, &si_other.sin_addr) == 0)
    {
        fprintf(stderr, "inet_aton() failed\n");
        exit(1);
    }

    memcpy(message, "what is this even?!", 20);

    if (sendto(s, message, strlen(message), 0, (struct sockaddr *)&si_other, slen) == -1)
    {
        printf("sendto()");
        return -1;
    }

    unsigned int foo = 0;
    while (1)
    {
        ssize_t bytes_read;
        if ((bytes_read = read(tun_fd, buffer, BUFFER_SIZE - 1)) > 0)
        {
            if (bytes_read > 4)
            {
                /* Flags: 0x0060, Proto: 0x0000 */
                /* Flags: 0x0045, Proto: 0x3C00 */
                printf("%d: ", foo++);
                printf("Flags: 0x%X\t", ((uint32_t)buffer[0]) << 8 | buffer[1]);
                printf("Proto: 0x%X\n", ((uint32_t)buffer[2]) << 8 | buffer[3]);

		unsigned int j = 0;
		for(j = 4; i < BUFLEN; i++) {
			printf("%c ", buffer[j]);
		}
		printf("\n");

            }
            sendto(s, buffer, bytes_read, 0, (struct sockaddr *)&si_other, slen);
            //            write(fd, buffer, bytes_read);
        }

        //        if ((bytes_read = read(fd, buffer, BUFFER_SIZE - 1)) > 0)
        //        {
        //            write(tun_fd, buffer, bytes_read);
        //        }
    }
}
