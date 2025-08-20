// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;

var applicationDataFolder = $"AppData";
Directory.CreateDirectory(applicationDataFolder);
var tempDirectory = new TempDirectory(applicationDataFolder);

for (int i = 0; i < 10000; i++)
{
    var tempFileInfo = tempDirectory.CreateTempFileInfo();
    Console.WriteLine(tempFileInfo.FullName);
    tempFileInfo.Create().Dispose();

    var subDirectory = tempDirectory.CreateSubDirectory();
    foreach (var _ in Enumerable.Range(0, 10))
    {
        tempFileInfo = subDirectory.CreateTempFileInfo();
        Console.WriteLine(tempFileInfo.FullName);
        tempFileInfo.Create().Dispose();
    }
}

TempDirectory.SetTempDirectoryCanSafeDelete(tempDirectory);
TempDirectory.ClearTempDirectories(tempDirectory, 0);

Console.WriteLine("Hello, World!");

public partial class TempDirectory
{
    public TempDirectory(string applicationDataFolder)
    {
        if (!Directory.Exists(applicationDataFolder))
        {
            throw new ArgumentException($"输入的 applicationDataFolder {applicationDataFolder} 不存在");
        }

        _applicationsTempFolder = Path.Join(applicationDataFolder, "Temp");

        // 临时文件夹的命名影响临时文件夹的清理，如果需要修改请同时修改
        var tempFolderName = GetTempFolderName();
        var tempFolder = Path.Combine(_applicationsTempFolder, tempFolderName);
        _tempDirectoryInfo = Directory.CreateDirectory(tempFolder);
    }

    private static string GetTempFolderName()
    {
        // 命名规则： 年月日_小时分钟秒,进程ID
        // 20250724_171139,63912
        var now = DateTime.Now;
        var processId = Environment.ProcessId;
        return GetTempFolderName(now, processId);
    }

    private static string GetTempFolderName(DateTime now, int processId)
    {
        // 加上 FormattableString 防止多语言创建的文件夹离谱
        return FormattableString.Invariant($"{now:yyyyMMdd_HHmmss},{processId}");
    }

    /// <summary>
    /// 只有里层的临时文件夹才会使用此构造函数，外层的临时文件夹不需要传入参数
    /// </summary>
    /// <param name="tempDirectory"></param>
    /// <param name="subTempDirectoryInfo"></param>
    private TempDirectory(TempDirectory tempDirectory, DirectoryInfo subTempDirectoryInfo)
    {
        _applicationsTempFolder = tempDirectory._applicationsTempFolder;
        _tempDirectoryInfo = subTempDirectoryInfo;
    }

    private readonly string _applicationsTempFolder;
    private readonly DirectoryInfo _tempDirectoryInfo;

    private readonly
#if NET9_0
        Lock
#else
        object
#endif
        _lockObject = new();

    /// <summary>
    /// 创建临时文件(夹)的数量，用于快速创建临时文件(夹)
    /// </summary>
    private int _genericCount = 0;

    public static implicit operator string(TempDirectory tempDirectory) => tempDirectory._tempDirectoryInfo.FullName;

    public static implicit operator DirectoryInfo(TempDirectory tempDirectory)
    {
        return tempDirectory._tempDirectoryInfo;
    }

    public override string ToString()
    {
        return this;
    }

    /// <summary>
    ///     创建临时使用的文件
    /// </summary>
    /// <param name="fileExtension">文件扩展名，如 .png ，为空不添加扩展名</param>
    /// <param name="fileName">文件名，为空随机生成文件名</param>
    /// <returns></returns>
    public FileInfo CreateTempFileInfo(string? fileExtension = null, string? fileName = null)
    {
        if (string.IsNullOrEmpty(fileExtension) && !string.IsNullOrEmpty(fileName))
        {
            // 只传入文件名，没有传入扩展名的情况
            // 取出文件名里面的扩展名
            fileExtension = Path.GetExtension(fileName);
            fileName = Path.GetFileNameWithoutExtension(fileName);
        }

        if (!string.IsNullOrEmpty(fileExtension))
        {
            if (!fileExtension.StartsWith("."))
            {
                fileExtension = "." + fileExtension;
            }
        }

        var originFileName = string.IsNullOrEmpty(fileName) ? "file" : fileName;

        string fullName = this;

        // 无论是否传入了文件名，都执行一次生成文件名逻辑。解决判断文件名对应的文件不存在，多线程进入，下一个线程也判断不存在，于是两个线程将使用相同的文件
        fileName = CreateFileName();

        while (true)
        {
            var path = Path.Join(fullName, fileName + fileExtension);
            if (!File.Exists(path))
            {
                // 正常来说，不需要在此进行判断，不会有重复的文件名。除非是业务端自己在捣乱
                return new FileInfo(path);
            }

            fileName = CreateFileName();
        }

        string CreateFileName()
        {
            var genericCount = Interlocked.Increment(ref _genericCount);
            var name = FormattableString.Invariant($"{originFileName}{genericCount:X4}");
            return name;
        }
    }

    /// <summary>
    /// 创建一个子临时文件夹
    /// </summary>
    /// <param name="directoryName">临时文件夹名，如果不传入会自动创建一个空白的随机文件夹，如果传入已经存在的临时文件夹会返回已经存在的临时文件夹</param>
    /// <returns></returns>
    public TempDirectory CreateSubDirectory(string? directoryName = null)
    {
        if (string.IsNullOrEmpty(directoryName))
        {
            var genericCount = Interlocked.Increment(ref _genericCount);
            directoryName = FormattableString.Invariant($"t{genericCount:X4}");
        }

        var subDirectory = Path.Join(this, directoryName);
        return new TempDirectory(this, Directory.CreateDirectory(subDirectory));
    }

    /// <summary>
    /// 设置到环境变量 TEMP 的文件夹
    /// </summary>
    /// 通过临时变量修改程序的文件夹，默认使用的是系统的文件夹，但是系统的文件夹存在这样的问题
    /// 如果使用 Path.GetTempFileName() 方法创建的临时文件数量达到了 65535 个，而又不及时删除掉创建的文件的话，那么再调用此方法将抛出异常 IOException 此 API 调用创建的文件数量是当前用户账户下所有程序共同累计的，其他程序用“满”了你的进程也一样会挂
    /// 为什么这个方法是静态的？因为只有框架搭建模块才会调用此，其他业务模块不能调用。作为静态方法可以避免业务模块误用
    public static void SetToEnvironmentTempFolder(TempDirectory tempDirectory)
    {
        // [通过修改环境变量修改当前进程使用的系统 Temp 文件夹的路径 - walterlv](https://blog.walterlv.com/post/redirect-environment-temp-folder.html )
        Environment.SetEnvironmentVariable("TEMP", tempDirectory._tempDirectoryInfo.FullName);
        Environment.SetEnvironmentVariable("TMP", tempDirectory._tempDirectoryInfo.FullName);
    }

    /// <summary>
    /// 当进程退出时，可以调用此方法标记当前的临时文件夹可以安全删除。具体实现就是写入一个 .ThisFolderCanSafeDelete 文件到临时文件夹中，表示可以安全删除
    /// </summary>
    /// <param name="tempDirectory"></param>
    /// 为什么这个方法是静态的？因为只有框架搭建模块才会调用此，其他业务模块不能调用。作为静态方法可以避免业务模块误用
    public static void SetTempDirectoryCanSafeDelete(TempDirectory tempDirectory)
    {
        File.Create(Path.Join(tempDirectory, ThisFolderCanSafeDeleteFileName), 16, FileOptions.None)
            .Dispose();
    }

    private const string ThisFolderCanSafeDeleteFileName = ".ThisFolderCanSafeDelete";

    /// <summary>
    /// 清理临时文件夹
    /// </summary>
    /// <param name="tempDirectory">临时文件夹路径</param>
    /// <param name="keepDays">超过多少天删除临时文件</param>
    /// <param name="canThrowException"></param>
    /// 为什么这个方法是静态的？因为只有框架搭建模块才会调用此，其他业务模块不能调用。作为静态方法可以避免业务模块误用
    public static void ClearTempDirectories(TempDirectory tempDirectory, int keepDays, bool canThrowException = false)
    {
        var exceptionList = new List<Exception>();
        try
        {
            var processList = Process.GetProcesses().Select(temp => temp.Id).ToList();
            var tempFolderRoot = tempDirectory._applicationsTempFolder;
            // 只有找不到使用临时文件夹的进程并且超过7天才删除
            // 进程号可能重复，也就是原来的 100 进程创建的临时文件夹，在原来的 100 进程关闭之后经过10天，再次清理发现还是存在一个进程号是 100 的进程，这时不再清理这个临时文件夹，等下一次清理时再寻找。连续存在两次启动的过程发现相同的进程号的概率是很小的，不需要担心没有清理
            // 有手动标记 ThisFolderCanSafeDelete 文件的临时文件夹可以安全删除，无视进程是否存在
            IEnumerable<DirectoryInfo> deleteFolderList = GetToDeleteFolderList(processList, tempFolderRoot, keepDays);
            foreach (var folder in deleteFolderList)
            {
                try
                {
                    folder.Delete(recursive: true);
                }
                catch (Exception e)
                {
                    exceptionList.Add(e);
                }
            }

            // 删除tempFolderRoot目录下所有文件。
            foreach (var file in Directory.GetFiles(tempFolderRoot))
            {
                if ((DateTime.Now - File.GetCreationTime(file)).Days > keepDays)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }
        }
        catch (Exception e)
        {
            exceptionList.Add(e);
        }

        if (canThrowException)
        {
            if (exceptionList.Count > 0)
            {
                throw new AggregateException(exceptionList);
            }
        }
    }

    /// <summary>
    /// 获取需要删除的文件夹
    /// </summary>
    /// <returns></returns>
    private static IEnumerable<DirectoryInfo> GetToDeleteFolderList(List<int> processList, string tempFolderRoot,
        int keepDays)
    {
        foreach (var directory in Directory.EnumerateDirectories(tempFolderRoot))
        {
            var directoryInfo = new DirectoryInfo(directory);
            if (TryParseTempDirectoryProcessId(directoryInfo.Name, out var processId))
            {
                // 如果有写入 .ThisFolderCanSafeDelete 文件，表示可以安全删除
                var canDelete = File.Exists(Path.Join(directory, ThisFolderCanSafeDeleteFileName));
                if (!canDelete)
                {
                    // 没有写入文件，继续判断进程是否存在
                    var notExistedProcess = processList.All(t => t != processId);
                    if (notExistedProcess)
                    {
                        // 进程已经不存在了，此时才可以尝试删除文件夹
                        canDelete = true;
                    }
                }

                if (canDelete)
                {
                    // 尝试获取文件夹创建时间
                    var directoryCreationTime = directoryInfo.CreationTime;
                    var days = (DateTime.Now - directoryCreationTime).TotalDays;
                    // 如果是 keepDays 之前创建的文件夹，才可以进行删除
                    if (days > keepDays)
                    {
                        yield return directoryInfo;
                    }
                    else
                    {
                        // 不能删除了
                    }
                }
            }
            else
            {
                // 不按照规范命名的都是可以删除的
                yield return directoryInfo;
            }
        }
    }

    /// <summary>
    /// 尝试转换出临时文件夹的创建进程号 通过进程号可以用来清理临时文件夹
    /// </summary>
    /// <param name="folderName"></param>
    /// <param name="processId"></param>
    /// <returns></returns>
    private static bool TryParseTempDirectoryProcessId(string folderName, out int processId)
    {
        var regex = GetLogFolderProcessRegex();
        var match = regex.Match(folderName);
        if (match.Groups.Count > 1)
        {
            if (int.TryParse(match.Groups[1].Value, out processId))
            {
                return true;
            }
        }

        processId = -1;

        return false;
    }

    [GeneratedRegex(@"\d+_\d+,(\d+)")]
    private static partial Regex GetLogFolderProcessRegex();
}