using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;

namespace BerjearnearheliCallrachurjallhelur
{
    class Program
    {
        static void Main(string[] args)
        {
            using (DataTarget dataTarget = DataTarget.LoadCrashDump(@"f:\lindexi\test\dotnet.exe.52348.dmp"))
            {
                foreach (ClrInfo version in dataTarget.ClrVersions)
                {
                    Console.WriteLine("Found CLR Version: " + version.Version);

                    // This is the data needed to request the dac from the symbol server:
                    DacInfo dacInfo = version.DacInfo;

                    Console.WriteLine("Filesize:  {0:X}", dacInfo.IndexFileSize);
                    Console.WriteLine("Timestamp: {0:X}", dacInfo.IndexTimeStamp);
                    Console.WriteLine("Dac File:  {0}", dacInfo.PlatformSpecificFileName);

                    // If we just happen to have the correct dac file installed on the machine,
                    // the "LocalMatchingDac" property will return its location on disk:
                    string dacLocation = version.LocalMatchingDac;
                    if (!string.IsNullOrEmpty(dacLocation))
                        Console.WriteLine("Local dac location: " + dacLocation);

                    // You may also download the dac from the symbol server, which is covered
                    // in a later section of this tutorial.
                }
            }
        }
    }
}
