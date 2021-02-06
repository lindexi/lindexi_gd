using System;
using System.Linq;
using System.Management;

namespace QabebeedeewewoheaKoyerejefowawle
{
    class Program
    {
        static void Main(string[] args)
        {
            ManagementClass mc = new ManagementClass(
                $@"\\{Environment.MachineName}\root\wmi:WmiMonitorDescriptorMethods");

            foreach (ManagementObject mo in mc.GetInstances()) //Do this for each connected monitor
            {
                for (int i = 0; i < 256; i++)
                {
                    ManagementBaseObject inParams = mo.GetMethodParameters("WmiGetMonitorRawEEdidV1Block");
                    inParams["BlockId"] = i;

                    try
                    {
                        var outParams = mo.InvokeMethod("WmiGetMonitorRawEEdidV1Block", inParams, null);
                        Console.WriteLine($"Returned a block of type {outParams["BlockType"]}, having a content of type {outParams["BlockContent"].GetType()} {string.Join(" ", ((byte[])outParams["BlockContent"]).Select(temp => temp.ToString("X2")))}");
                    }
                    catch
                    {
                        break;
                    } //No more EDID blocks
                }
            }
        }
    }
}