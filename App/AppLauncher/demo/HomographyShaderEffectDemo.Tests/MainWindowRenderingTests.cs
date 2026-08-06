using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Xunit;

namespace HomographyShaderEffectDemo.Tests;

public class MainWindowRenderingTests
{
    [Fact]
    public void WhenWindowIsRenderedThenPerspectivePreviewIsSavedAsPng()
    {
        RunInSta(() =>
        {
            using var applicationScope = new WpfApplicationScope();
            var window = new MainWindow
            {
                Width = 1080,
                Height = 720,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false
            };

            try
            {
                window.Show();
                FlushDispatcher();

                var screenshot = Render(window);
                var screenshotPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "TestArtifacts",
                    "HomographyShaderEffectDemo.png");
                SavePng(screenshot, screenshotPath);

                Assert.InRange(screenshot.PixelWidth, 1000, 1080);
                Assert.InRange(screenshot.PixelHeight, 650, 720);
                Assert.True(ContainsOpaquePixels(screenshot));
                Assert.True(ContainsYellowControlPoint(screenshot));
                Assert.True(ContainsCheckerboardNearViewportCorner(screenshot));
                Assert.True(File.Exists(screenshotPath), $"未生成截图：{screenshotPath}");
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static RenderTargetBitmap Render(Visual visual)
    {
        var bounds = VisualTreeHelper.GetDescendantBounds(visual);
        var width = checked((int)Math.Ceiling(bounds.Width));
        var height = checked((int)Math.Ceiling(bounds.Height));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void SavePng(BitmapSource bitmap, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static bool ContainsOpaquePixels(BitmapSource bitmap)
    {
        var pixels = CopyPixels(bitmap);
        for (var index = 3; index < pixels.Length; index += 4)
        {
            if (pixels[index] == byte.MaxValue)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsYellowControlPoint(BitmapSource bitmap)
    {
        var pixels = CopyPixels(bitmap);
        for (var index = 0; index < pixels.Length; index += 4)
        {
            var blue = pixels[index];
            var green = pixels[index + 1];
            var red = pixels[index + 2];
            var alpha = pixels[index + 3];
            if (alpha > 240 && red > 220 && green > 130 && blue < 40)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsCheckerboardNearViewportCorner(BitmapSource bitmap)
    {
        var pixels = CopyPixels(bitmap);
        var stride = bitmap.PixelWidth * 4;
        var startX = bitmap.PixelWidth / 12;
        var endX = bitmap.PixelWidth / 3;
        var startY = bitmap.PixelHeight / 4;
        var endY = bitmap.PixelHeight * 3 / 4;

        for (var y = startY; y < endY; y++)
        {
            for (var x = startX; x < endX; x++)
            {
                var index = (y * stride) + (x * 4);
                var blue = pixels[index];
                var green = pixels[index + 1];
                var red = pixels[index + 2];
                if (Math.Abs(red - green) < 3 && Math.Abs(green - blue) < 3 && red is >= 30 and <= 60)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static byte[] CopyPixels(BitmapSource bitmap)
    {
        var stride = bitmap.PixelWidth * 4;
        var pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);
        return pixels;
    }

    private static void FlushDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception currentException)
            {
                exception = currentException;
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private sealed class WpfApplicationScope : IDisposable
    {
        private readonly Application? _application;

        public WpfApplicationScope()
        {
            if (Application.Current is null)
            {
                _application = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }
        }

        public void Dispose()
        {
            _application?.Shutdown();
        }
    }
}
