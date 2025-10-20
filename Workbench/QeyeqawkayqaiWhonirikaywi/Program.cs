// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

if (OperatingSystem.IsWindows())
{
    BoostHelper.AddStartup("Test", Environment.ProcessPath!);
}
Console.WriteLine("Hello, World!");
Console.Read();

static class BoostHelper
{
    /// <summary>
    /// 添加到启动项，添加到注册表，仅限 Windows 系统，写入到 HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run 里，实现开机启动
    /// </summary>
    /// <param name="name"></param>
    /// <param name="exePath"></param>
    /// <param name="arguments"></param>
    [SupportedOSPlatform("Windows")]
    public static void AddStartup(string name, string exePath, params string[] arguments)
    {
        // 添加开机启动
        // 写入到 HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run 里
        var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        if (runKey is not null)
        {
            var valueStringBuilder = new StringBuilder();
            valueStringBuilder.Append($"\"{exePath}\"");
            foreach (var argument in arguments)
            {
                valueStringBuilder.Append($" \"{argument}\"");
            }

            string value = valueStringBuilder.ToString();

            var existingValue = runKey.GetValue(name) as string;
            if (existingValue != value)
            {
                runKey.SetValue(name, value);
                //LogMessage($"已设置开机启动，路径：{exePath}");
            }
            else
            {
                //LogMessage($"已设置过开机启动，路径：{exePath}");
            }
        }
    }

    [SupportedOSPlatform("Windows")]
    public static void RemoveStartup(string name)
    {
        var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        if (runKey is not null)
        {
            runKey.DeleteValue(name, false);
            //LogMessage($"已删除开机启动，路径：{exePath}");
        }
    }
}