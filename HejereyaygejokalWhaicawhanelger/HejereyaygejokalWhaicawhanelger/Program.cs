using System.Diagnostics;
using System.Text;

var foo = ProcessCommand.ExecuteCommand("dotnet", $" \"{@"C:\lindexi\Foo\Foo.sln"}\" publish -r win-x86 -c release --self-contained true");
(bool success,string message) = foo;

readonly struct ProcessOutputInfo
{
    public ProcessOutputInfo(OutputType outputType, string? message)
    {
        OutputType = outputType;
        Message = message;

        OutputTime = DateTime.Now;
    }

    public OutputType OutputType { get; }
    public string? Message { get; }
    public DateTimeOffset OutputTime { get; }
}

enum OutputType
{
    StandardOutput,
    ErrorOutput,
}

readonly struct ProcessResult
{
    public ProcessResult(int exitCode, IReadOnlyList<ProcessOutputInfo> processOutputInfoList)
    {
        ExitCode = exitCode;
        ProcessOutputInfoList = processOutputInfoList;
    }

    public bool Success => ExitCode == 0;
    public int ExitCode { get; }

    public string OutputMessage
    {
        get
        {
            return string.Join(Environment.NewLine,
                ProcessOutputInfoList.Select(processOutputInfo => processOutputInfo.Message));
        }
    }
    public IReadOnlyList<ProcessOutputInfo> ProcessOutputInfoList { get; }

    public void Deconstruct(out bool success, out string message)
    {
        success = Success;
        message = OutputMessage;
    }
}

class ProcessCommand
{
    public static ProcessResult ExecuteCommand(string exeName, string arguments,
        string workingDirectory = "", Action<ProcessOutputInfo>? onReceivedOutput = null)
    {
        var processStartInfo = new ProcessStartInfo
        {
            WorkingDirectory = workingDirectory,
            FileName = exeName,

            Arguments = arguments,
            // 设置为 true 那么不会将输出内容，输出内容到当前控制台
            // 设置为 false 且 RedirectStandardOutput 为 false 那么对方控制台内容将会输出到当前控制台
            CreateNoWindow = true,
            UseShellExecute = false,
            // 期望拿到输出信息，需要设置为 true 的值
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // 编码？这就不设置了，如果还需要配置编码等，那就自己业务实现好了
            //StandardOutputEncoding = Encoding.UTF8
        };

        var processOutputInfoList = new List<ProcessOutputInfo>();

        var process = new Process();
        process.StartInfo = processStartInfo;
        // 等待调用 BeginOutputReadLine 方法，才能收到事件
        process.OutputDataReceived += (sender, args) =>
        {
            var outputInfo = new ProcessOutputInfo(OutputType.StandardOutput, args.Data);
            onReceivedOutput?.Invoke(outputInfo);

            processOutputInfoList.Add(outputInfo);
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            var outputInfo = new ProcessOutputInfo(OutputType.ErrorOutput, args.Data);
            onReceivedOutput?.Invoke(outputInfo);

            processOutputInfoList.Add(outputInfo);
        };

        //// 让 Exit 事件触发
        //process.EnableRaisingEvents = true;

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();
        var exitCode = 0;
        try
        {
            Debug.Assert(process.HasExited);
            exitCode = process.ExitCode;
        }
        catch (Exception)
        {
            // 也许有些进程拿不到
        }
        processOutputInfoList.TrimExcess();
        return new ProcessResult(exitCode, processOutputInfoList);
    }
}