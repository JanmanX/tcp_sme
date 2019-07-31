#include <fcntl.h>  /* O_RDWR */
#include <string.h> /* memset(), memcpy() */
#include <stdio.h> /* perror(), printf(), fprintf() */
#include <stdlib.h> /* exit(), malloc(), free() */
#include <sys/ioctl.h> /* ioctl() */

/* includes for struct ifreq, etc */
#include <sys/types.h>
#include <sys/socket.h>
#include <linux/if.h>
#include <linux/if_tun.h>

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



#define BUFFER_SIZE 2048


int tun_open(char *devname)
{
	struct ifreq ifr;
	int fd, err;

	if ( (fd = open("/dev/net/tun", O_RDWR)) == -1 ) {
		perror("open /dev/net/tun");
		exit(1);
	}
	memset(&ifr, 0, sizeof(ifr));
	ifr.ifr_flags = IFF_TUN;
	strncpy(ifr.ifr_name, devname, IFNAMSIZ); // devname = "tun0" or "tun1", etc

	/* ioctl will use ifr.if_name as the name of TUN
	 * interface to open: "tun0", etc. */
	if ( (err = ioctl(fd, TUNSETIFF, (void *) &ifr)) == -1 ) {
		perror("ioctl TUNSETIFF");close(fd);exit(1);
	}

	/* After the ioctl call the fd is "connected" to tun device specified
	 * by devname ("tun0", "tun1", etc)*/

	return fd;
}


int main(int argc, char *argv[])
{
	/* Connection to the TUN */
	int tun_fd;

	tun_fd = tun_open("tun1"); /* devname = ifr.if_name = "tun0" */
	printf("Device tun0 opened\n");

	/* Connection to the Simulator */
#define SERVER "127.0.0.1"
#define BUFLEN 2048 //Max length of buffer
#define PORT 8888  //The port on which to send data

	struct sockaddr_in si_other;
	int s, i, slen = sizeof(si_other);
	char buffer[BUFLEN];
	char message[BUFLEN];

	memset(buffer, 0, sizeof(buffer));

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



	/* Main loop */
	unsigned int foo = 0;
	while (1)
	{
		ssize_t bytes_read;
		if ((bytes_read = read(tun_fd, buffer, sizeof(buffer)-1)) > 0)
		{
			printf("Read %d bytes from tun1\n", bytes_read);

			if (bytes_read > 4)
			{
				/* Flags: 0x0060, Proto: 0x0000 */
				/* Flags: 0x0045, Proto: 0x3C00 */
				printf("%d: ", foo++);
				printf("Flags: 0x%X\t", ((uint32_t)buffer[0]) << 8 | buffer[1]);
				printf("Proto: 0x%X\n", ((uint32_t)buffer[2]) << 8 | buffer[3]);

				unsigned int j = 0;
				for(j = 4; j < bytes_read; j++) {
					printf("%c ", buffer[j]);
				}
				printf("\n");

			}
			sendto(s, buffer, bytes_read, 0, (struct sockaddr *)&si_other, slen);
			//            write(fd, buffer, bytes_read);
		}

	}

	return 0;
}
