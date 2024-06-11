using SkiaSharp;

namespace UnoInk.Inking.InkCore;

/// <summary>
/// 笔迹信息 用于静态笔迹层
/// </summary>
public record StrokeCollectionInfo(InkId InkId, SKColor StrokeColor, SKPath? InkStrokePath);
