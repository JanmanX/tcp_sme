using System;
using System.IO;
using System.Threading.Tasks;
using SME;

namespace TCPIP
{
    // Is not actually used, but replaced by the preprocessor if used in *.cpp.cs files
    // public class LOGGER
    // {
    //     static string err = "LOGGER (not to be confused with Logging) cannot be called at runtime. Check if the file ends with *.cpp.cs";
    //     private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
    //     public static void TRACE(string x) { throw new System.Exception(err); }
    //     public static void DEBUG(string x) { throw new System.Exception(err); }
    //     public static void INFO(string x) { throw new System.Exception(err); }
    //     public static void WARN(string x) { throw new System.Exception(err); }
    //     public static void ERROR(string x) { throw new System.Exception(err); }
    //     public static void FATAL(string x) { throw new System.Exception(err); }
    // }
    // Used for actual logging, simply a wrapper with another name to not
    // conflict with the preprosessor
    class Logging
    {
        public static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
    }
    class GENERAL_TOOLS{
        // All general macros are inline, where the first argument of the function
        // is the variable in which the return is saved;
        public static void GENERATE_CHECKSUM(ushort test)
        {
            throw new System.Exception("Not implemented yet");
        }

    }
}