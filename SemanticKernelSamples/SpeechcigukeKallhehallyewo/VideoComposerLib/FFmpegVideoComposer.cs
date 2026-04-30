namespace VideoComposerLib;

public class FFmpegVideoComposer : IAsyncDisposable
{
    private readonly FileInfo _ffmpegExe;
    private readonly VideoEncodeSettings _encodeSettings;
    private readonly DirectoryInfo _workingDirectory;
    private readonly LogHandler? _logHandler;
    private readonly DirectoryInfo _tempDirectory;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ffmpegExe">FFmpeg可执行文件路径（强类型，避免无效路径）</param>
    /// <param name="encodeSettings">视频编码配置，不传则用默认值</param>
    /// <param name="workingDirectory">工作目录，不传则使用系统临时目录</param>
    /// <param name="logHandler">日志回调，上层可自定义日志输出方式</param>
    /// <exception cref="FileNotFoundException">FFmpeg文件不存在时抛出</exception>
    public FFmpegVideoComposer(
        FileInfo ffmpegExe,
        VideoEncodeSettings? encodeSettings = null,
        DirectoryInfo? workingDirectory = null,
        LogHandler? logHandler = null)
    {
        // 前置校验，提前拦截无效参数
        if (!ffmpegExe.Exists)
            throw new FileNotFoundException("FFmpeg可执行文件不存在", ffmpegExe.FullName);

        _ffmpegExe = ffmpegExe;
        _encodeSettings = encodeSettings ?? new VideoEncodeSettings();
        _workingDirectory = workingDirectory ?? new DirectoryInfo(Path.GetTempPath());
        _logHandler = logHandler;

        // 创建临时工作目录，用Path.GetRandomFileName生成随机名
        _tempDirectory = new DirectoryInfo(Path.Join(_workingDirectory.FullName, Path.GetRandomFileName()));
        _tempDirectory.Create();
        Log(VideoComposerLogLevel.Debug, $"临时工作目录创建完成：{_tempDirectory.FullName}");
    }

    /// <summary>
    /// 合成视频核心方法（全异步）
    /// </summary>
    /// <param name="segments">按播放顺序排列的视频分段列表</param>
    /// <param name="outputFile">最终输出视频文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否合成成功</returns>
    /// <exception cref="ArgumentNullException">参数为空时抛出</exception>
    /// <exception cref="ArgumentException">无效参数时抛出</exception>
    public async Task<bool> ComposeAsync(
        IReadOnlyList<VideoSegment> segments,
        FileInfo outputFile,
        CancellationToken cancellationToken = default)
    {
        // 前置校验
        if (segments == null) throw new ArgumentNullException(nameof(segments));
        if (segments.Count == 0) throw new ArgumentException("分段列表不能为空", nameof(segments));
        if (outputFile == null) throw new ArgumentNullException(nameof(outputFile));

        try
        {
            // 1. 生成每个分段的临时视频
            List<FileInfo> segmentVideos = new List<FileInfo>();
            for (int i = 0; i < segments.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var segment = segments[i];
                Log(VideoComposerLogLevel.Info, $"开始处理第{i + 1}/{segments.Count}个分段");

                var segmentVideo = await GenerateSegmentVideoAsync(segment, i, cancellationToken);
                if (segmentVideo == null)
                {
                    Log(VideoComposerLogLevel.Error, $"第{i + 1}个分段生成失败");
                    return false;
                }

                segmentVideos.Add(segmentVideo);
            }

            // 2. 拼接所有分段为最终视频
            Log(VideoComposerLogLevel.Info, "开始拼接所有分段为最终视频");
            bool concatSuccess = await ConcatVideosAsync(segmentVideos, outputFile, cancellationToken);
            if (!concatSuccess)
            {
                Log(VideoComposerLogLevel.Error, "视频拼接失败");
                return false;
            }

            Log(VideoComposerLogLevel.Info, $"视频合成完成，输出路径：{outputFile.FullName}");
            return true;
        }
        catch (OperationCanceledException)
        {
            Log(VideoComposerLogLevel.Warning, "合成任务被取消");
            throw;
        }
        catch (Exception ex)
        {
            Log(VideoComposerLogLevel.Error, $"合成过程发生异常：{ex.Message}");
            return false;
        }
    }

    #region 内部实现方法

    /// <summary>
    /// 生成单个分段的临时视频
    /// </summary>
    private async Task<FileInfo?> GenerateSegmentVideoAsync(VideoSegment segment, int index,
        CancellationToken cancellationToken)
    {
        // 生成临时音频拼接列表
        var audioListFile =
            new FileInfo(Path.Join(_tempDirectory.FullName, $"audio_{index}_{Path.GetRandomFileName()}.txt"));
        var sb = new List<string>();
        foreach (var audio in segment.AudioFiles)
        {
            if (!audio.Exists)
            {
                Log(VideoComposerLogLevel.Error, $"音频文件不存在：{audio.FullName}");
                return null;
            }

            // 路径转义处理特殊字符
            string escapedPath = audio.FullName.Replace("'", @"'\''");
            sb.Add($"file '{escapedPath}'");
        }

        await File.WriteAllLinesAsync(audioListFile.FullName, sb, cancellationToken);

        // 生成分段视频临时文件
        var segmentVideoFile =
            new FileInfo(Path.Join(_tempDirectory.FullName, $"seg_{index}_{Path.GetRandomFileName()}.mp4"));

        // 构造FFmpeg参数
        var args = $"-loop 1 -r {_encodeSettings.Fps} -i \"{segment.ImageFile.FullName}\" " +
                   $"-f concat -safe 0 -i \"{audioListFile.FullName}\" " +
                   $"-vf \"scale={_encodeSettings.Width}:{_encodeSettings.Height}:force_original_aspect_ratio=decrease,pad={_encodeSettings.Width}:{_encodeSettings.Height}:(ow-iw)/2:(oh-ih)/2:black\" " +
                   $"-c:v libx264 -b:v {_encodeSettings.VideoBitrate} -c:a aac -b:a {_encodeSettings.AudioBitrate} -pix_fmt yuv420p " +
                   $"-shortest -y \"{segmentVideoFile.FullName}\"";

        Log(VideoComposerLogLevel.Debug, $"分段{index}执行命令：ffmpeg {args}");
        int exitCode = await RunFFmpegCommandAsync(args, cancellationToken);
        if (exitCode != 0) return null;

        return segmentVideoFile.Exists ? segmentVideoFile : null;
    }

    /// <summary>
    /// 拼接多个视频为一个
    /// </summary>
    private async Task<bool> ConcatVideosAsync(IReadOnlyList<FileInfo> segmentVideos, FileInfo outputFile,
        CancellationToken cancellationToken)
    {
        // 生成拼接列表文件
        var concatListFile = new FileInfo(Path.Join(_tempDirectory.FullName, $"concat_{Path.GetRandomFileName()}.txt"));
        var sb = new List<string>();
        foreach (var seg in segmentVideos)
        {
            string escapedPath = seg.FullName.Replace("'", @"'\''");
            sb.Add($"file '{escapedPath}'");
        }

        await File.WriteAllLinesAsync(concatListFile.FullName, sb, cancellationToken);

        // 拼接命令：直接复制编码，速度极快无画质损失
        var args = $"-f concat -safe 0 -i \"{concatListFile.FullName}\" -c copy -y \"{outputFile.FullName}\"";
        Log(VideoComposerLogLevel.Debug, $"拼接执行命令：ffmpeg {args}");

        int exitCode = await RunFFmpegCommandAsync(args, cancellationToken);
        return exitCode == 0;
    }

    /// <summary>
    /// 异步执行FFmpeg命令
    /// </summary>
    private async Task<int> RunFFmpegCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = _ffmpegExe.FullName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            StandardOutputEncoding = System.Text.Encoding.UTF8
        };

        using var process = new System.Diagnostics.Process();
        process.StartInfo = processStartInfo;
        process.EnableRaisingEvents = true;
        try
        {
            process.Start();

            // 异步读取输出避免死锁
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            // 异步等待进程退出，支持取消
            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            if (!string.IsNullOrWhiteSpace(output))
                Log(VideoComposerLogLevel.Debug, $"FFmpeg输出：{output}");
            if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
                Log(VideoComposerLogLevel.Error, $"FFmpeg错误：{error}");

            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill();
            throw;
        }
    }

    /// <summary>
    /// 日志输出
    /// </summary>
    private void Log(VideoComposerLogLevel level, string message)
    {
        _logHandler?.Invoke(level, message);
    }

    #endregion

    #region 资源清理

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // 清理临时目录
        try
        {
            if (_tempDirectory.Exists)
            {
                Log(VideoComposerLogLevel.Debug, $"清理临时目录：{_tempDirectory.FullName}");
                _tempDirectory.Delete(true);
            }
        }
        catch (Exception ex)
        {
            Log(VideoComposerLogLevel.Warning, $"临时目录清理失败：{ex.Message}");
        }

        GC.SuppressFinalize(this);
        await ValueTask.CompletedTask;
    }

    #endregion
}