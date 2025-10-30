namespace DotNetCampus.InkCanvas.X11InkCanvas.Renders;

/// <summary>
/// 应用的后端渲染上下文
/// </summary>
/// <remarks>默认要求使用 SKColorType.Bgra8888, SKAlphaType.Premul, SKPixelGeometry.BgrHorizontal格式</remarks>
interface IApplicationBackendRenderContext
{
    int PixelWidth { get; }
    int PixelHeight { get; }

    int RowBytes => PixelWidth * 4;

    int PixelBytesLength => RowBytes * PixelHeight;

    nint GetPixelIntPtr();
}