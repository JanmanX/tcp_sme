using System;
using System.IO;
using System.Threading.Tasks;
using SME;

namespace TCPIP
{
    class Logger{
        public static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
       
    }
}