using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 项目符号
/// </summary>
public abstract class TextMarker
{
    public IReadOnlyRunProperty? RunProperty { get; init; }

    /// <summary>
    /// 用于限制程序集外继承的科技
    /// </summary>
    internal abstract void DisableInherit();
}

/// <summary>
/// 无符号项目符号，又称无序项目符号
/// </summary>
public sealed class BulletMarker : TextMarker
{
    public string? MarkerText { get; init; }

    internal override void DisableInherit()
    {
    }
}

/// <summary>
/// 编号项目符号，又称有序项目符号
/// </summary>
public class NumberMarker : TextMarker
{
    /// <summary>
    /// 表示当前缩进级别的数字项目符号起始编号
    /// </summary>
    public int StartAt { get; init; } = 1;

    /// <summary>
    /// 编号项目符号类型
    /// </summary>
    public AutoNumberType AutoNumberType { get; set; } = AutoNumberType.ArabicPeriod;

    /// <summary>
    /// 获取当前级别的编号文本
    /// </summary>
    /// <param name="levelIndex"></param>
    /// <returns></returns>
    public virtual string GetMarkerText(uint levelIndex)
    {
        // todo 填充更多的转换关系
        return GetLowerLatin(levelIndex) + ".";
    }

    internal override void DisableInherit()
    {
    }

    /// <summary>
    /// 根据当前级别获取英文字符串
    /// </summary>
    /// <param name="levelIndex"></param>
    /// <returns></returns>
    protected static string GetLowerLatin(uint levelIndex)
    {
/*
   其对应关系如下：
   0 a.
   1 b.
   2 c.
   3 d.
   4 e.
   ...
   23 x.
   24 y.
   25 z.
   26 aa.
   27 bb.
   28 cc.
   29 dd.
   30 ee.
 */
        const int startAsciiNum = 'a'; //97
        const int aToZCount = 'z' - 'a' + 1;
        int count = (int) levelIndex / aToZCount;
        int index = (int) levelIndex % aToZCount;

        var word = (char) (startAsciiNum + index);
        return string.Concat(new string(word, count + 1));
    }
}