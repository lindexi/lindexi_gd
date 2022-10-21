//using System.Collections.Generic;
//using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 段内行测量布局结果
/// </summary>
/// <param name="Size">这一行的尺寸</param>
/// <param name="CharCount">这一行使用的 字符 的数量</param>
public readonly record struct WholeLineLayoutResult(Size Size, int CharCount)
{
    // 现在使用字符布局了，不再需要对 Run 进行分割
    ///// <summary>
    ///// 是否最后一个 Run 需要被分割。也就是最后一个 Run 将会跨多行
    ///// </summary>
    //public bool NeedSplitLastRun => LastRunHitIndex > 0;
}