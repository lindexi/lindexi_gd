using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RotDemo
{
    class Program
    {
        const string WppPath = @"C:\Users\Xyy\AppData\Local\Kingsoft\WPS Office\11.1.0.10578\office6\wpp.exe";
        const string WppArgs = "/wpp /from_prome ";
        static void Main(string[] args)
        {
            Console.WriteLine($"开始创建wpp进程，目标个数：{Environment.ProcessorCount * 2}");
            var initFiles = Directory.GetFiles(@"C:\Remote\Test");
            foreach (string path in initFiles.Take(Environment.ProcessorCount * 2))
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = WppPath,
                    Arguments = $"{WppArgs} \"{path}\"",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                process.WaitForInputIdle();
            }

            Console.WriteLine("让进程飞一会");
            Thread.Sleep(5000);

            Console.WriteLine("开始获取操作对象");
            var list = RunningObjectTable.GetPresentationsList();
            //待处理的文件数，100个文件，模拟并发
            var filePaths = Directory.GetFiles(@"C:\Remote\Input");
            List<TestResult> testResults = new List<TestResult>();
            for (int count = 1; count <= list.Count; count++)
            {
                TestResult result = new TestResult();
                result.WppCount = count;
                Console.WriteLine($"{count}进程开始");
                Stopwatch stopwatch = Stopwatch.StartNew();
                ConcurrentStack<string> stack = new ConcurrentStack<string>(filePaths);
                DirectoryInfo dir = new DirectoryInfo(@"C:\Remote\Output\");
                dir.Create();
                long totalElapsed = 0;
                Parallel.ForEach(list.Take(count), presentations =>
                {
                    while (stack.TryPop(out string path))
                    {
                        Stopwatch swatch = Stopwatch.StartNew();
                        var pres = presentations.Open(path);
                        string fileName = Path.Combine(dir.FullName, $"{Path.GetFileNameWithoutExtension(path)}.pdf");
                        pres.SaveCopyAs(fileName, PpSaveAsFileType.ppSaveAsPDF);
                        pres.Close();
                        swatch.Stop();
                        totalElapsed += swatch.ElapsedMilliseconds;
                        Console.WriteLine($"{count}-单文件“{Path.GetFileName(path)}”转换成功，耗时：{swatch.ElapsedMilliseconds}ms，时间：{DateTime.Now.ToString("mm:ss")}");
                    }
                });
                stopwatch.Stop();
                result.TotalElapsed = stopwatch.ElapsedMilliseconds;
                result.AverageElapsed = totalElapsed / filePaths.Length;
                testResults.Add(result);
                //把创建好的文件都删掉
                foreach (var file in dir.GetFiles())
                {
                    file.Delete();
                }
            }
            Console.WriteLine();
            Console.WriteLine("进程数\t总耗时\t平均耗时");
            testResults.ForEach(a => Console.WriteLine($"{a.WppCount}\t{a.TotalElapsed}\t{a.AverageElapsed}"));

            Console.WriteLine($"收工");
            Console.ReadLine();
        }
    }

    public class TestResult
    {
        public long TotalElapsed { get; set; }

        public long AverageElapsed { get; set; }

        public int WppCount { get; set; }
    }

    /// <summary>
    /// ROT操作
    /// </summary>
    public static class RunningObjectTable
    {
        /// <summary>
        /// 获取ROT
        /// </summary>
        /// <param name="reserved">保留字段</param>
        /// <param name="pprot">返回系统的ROT</param>
        /// <returns></returns>
        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(uint reserved, out IRunningObjectTable pprot);
        /// <summary>
        /// 创建绑定上下文
        /// </summary>
        /// <param name="reserved">保留字段</param>
        /// <param name="pctx">返回已创建的绑定上下文</param>
        /// <returns></returns>
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx pctx);

        public static List<Presentations> GetPresentationsList()
        {
            HashSet<Presentations> set = new HashSet<Presentations>();
            HashSet<Application> apps = new HashSet<Application>();
            // ROT
            IRunningObjectTable runningObjectTable = null;
            // Moniker-list
            IEnumMoniker monikerList = null;
            try
            {
                // 获取当年系统的ROT
                if (GetRunningObjectTable(0, out runningObjectTable) == 0 && runningObjectTable != null)
                {
                    runningObjectTable.EnumRunning(out monikerList);
                    monikerList.Reset();
                    IMoniker[] monikerContainer = new IMoniker[1];
                    IntPtr pointerFetchedMonikers = IntPtr.Zero;
                    while (monikerList.Next(1, monikerContainer, pointerFetchedMonikers) == 0)
                    {
                        CreateBindCtx(0, out IBindCtx bindInfo);
                        monikerContainer[0].GetDisplayName(bindInfo, null, out string displayName);
                        Marshal.ReleaseComObject(bindInfo);
                        runningObjectTable.GetObject(monikerContainer[0], out object comInstance);

                        if (comInstance is Presentation presentation && presentation.Parent is Presentations ps)
                        {
                            if (!set.Contains(ps))
                            {
                                set.Add(ps);
                            }
                            if (!apps.Contains(ps.Application))
                            {
                                apps.Add(ps.Application);
                            }
                            Console.WriteLine($"我是Presentation，我是{displayName}");
                        }
                        else if (comInstance is Application)
                        {
                            Console.WriteLine($"我是Application，我是{displayName}");
                        }
                        else
                        {
                            Console.WriteLine($"我是其他，{displayName}");
                        }
                    }
                }
            }
            finally
            {
                //释放相关的对象
                if (runningObjectTable != null) Marshal.ReleaseComObject(runningObjectTable);
                if (monikerList != null) Marshal.ReleaseComObject(monikerList);
            }
            Console.WriteLine($"对象池中包含{set.Count}个演示文稿窗口,{apps.Count}个演示文稿进程");
            return set.ToList();
        }
    }
}
