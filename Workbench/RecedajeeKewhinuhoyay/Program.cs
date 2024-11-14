// See https://aka.ms/new-console-template for more information

var workFolder = Path.Join(Path.GetTempPath(), "Test-RecedajeeKewhinuhoyay");
Directory.CreateDirectory(workFolder);

Console.WriteLine($"测试文件夹 {workFolder}");

var locker = new object();

var taskList = new List<Task>();

for (int i = 0; i < 50; i++)
{
    var task = Task.Run(async () =>
    {
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Join(workFolder, fileName + ".tmp");

        await MockDownloadAsync(filePath);

        var newFilePath = Path.Join(workFolder, fileName);
        try
        {
            File.Move(filePath, newFilePath);
        }
        catch (Exception e)
        {
            /*
            Move file fail. FilePath=C:\Users\admin\AppData\Local\Temp\Test-RecedajeeKewhinuhoyay\zvxau5gx.lmz.tmp. HResult=80070003;System.IO.DirectoryNotFoundException: Could not find a part of the path.
               at System.IO.FileSystem.MoveFile(String, String, Boolean)
               at System.IO.File.Move(String, String, Boolean)
               at System.IO.File.Move(String, String)
               at Program.<>c__DisplayClass0_0.<<<Main>$>b__2>d.MoveNext()
             */

            // 新文件存在: True;原文件存在： False

            if (e is IOException ioException)
            {
                Output($"Move file fail. FilePath={filePath}. HResult={ioException.HResult:X};\r\n新文件存在: {File.Exists(newFilePath)};原文件存在： {File.Exists(filePath)}\r\n{ioException}");
            }
            else
            {
                Output($"Move file fail. FilePath={filePath}.\r\n新文件存在: {File.Exists(newFilePath)};原文件存在： {File.Exists(filePath)}\r\n{e}");
            }
        }
    });

    taskList.Add(task);
}

void Output(string message)
{
    lock (locker)
    {
        Console.WriteLine(message);
    }
}

async Task MockDownloadAsync(string filePath)
{
    using var fileStream = File.OpenWrite(filePath);
    var buffer = new byte[10240];
    for (int i = 0; i < 100; i++)
    {
        Random.Shared.NextBytes(buffer);
        fileStream.Write(buffer, 0, buffer.Length);

        await Task.Delay(15);
    }
}

Task.WaitAll(taskList);

Console.WriteLine("运行完成");
