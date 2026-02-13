// See https://aka.ms/new-console-template for more information

using System.Globalization;
using RemoteExecutors;

RemoteExecutor.Invoke(() =>
{
    // 写个文件测试一下
    var file = Path.Join(AppContext.BaseDirectory, "1.txt");
    File.WriteAllText(file, DateTime.Now.ToString(CultureInfo.InvariantCulture));
});

Console.WriteLine("Hello, World!");
