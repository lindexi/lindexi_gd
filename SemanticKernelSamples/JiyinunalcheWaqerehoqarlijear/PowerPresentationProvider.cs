using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JiyinunalcheWaqerehoqarlijear;

internal sealed class PowerPresentationProvider : IDisposable
{
    private const string PasswordMark = "::PASSWORD::";
    private const double ExportDpi = 192;
    private FileInfo? _tempPresentationFile;
    private bool _disposedValue;

    public Application? Application { get; private set; }

    public Presentation? Presentation { get; private set; }

    /// <summary>
    /// 默认的打开超时时间，默认是 <see cref="Timeout.InfiniteTimeSpan"/> 的值
    /// </summary>
    public static TimeSpan DefaultOpenPowerPresentationTimeout { set; get; } = Timeout.InfiniteTimeSpan;

    /// <summary>
    /// 打开文件
    /// </summary>
    /// <param name="filePath">PPT文件地址</param>
    /// <param name="shouldCopyFile">是否需要将原文件复制到临时文件</param>
    public void OpenPowerPresentation(FileInfo filePath, bool shouldCopyFile = true)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!filePath.Exists)
        {
            throw new FileNotFoundException("未找到指定的 PowerPoint 文件。", filePath.FullName);
        }

        var openedFile = shouldCopyFile ? CopyTempPpt(filePath) : filePath;
        _tempPresentationFile = shouldCopyFile ? openedFile : null;

        var application = new Application();
        application.DisplayAlerts = PpAlertLevel.ppAlertsNone;
        //application.Visible = MsoTriState.msoFalse;

        try
        {
            Application = application;
            Presentation = application.Presentations.Open(openedFile.FullName + PasswordMark, MsoTriState.msoTrue,
                MsoTriState.msoFalse,
                MsoTriState.msoFalse);
        }
        catch
        {
            try
            {
                application.Quit();
                Marshal.ReleaseComObject(application);
            }
            catch (COMException ex)
            {
                Debug.WriteLine(ex);
            }

            Application = null;
            TryKillPowerPointProcess();
            throw;
        }
    }

    public IReadOnlyList<string> ExportSlideImages(string outputFolder, IProgress<string>? statusReporter, CancellationToken cancellationToken)
    {
        var presentation = Presentation ?? throw new InvalidOperationException("PowerPoint 文件尚未打开。");
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            throw new InvalidOperationException("输出目录不能为空。");
        }

        Directory.CreateDirectory(outputFolder);

        Slides? slides = null;
        try
        {
            slides = presentation.Slides;
            var slideCount = slides.Count;
            var slideWidth = Math.Max(1, (int) Math.Ceiling(presentation.PageSetup.SlideWidth * ExportDpi / 72d));
            var slideHeight = Math.Max(1, (int) Math.Ceiling(presentation.PageSetup.SlideHeight * ExportDpi / 72d));
            var slideImagePaths = new List<string>(slideCount);

            for (var slideIndex = 1; slideIndex <= slideCount; slideIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                statusReporter?.Report($"正在导出原始页面截图 ({slideIndex}/{slideCount})...");

                Slide? slide = null;
                try
                {
                    slide = slides[slideIndex];
                    var slideImagePath = Path.Join(outputFolder, $"{slideIndex:D3}.png");

                    // 换一个路径，防止写入失败
                    var tempImagePath = Path.Join(Path.GetTempPath(), $"{Path.GetRandomFileName()}.png");

                    slide.Export(tempImagePath, "PNG", slideWidth, slideHeight);
                    File.Copy(tempImagePath, slideImagePath, true);
                    File.Delete(tempImagePath);

                    slideImagePaths.Add(slideImagePath);
                }
                finally
                {
                    if (slide is not null)
                    {
                        Marshal.ReleaseComObject(slide);
                    }
                }
            }

            return slideImagePaths;
        }
        finally
        {
            if (slides is not null)
            {
                Marshal.ReleaseComObject(slides);
            }
        }
    }

    public static Task<IReadOnlyList<string>> ExportSlideImagesAsync(
        FileInfo filePath,
        string outputFolder,
        IProgress<string>? statusReporter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        return RunOnStaThreadAsync(() =>
        {
            using var provider = new PowerPresentationProvider();
            provider.OpenPowerPresentation(filePath);
            return provider.ExportSlideImages(outputFolder, statusReporter, cancellationToken);
        }, cancellationToken);
    }

    private static Task<T> RunOnStaThreadAsync<T>(Func<T> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        var taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = action();
                taskCompletionSource.TrySetResult(result);
            }
            catch (OperationCanceledException ex)
            {
                taskCompletionSource.TrySetCanceled(ex.CancellationToken.CanBeCanceled ? ex.CancellationToken : cancellationToken);
            }
            catch (Exception ex)
            {
                TryKillPowerPointProcess();
                taskCompletionSource.TrySetException(ex);
            }
        });

        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return taskCompletionSource.Task;
    }

    private static void TryKillPowerPointProcess()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("powerpnt"))
            {
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine(ex);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine(ex);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    /// <summary>
    /// 拷贝一份临时Ppt 为什么需要复制，防止用户打开的时候也使用 WPS 打开
    /// </summary>
    /// <param name="pptFilePath"></param>
    /// <returns></returns>
    private static FileInfo CopyTempPpt(FileInfo pptFilePath)
    {
        var tempPptPath = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + pptFilePath.Extension));
        pptFilePath.CopyTo(tempPptPath.FullName);
        return tempPptPath;
    }

    #region IDisposable Support

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        var presentation = Presentation;
        var application = Application;
        Presentation = null;
        Application = null;

        try
        {
            if (presentation is not null)
            {
                presentation.Close();
                Marshal.ReleaseComObject(presentation);
            }

            if (application is not null)
            {
                application.Quit();
                Marshal.ReleaseComObject(application);
            }
        }
        catch (COMException ex)
        {
            Debug.WriteLine(ex);
        }
        catch (InvalidComObjectException ex)
        {
            Debug.WriteLine(ex);
        }

        if (_tempPresentationFile is not null)
        {
            try
            {
                if (_tempPresentationFile.Exists)
                {
                    _tempPresentationFile.Delete();
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine(ex);
            }

            _tempPresentationFile = null;
        }

        _disposedValue = true;
    }

    ~PowerPresentationProvider()
    {
        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中
        Dispose(false);
    }

    // 添加此代码以正确实现可处置模式。
    public void Dispose()
    {
        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中
        Dispose(true);
        // 使用了终结器，需要取消注释以下行
        GC.SuppressFinalize(this);
    }

    #endregion
}