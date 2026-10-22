namespace MediaFoundation.Controls;

/// <summary>视频呈现方式；仅可在 Open 前或 Close 后更改。</summary>
public enum VideoRenderingMode
{
    /// <summary>MFPlay 子 HWND；通常开销较低，但存在 HWND 空域问题，不能参与常规 WPF 合成。</summary>
    Hwnd,
    /// <summary>Media Engine 共享表面经 D3DImage 参与 WPF 合成，无子 HWND 空域；增加互操作和合成开销，实际性能取决于设备及媒体。</summary>
    D3DImage
}
