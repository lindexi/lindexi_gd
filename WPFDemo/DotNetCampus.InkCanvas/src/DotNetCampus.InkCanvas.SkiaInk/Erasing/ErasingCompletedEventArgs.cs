using DotNetCampus.Inking;

namespace WejallkachawDadeawejearhuce.Inking.Contexts;

/// <summary>
/// 擦除完成参数
/// </summary>
class SkInkCanvasErasingCompletedEventArgs : EventArgs
{
    public SkInkCanvasErasingCompletedEventArgs(bool isCanceled)
    {
        IsCanceled = isCanceled;
    }

    public SkInkCanvasErasingCompletedEventArgs(IReadOnlyList<SkiaStrokeSynchronizer> originList,
        IReadOnlyList<SkiaStrokeSynchronizer> newList) : this(false)
    {
        OriginList = originList;
        NewList = newList;
    }

    public IReadOnlyList<SkiaStrokeSynchronizer> OriginList { get; } = null!;

    public IReadOnlyList<SkiaStrokeSynchronizer> NewList { get; } = null!;

    /// <summary>
    /// 是否取消
    /// </summary>
    public bool IsCanceled { get; private set; }
}