// See https://aka.ms/new-console-template for more information

using System;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

public class Program
{
    static void Main(string[] args)
    {
        var file = @"..\..\..\..\CalbuhewaNallrolayrani\bin\Debug\net45\CalbuhewaNallrolayrani.exe";
        file = Path.GetFullPath(file);

        var configFile = file + ".config";
        if (File.Exists(configFile))
        {
            var xDocument = XDocument.Load(configFile);
            var element = xDocument.XPathSelectElement("/configuration/startup/supportedRuntime");
            if (element != null)
            {
                var sku = element.Attribute("sku");
                var version = sku?.Value;
                var match = Regex.Match(version ?? string.Empty, @"\.NETFramework,Version=v(\S+)");
                if (match.Success)
                {
                    var dotnet = match.Groups[1].Value;
                }
            }
        }
    }
}
