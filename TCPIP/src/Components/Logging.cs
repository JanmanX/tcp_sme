using System;
using System.IO;
using System.Threading.Tasks;
using SME;

namespace TCPIP
{
    class Logger{
        public static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        // Template void functions, changed with the preprocessor
        public static void TRACE(string x){log.Trace(x);}
        public static void DEBUG(string x){log.Debug(x);}
        public static void INFO(string x){log.Info(x);}
        public static void WARN(string x){log.Warn(x);}
        public static void ERROR(string x){log.Error(x);}
        public static void FATAL(string x){log.Fatal(x);}
    }
class Logging{
        public static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        // Template void functions, changed with the preprocessor
        public static void TRACE(string x){log.Trace(x);}
        public static void DEBUG(string x){log.Debug(x);}
        public static void INFO(string x){log.Info(x);}
        public static void WARN(string x){log.Warn(x);}
        public static void ERROR(string x){log.Error(x);}
        public static void FATAL(string x){log.Fatal(x);}
    }
}