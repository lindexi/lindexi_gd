using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;

namespace RalboleaballNeeqaqiku
{
    class Program
    {
        static void Main(string[] args)
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();

            for (var i = 1; i <= instances.Count; i++)
            {
                var instance = instances[i - 1];
                var recommended = string.Empty;

                // The dev console is probably always the right choice because the user explicitly opened
                // one associated with a Visual Studio install. It will always be first in the list.
                if (instance.DiscoveryType == DiscoveryType.DeveloperConsole)
                    recommended = " (Recommended!)";

                Console.WriteLine($"{i}) {instance.Name} - {instance.Version}{recommended}");
            }

            // 必须调用 RegisterInstance 方法，否则将提示找不到 msbuild 文件
            MSBuildLocator.RegisterInstance(instances.First());

            // 需要将构建的代码放在另一个方法里面，否则将会因为放在相同的方法，没有加上程序集
            Build();
        }

        private static void Build()
        {
            var projectFile = new FileInfo(@"..\..\..\RalboleaballNeeqaqiku.csproj");

            var projectRootElement = ProjectRootElement.Open(projectFile.FullName);
            var project = new Project(projectRootElement);
            project.Build(new Logger());
        }

        private class Logger : ILogger
        {
            public void Initialize(IEventSource eventSource)
            {
                eventSource.AnyEventRaised += (_, args) => { Console.WriteLine(args.Message); };
            }

            public void Shutdown()
            {
            }

            public LoggerVerbosity Verbosity { get; set; }
            public string Parameters { get; set; }
        }
    }
}
