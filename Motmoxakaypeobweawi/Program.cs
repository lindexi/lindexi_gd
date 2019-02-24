using System;
using System.Diagnostics;
using System.Management.Automation;

namespace Motmoxakaypeobweawi
{
    class Program
    {
        static void Main(string[] args)
        {
            Execute("([System.Management.Automation.ActionPreference], [System.Management.Automation.AliasAttribute]).FullName");
        }

        public static void Execute(string command)
        {
            using (var ps = PowerShell.Create())
            {
                var results = ps.AddScript(command).Invoke();
                foreach (var result in results)
                {
                    Console.Write(result.ToString());
                }
            }
        }
    }
}
