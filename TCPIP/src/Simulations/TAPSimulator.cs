using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

using SME;
using SME.Components;

namespace TCPIP
{
    public class TAPSimulator
    {

        public TAPSimulator()
        {
            Console.WriteLine("Listening!");
 
            char[] buf = new char[256];

            string text;
            var fileStream = new FileStream("/tmp/tap_sme_pipe", FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream))
            {
                while(true) {
                if(streamReader.ReadBlock(buf, 0, 255) > 0) {
                    Console.WriteLine(buf);
                }
                }
            }

            fileStream.Close();
       }

    }

}
