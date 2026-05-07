using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace JawjeleceeYairlubelhearrene;

internal sealed class PowerPresentationProvider : IDisposable
{
    private const string PasswordMark = "::PASSWORD::";
    private const double ExportDpi = 192;
    private System.IO.FileInfo? _tempPresentationFile;
    private bool _disposedValue;

    public Application? Application { get; private set; }

    public Presentation? Presentation { get; private set; }

    public void OpenPowerPresentation(System.IO.FileInfo filePath, bool shouldCopyFile = true)
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

        try
        {
            Application = application;
            Presentation = application.Presentations.Open(openedFile.FullName + PasswordMark, MsoTriState.msoTrue, MsoTriState.msoFalse, MsoTriState.msoFalse);
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
            var slideWidth = Math.Max(1, (int)Math.Ceiling(presentation.PageSetup.SlideWidth * ExportDpi / 72d));
            var slideHeight = Math.Max(1, (int)Math.Ceiling(presentation.PageSetup.SlideHeight * ExportDpi / 72d));
            var slideImagePaths = new List<string>(slideCount);

            for (var slideIndex = 1; slideIndex <= slideCount; slideIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                statusReporter?.Report($"正在导出页面截图 ({slideIndex}/{slideCount})...");

                Slide? slide = null;
                try
                {
                    slide = slides[slideIndex];
                    var slideImagePath = Path.Join(outputFolder, $"{slideIndex:D3}.png");
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

    public static Task<IReadOnlyList<string>> ExportSlideImagesAsync(System.IO.FileInfo filePath, string outputFolder, IProgress<string>? statusReporter, CancellationToken cancellationToken)
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

    private static System.IO.FileInfo CopyTempPpt(System.IO.FileInfo pptFilePath)
    {
        var tempPptPath = new System.IO.FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + pptFilePath.Extension));
        pptFilePath.CopyTo(tempPptPath.FullName);
        return tempPptPath;
    }

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
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
