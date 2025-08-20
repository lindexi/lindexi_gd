// See https://aka.ms/new-console-template for more information
using System.IO;

var applicationDataFolder = $"AppData";
Directory.CreateDirectory(applicationDataFolder);
var tempDirectory = new TempDirectory(applicationDataFolder);

for (int i = 0; i < 10000; i++)
{
    var tempFileInfo = tempDirectory.CreateTempFileInfo();
    Console.WriteLine(tempFileInfo.FullName);
}

Console.WriteLine("Hello, World!");

public class TempDirectory
{
    public TempDirectory(string applicationDataFolder)
    {
        if (!Directory.Exists(applicationDataFolder))
        {
            throw new ArgumentException($"输入的 applicationDataFolder {applicationDataFolder} 不存在");
        }

        _applicationsTempFolder = Path.Join(applicationDataFolder, "Temp");

        // 临时文件夹的命名影响临时文件夹的清理，如果需要修改请同时修改
        var tempFolder = Path.Combine(_applicationsTempFolder,
            // 加上 FormattableString 防止多语言创建的文件夹离谱
            FormattableString.Invariant($"{Environment.ProcessId}-{DateTime.Now:yy.MM.dd,HH-mm-ss,fffffff}"));
        _tempDirectoryInfo = Directory.CreateDirectory(tempFolder);
    }

    /// <summary>
    /// 设置到环境变量 TEMP 的文件夹
    /// </summary>
    /// 通过临时变量修改程序的文件夹，默认使用的是系统的文件夹，但是系统的文件夹存在这样的问题
    /// 如果使用 Path.GetTempFileName() 方法创建的临时文件数量达到了 65535 个，而又不及时删除掉创建的文件的话，那么再调用此方法将抛出异常 IOException 此 API 调用创建的文件数量是当前用户账户下所有程序共同累计的，其他程序用“满”了你的进程也一样会挂
    public void SetToEnvironmentTempFolder()
    {
        // [通过修改环境变量修改当前进程使用的系统 Temp 文件夹的路径 - walterlv](https://blog.walterlv.com/post/redirect-environment-temp-folder.html )
        Environment.SetEnvironmentVariable("TEMP", _tempDirectoryInfo.FullName);
        Environment.SetEnvironmentVariable("TMP", _tempDirectoryInfo.FullName);
    }

    private readonly string _applicationsTempFolder;
    private readonly DirectoryInfo _tempDirectoryInfo;

    private readonly
#if NET9_0
        Lock
#else
        object
#endif
        _lockObject = new ();

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
    public DirectoryInfo CreateSubDirectory(string? directoryName = null)
    {
        if (string.IsNullOrEmpty(directoryName))
        {
            var genericCount = Interlocked.Increment(ref _genericCount);
            directoryName = FormattableString.Invariant($"t{genericCount:X4}");
        }

        var subDirectory = Path.Join(this, directoryName);
        return Directory.CreateDirectory(subDirectory);
    }
}