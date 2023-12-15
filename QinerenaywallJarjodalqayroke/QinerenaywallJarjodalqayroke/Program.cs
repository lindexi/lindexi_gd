using System.Diagnostics;
using System.Text;
using Lindexi.Src.GitCommand;

if (args.Length == 0 || args[0] == "Merge")
{
    var slnFolder = FindSlnFolder(AppContext.BaseDirectory);

    if (slnFolder is null)
    {
        Console.WriteLine($"找不到代码文件夹");
        return;
    }

    var rootFolder = slnFolder.Parent!;

    var git = new Git(rootFolder);

    // 找到当前的 commit 号
    var currentCommit = git.GetCurrentCommit();

    using Process p = new Process
    {
        StartInfo =
        {
            FileName = "cmd.exe",
            WorkingDirectory = Path.Join(rootFolder.Parent!.FullName, "lindexi"),
            UseShellExecute = false, //是否使用操作系统shell启动
            RedirectStandardInput = true, //接受来自调用程序的输入信息
            RedirectStandardOutput = true, //由调用程序获取输出信息
            RedirectStandardError = true, //重定向标准错误输出
            CreateNoWindow = true, //不显示程序窗口
            StandardOutputEncoding = Encoding.GetEncoding("GBK") //Encoding.UTF8
            //Encoding.GetEncoding("GBK");//乱码
        }
    };

    p.Start(); //启动程序

    //向cmd窗口发送输入信息
    p.StandardInput.WriteLine($"git merge {currentCommit}" + "&exit");

    p.StandardInput.AutoFlush = true;

    //获取cmd窗口的输出信息
    string output = p.StandardOutput.ReadToEnd();
    output += p.StandardError.ReadToEnd();
    p.WaitForExit(); //等待程序执行完退出进程
    Console.WriteLine(output);
}
else
{

}

DirectoryInfo? FindSlnFolder(string folder)
{
    DirectoryInfo? currentFolder = new DirectoryInfo(folder);
    while (currentFolder != null)
    {
        if (currentFolder.EnumerateFiles("*.sln").Any())
        {
            return currentFolder;
        }

        currentFolder = currentFolder.Parent;
    }

    return null;
}