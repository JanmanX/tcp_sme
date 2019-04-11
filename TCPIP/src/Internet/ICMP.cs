using System;
using SME;

namespace TCPIP
{
/*
         0                   1                   2                   3  
         0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |      Type     |      Code     |            Checksum           |
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |                                                               |
        +                          Message Body                         +
        |                                                               |
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
 */

    public class ICMP
    {
        public const uint TYPE_OFFSET = 0x00;
        public const uint CODE_OFFSET = 0x01;
        public const uint CHECKSUM_OFFSET_0 = 0x02;
        public const uint CHECKSUM_OFFSET_1 = 0x03;


        // Types 
        public const byte ICMP_UNREACH = 3;/* dest unreachable, codes: */
        public const byte ICMP_UNREACH_NET = 0;/* bad net */
        public const byte ICMP_UNREACH_HOST = 1;/* bad host */
        public const byte ICMP_UNREACH_PROTOCOL = 2;/* bad protocol */
        public const byte ICMP_UNREACH_PORT = 3;/* bad port */
        public const byte ICMP_UNREACH_NEEDFRAG = 4;/* IP_DF caused drop */
        public const byte ICMP_UNREACH_SRCFAIL = 5;/* src route failed */
        public const byte ICMP_UNREACH_NET_UNKNOWN = 6;/* unknown net */
        public const byte ICMP_UNREACH_HOST_UNKNOWN = 7;/* unknown host */
        public const byte ICMP_UNREACH_ISOLATED = 8;/* src host isolated */
        public const byte ICMP_UNREACH_NET_PROHIB = 9;/* prohibited access */
        public const byte ICMP_UNREACH_HOST_PROHIB = 10;/* ditto */
        public const byte ICMP_UNREACH_TOSNET = 11;/* bad tos for net */
        public const byte ICMP_UNREACH_TOSHOST = 12;/* bad tos for host */
        public const byte ICMP_UNREACH_ADMIN_PROHIBIT = 13;/* communication
							   administratively
							   prohibited */
        public const byte ICMP_UNREACH_HOST_PREC = 14;/* host precedence
							   violation */
        public const byte ICMP_UNREACH_PREC_CUTOFF = 15;/* precedence cutoff */
        public const byte ICMP_SOURCEQUENCH = 4;/* packet lost, slow down */
        public const byte ICMP_REDIRECT = 5;/* shorter route, codes: */
        public const byte ICMP_REDIRECT_NET = 0;/* for network */
        public const byte ICMP_REDIRECT_HOST = 1;/* for host */
        public const byte ICMP_REDIRECT_TOSNET = 2;/* for tos and net */
        public const byte ICMP_REDIRECT_TOSHOST = 3;/* for tos and host */
        public const byte ICMP_ALTHOSTADDR = 6;/* alternative host address */
        public const byte ICMP_ECHO = 8;/* echo service */
        public const byte ICMP_ROUTERADVERT = 9;/* router advertisement */
        public const byte ICMP_ROUTERADVERT_NORMAL = 0;
        public const byte ICMP_ROUTERADVERT_NOROUTE = 16;
        public const byte ICMP_ROUTERSOLICIT = 10;/* router solicitation */
        public const byte ICMP_TIMXCEED = 11;/* time exceeded, code: */
        public const byte ICMP_TIMXCEED_INTRANS = 0;/* ttl==0 in transit */
        public const byte ICMP_TIMXCEED_REASS = 1;/* ttl==0 in reass */
        public const byte ICMP_PARAMPROB = 12;/* ip header bad */
        public const byte ICMP_PARAMPROB_ERRATPTR = 0;
        public const byte ICMP_PARAMPROB_OPTABSENT = 1;
        public const byte ICMP_PARAMPROB_LENGTH = 2;
        public const byte ICMP_TSTAMP = 13;/* timestamp request */
        public const byte ICMP_TSTAMPREPLY = 14;/* timestamp reply */
        public const byte ICMP_IREQ = 15;/* information request */
        public const byte ICMP_IREQREPLY = 16;/* information reply */
        public const byte ICMP_MASKREQ = 17;/* address mask request */
        public const byte ICMP_MASKREPLY = 18;/* address mask reply */
        public const byte ICMP_TRACEROUTE = 30;/* traceroute */
        public const byte ICMP_DATACONVERR = 31;/* data conversion error */
        public const byte ICMP_MOBILE_REDIRECT = 32;/* mobile redirect */
        public const byte ICMP_IPV6_WHEREAREYOU = 33;/* ipv6 where are you */
        public const byte ICMP_IPV6_IAMHERE = 34;/* ipv6 i am here */
        public const byte ICMP_MOBILE_REGREQUEST = 35;/* mobile registration req */
        public const byte ICMP_MOBILE_REGREPLY = 36;/* mobile registration reply */
        public const byte ICMP_SKIP = 39;/* SKIP */
        public const byte ICMP_PHOTURIS = 40;/* security */
        public const byte ICMP_PHOTURIS_UNKNOWN_INDEX = 0;/* unknown sec index */
        public const byte ICMP_PHOTURIS_AUTH_FAILED = 1;/* auth failed */
        public const byte ICMP_PHOTURIS_DECOMPRESS_FAILED = 2;/* decompress failed */
        public const byte ICMP_PHOTURIS_DECRYPT_FAILED = 3;/* decrypt failed */
        public const byte ICMP_PHOTURIS_NEED_AUTHN = 4;/* no authentication */
        public const byte ICMP_PHOTURIS_NEED_AUTHZ = 5;/* no authorization */



    }
}