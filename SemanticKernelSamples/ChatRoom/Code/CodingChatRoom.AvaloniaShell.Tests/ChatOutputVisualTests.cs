using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ChatOutputVisualTests
{
    [AssemblyInitialize]
    public static void InitializeAvalonia(TestContext _)
    {
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false,
            })
            .WithInterFont()
            .SetupWithoutStarting();
    }

    [TestMethod]
    public void FocusedChatOutputShouldNotRenderBlueOuterBorder()
    {
        var textBox = new TextBox
        {
            Classes = { "ChatOutput" },
            Text = "选择这段聊天消息时，只应显示柔和的文本选区，不应显示蓝色 TextBox 外框。",
            Width = 680,
        };
        var bubble = new Border
        {
            Classes = { "MessageBubbleAssistant" },
            Child = textBox,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(24),
        };
        var window = new Window
        {
            Width = 760,
            Height = 180,
            Background = Brushes.White,
            Content = bubble,
        };

        window.Show();
        textBox.Focus();
        textBox.SelectionStart = 0;
        textBox.SelectionEnd = textBox.Text?.Length ?? 0;

        using WriteableBitmap bitmap = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("无法捕获聊天消息视觉测试截图。");
        string screenshotPath = GetScreenshotPath();
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
        bitmap.Save(screenshotPath);

        Assert.IsFalse(ContainsBlueFocusBorder(bitmap, textBox, window));

        window.Close();
    }

    private static bool ContainsBlueFocusBorder(WriteableBitmap bitmap, TextBox textBox, Window window)
    {
        Point origin = textBox.TranslatePoint(default, window)
            ?? throw new InvalidOperationException("无法获取聊天文本框的截图坐标。");
        PixelRect bounds = PixelRect.FromRect(new Rect(origin, textBox.Bounds.Size), 1);
        using ILockedFramebuffer framebuffer = bitmap.Lock();
        unsafe
        {
            byte* address = (byte*)framebuffer.Address;
            for (int y = bounds.Y; y < bounds.Bottom; y++)
            {
                if (IsFocusBlue(address, framebuffer.RowBytes, bounds.X, y)
                    || IsFocusBlue(address, framebuffer.RowBytes, bounds.Right - 1, y))
                {
                    return true;
                }
            }

            for (int x = bounds.X; x < bounds.Right; x++)
            {
                if (IsFocusBlue(address, framebuffer.RowBytes, x, bounds.Y)
                    || IsFocusBlue(address, framebuffer.RowBytes, x, bounds.Bottom - 1))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static unsafe bool IsFocusBlue(byte* address, int rowBytes, int x, int y)
    {
        byte* pixel = address + (y * rowBytes) + (x * 4);
        byte blue = pixel[0];
        byte green = pixel[1];
        byte red = pixel[2];
        return blue >= 180 && blue > green + 20 && green > red + 20;
    }

    private static string GetScreenshotPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "ChatOutputFocused.png");
    }

    [TestMethod]
    public void SelectedUserMessageTextShouldHaveHighContrast()
    {
        var textBox = new TextBox
        {
            Classes = { "ChatOutput", "UserChatOutput" },
            Text = "总结对话",
        };
        var bubble = new Border
        {
            Classes = { "MessageBubbleUser" },
            Child = textBox,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(24),
        };
        var window = new Window
        {
            Width = 340,
            Height = 180,
            Background = Brushes.White,
            Content = bubble,
        };

        window.Show();
        textBox.Focus();
        textBox.SelectionStart = 2;
        textBox.SelectionEnd = 4;

        using WriteableBitmap bitmap = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("无法捕获用户消息选区视觉测试截图。");
        bitmap.Save(Path.Combine(AppContext.BaseDirectory, "UserChatOutputSelection.png"));

        var selectionBrush = textBox.SelectionBrush as ISolidColorBrush;
        var selectionForegroundBrush = textBox.SelectionForegroundBrush as ISolidColorBrush;
        Assert.AreEqual(Color.Parse("#1976D2"), selectionBrush?.Color);
        Assert.AreEqual(Colors.White, selectionForegroundBrush?.Color);

        window.Close();
    }
}
