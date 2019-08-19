using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Windows.Forms;

namespace PageTurning
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var guid = Guid.NewGuid().ToString("N");

                for (int i = 1; i <= 10000; i++)
                {
                    Process p = new Process();
                    //设置要启动的应用程序
                    p.StartInfo.FileName = "cmd.exe";
                    //是否使用操作系统shell启动
                    p.StartInfo.UseShellExecute = false;
                    // 接受来自调用程序的输入信息
                    p.StartInfo.RedirectStandardInput = true;
                    //输出信息
                    p.StartInfo.RedirectStandardOutput = true;
                    // 输出错误
                    p.StartInfo.RedirectStandardError = true;
                    //不显示程序窗口
                    p.StartInfo.CreateNoWindow = true;

                    Thread.Sleep(2000);

                    var directory = new DirectoryInfo($"第{i}次 {guid}");
                    directory.Create();
                    string pptFile = GetPPT();
                    Console.WriteLine("启动文件： " + pptFile);

                    Console.WriteLine(@"第 " + i + " 次启动：");
                    p.Start();
                    //向cmd窗口发送输入信息
                    Thread.Sleep(2000);
                    p.StandardInput.WriteLine("\"" + pptFile + "\"");
                    p.StandardInput.AutoFlush = true;
                    Console.WriteLine(@"开始启动客户端");

                    Thread.Sleep(10000);

                    //触发F5按键
                    keybd_event(116, 0, 0, 0);

                    for (int j = 1; j < 10; j++)
                    {
                        Thread.Sleep(2000);
                        keybd_event(vbKeyDown, 0, 0, 0);
                        Console.WriteLine(@"开始翻页");

                        // 翻一页截图
                        // 通过Graphics的CopyFromScreen方法把全屏图片的拷贝到我们定义好的一个和屏幕大小相同的空白图片中，
                        // 拷贝完成之后，CatchBmp就是全屏图片的拷贝了，然后指定为截图窗体背景图片就好了。
                        // 新建一个和屏幕大小相同的图片
                        Bitmap CatchBmp = new Bitmap(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height);

                        // 创建一个画板，让我们可以在画板上画图
                        // 这个画板也就是和屏幕大小一样大的图片
                        // 我们可以通过Graphics这个类在这个空白图片上画图
                        Graphics g = Graphics.FromImage(CatchBmp);

                        // 把屏幕图片拷贝到我们创建的空白图片 CatchBmp中
                        g.CopyFromScreen(new Point(0, 0), new Point(0, 0),
                            new Size(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height));

                        var file = new FileInfo(Path.Combine(directory.FullName, $"{j} {DateTime.Now:MM DD HHmmss}.png"));

                        var fileStream = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);

                        using (fileStream)
                        {
                            CatchBmp.Save(fileStream, ImageFormat.Png);
                            CatchBmp.Dispose();
                        }

                        Console.WriteLine("保存截图" + file.FullName);
                    }

                    Thread.Sleep(2000);

                    keybd_event(18, 0, 0, 0);
                    keybd_event(115, 0, 0, 0);
                    keybd_event(18, 0, 2, 0);
                    keybd_event(115, 0, 2, 0);

                    Thread.Sleep(2000);

                    Console.WriteLine("干掉进程");

                    foreach (var temp in Process.GetProcesses())
                    {
                        try
                        {
                            if (temp.ProcessName.IndexOf("power", StringComparison.InvariantCultureIgnoreCase) >= 0)
                            {
                                temp.Kill();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }

                    p.Kill();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static string GetPPT()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var list = directory.GetFiles("*.pptx").ToList();

            return list[_random.Next(list.Count)].FullName;
        }

        private static readonly Random _random = new Random();

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        public const byte vbKeyDown = 0x28; // DOWN ARROW 键
    }
}