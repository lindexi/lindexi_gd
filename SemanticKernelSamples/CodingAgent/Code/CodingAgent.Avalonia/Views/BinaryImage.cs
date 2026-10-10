using System;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace CodingChatRoom.AvaloniaShell.Views;

/// <summary>
/// 显示来自 <see cref="BinaryData"/> 的图片内容。
/// </summary>
public sealed class BinaryImage : Control
{
    /// <summary>
    /// 定义图片数据属性。
    /// </summary>
    public static readonly StyledProperty<BinaryData?> DataProperty =
        AvaloniaProperty.Register<BinaryImage, BinaryData?>(nameof(Data));

    private Bitmap? _bitmap;

    static BinaryImage()
    {
        AffectsRender<BinaryImage>(DataProperty);
    }

    /// <summary>
    /// 获取或设置图片二进制数据。
    /// </summary>
    public BinaryData? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DataProperty)
        {
            ReloadBitmap(change.NewValue as BinaryData);
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_bitmap is null)
        {
            ReloadBitmap(Data);
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _bitmap?.Dispose();
        _bitmap = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_bitmap is null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        PixelSize pixelSize = _bitmap.PixelSize;
        double scale = Math.Min(Bounds.Width / pixelSize.Width, Bounds.Height / pixelSize.Height);
        double width = pixelSize.Width * scale;
        double height = pixelSize.Height * scale;
        var destinationRect = new Rect(
            (Bounds.Width - width) / 2,
            (Bounds.Height - height) / 2,
            width,
            height);
        context.DrawImage(
            _bitmap,
            new Rect(0, 0, pixelSize.Width, pixelSize.Height),
            destinationRect);
    }

    private void ReloadBitmap(BinaryData? data)
    {
        _bitmap?.Dispose();
        _bitmap = null;
        if (data is null)
        {
            InvalidateVisual();
            return;
        }

        try
        {
            using var stream = new MemoryStream(data.ToArray(), writable: false);
            _bitmap = new Bitmap(stream);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or IOException)
        {
        }

        InvalidateVisual();
    }
}
