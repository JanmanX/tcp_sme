using System;
using System.IO;
using System.Threading.Tasks;
using SME;

namespace TCPIP
{
    // Is not actually used, but replaced by the preprocessor if used in *.cpp.cs files
    public class LOGGER
    {
        public static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        public static void TRACE(string x) { log.Trace(x); }
        public static void DEBUG(string x) { log.Debug(x); }
        public static void INFO(string x) { log.Info(x); }
        public static void WARN(string x) { log.Warn(x); }
        public static void ERROR(string x) { log.Error(x); }
        public static void FATAL(string x) { log.Fatal(x); }
    }
    // Used for actual logging, simply a wrapper with another name to not
    // conflict with the preprosessor
    class Logging
    {
        public static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
    }
}