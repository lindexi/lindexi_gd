using System;

namespace HechearjofereWifallcecawo
{
    class Program
    {
        static void Main(string[] args)
        {
            Random ran = new Random();
            Predicate<ProcessInfo>? processFilter = t => t.Id > 10;
            ProcessInfo[] processInfos = new ProcessInfo[100];
            for (int i = 0; i < 100; i++)
            {
                processInfos[i] = new ProcessInfo()
                {
                    Id = ran.Next(0,20)
                };
            }

            int processesLength = 0;

            for (int i = 0; i < processInfos.Length; i++)
            {
                ProcessInfo processInfo = processInfos[i];
                if (processFilter == null || processFilter(processInfo))
                {
                    if (i != processesLength)
                    {
                        processInfos[processesLength] = processInfo;
                        processesLength++;
                    }
                }
            }

            for (int i = 0; i < processesLength; i++)
            {
                Console.WriteLine(processInfos[i].Id);
            }
        }
    }

    class ProcessInfo
    {
        public int Id { get; set; }
    }
}
