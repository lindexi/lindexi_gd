namespace SkiaInkCore.Interactives;

record InkingInputProcessorSettings
{
    // 不好实现，存在漏洞是首次收到 Move 的情况，此时不仅需要补 Down 还需要补 Start 的情况
    ///// <summary>
    ///// 对于丢失了 Down 的触摸，是否启用。如启用，则会自动补 Down 事件。默认 false 即丢点
    ///// </summary>
    //public bool EnableLostDownTouch { init; get; } = false;

    public bool EnableMultiTouch { init; get; } = true;

    public static readonly InkingInputProcessorSettings Default = new InkingInputProcessorSettings();
}