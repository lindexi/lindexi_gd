using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个 人类语言文化 的字符信息，包括字符和对应的字符属性
/// </summary>
/// 还没考虑好，先不立刻开放出来
file interface ICharData
{
    /// <summary>
    /// 字符对象
    /// </summary>
    ICharObject CharObject { get; }

    /// <summary>
    /// 文本字符属性
    /// </summary>
    IReadOnlyRunProperty RunProperty { get; }
}

/// <summary>
/// 布局过程中的字符数据
/// </summary>
/// 还没考虑好，先不立刻开放出来
file interface ILayoutCharData : ICharData
{
    /// <summary>
    /// 字符信息，包括尺寸等信息
    /// </summary>
    CharDataInfo CharDataInfo { get; }

    /// <summary>
    /// 获取当前字符的左上角坐标，坐标相对于文本框。此属性必须是在布局完成之后才能获取
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    TextPoint GetStartPoint();

    /// <summary>
    /// 获取字符的布局范围
    /// </summary>
    /// <returns></returns>
    TextRect GetBounds();
}

/// <summary>
/// 表示一个 人类语言文化 的字符信息，包括字符和对应的字符属性
/// </summary>
public sealed class CharData : ICharData, ILayoutCharData
{
    /// <summary>
    /// 创建字符信息
    /// </summary>
    /// <param name="charObject"></param>
    /// <param name="runProperty"></param>
    public CharData(ICharObject charObject, IReadOnlyRunProperty runProperty)
    {
        CharObject = charObject;
        RunProperty = runProperty;
    }

    /// <summary>
    /// 是否一个表示换行的字符
    /// </summary>
    public bool IsLineBreakCharData => ReferenceEquals(CharObject, LineBreakCharObject.Instance);

    /// <summary>
    /// 字符对象
    /// </summary>
    public ICharObject CharObject { get; }

    /// <summary>
    /// 文本字符属性
    /// </summary>
    public IReadOnlyRunProperty RunProperty { get; private set; }

    /// <summary>
    /// 不安全的方式修改字符属性
    /// </summary>
    /// <param name="runProperty"></param>
    internal void DangerousChangeRunProperty(IReadOnlyRunProperty runProperty)
    {
        RunProperty = runProperty;
        ClearCharDataInfo();
    }

    internal CharLayoutData? CharLayoutData { set; get; } // ~~考虑将 CharLayoutData 作为结构体，直接存放在类里面，避免多余地创建~~ 尝试的 COMMIT: 4e4c9b2f1531afbe9d11abdd2de87b07c0319fbd 失败原因是 尝试将 CharLayoutData 替换为结构体，但是实际测试过程中会存在许多次结构体拷贝动作，会拖慢性能。换成结构体的 CharDataLayoutInfo 有 6 个属性，频繁只获取某个属性时，会导致 6 个属性都需要拷贝，其性能确实比较差

    /// <summary>
    /// 获取当前字符的左上角坐标，坐标相对于文本框。此属性必须是在布局完成之后才能获取
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public TextPoint GetStartPoint()
    {
        if (CharLayoutData is null)
        {
            throw new InvalidOperationException($"禁止在加入到段落之前获取");
        }

        if (CharLayoutData.CurrentLine is null)
        {
            throw new InvalidOperationException($"禁止在开始布局之前获取");
        }

        if (CharLayoutData.IsInvalidVersion())
        {
            throw new InvalidOperationException($"字符数据已失效");
        }

        var textPoint = CharLayoutData.CharLineStartPoint.ToDocumentPoint(CharLayoutData.CurrentLine);

        return textPoint.ToCurrentArrangingTypePoint();
    }

    /// <summary>
    /// 设置当前字符的行内左上角坐标
    /// </summary>
    /// <param name="point"></param>
    /// <exception cref="InvalidOperationException"></exception>
    /// 这是文本排版布局的核心方法，通过此方法即可设置每个字符的位置
    [MemberNotNull(nameof(CharLayoutData))]
    internal void SetLayoutCharLineStartPoint(TextPointInLineCoordinateSystem point/*, TextPoint baselineStartPoint*/)
    {
        if (CharLayoutData is null)
        {
            throw new InvalidOperationException("禁止在加入到段落之前设置字符的起始点信息");
        }

        CharLayoutData.CharLineStartPoint = point;
        //CharLayoutData.BaselineStartPoint = baselineStartPoint;

        IsSetStartPointInDebugMode = true;
    }

    // 原本以为直接设置给基线即可，后续发现这样排版依然是无法对齐的，需要获取明确的左上角坐标。然后在渲染层自行叠加基线
    //public TextPoint BaselineStartPoint => CharLayoutData!.BaselineStartPoint;

    /// <summary>
    /// 是否已经设置了此字符的起始（左上角）坐标。这是一个调试属性，仅调试下有用
    /// </summary>
    public bool IsSetStartPointInDebugMode { set; get; }

    /// <summary>
    /// 设置字符信息
    /// </summary>
    /// <param name="charDataInfo"></param>
    internal void SetCharDataInfo(in CharDataInfo charDataInfo)
    {
        if (charDataInfo.IsInvalid)
        {
            throw new ArgumentException("禁止传入无效的字符信息", nameof(charDataInfo));
        }

        if (!CharDataInfo.IsInvalid)
        {
            throw new InvalidOperationException($"禁止重复给 {nameof(CharDataInfo)} 字符信息赋值");
        }

        CharDataInfo = charDataInfo;
    }

    /// <summary>
    /// 字符信息，包括尺寸等信息
    /// </summary>
    public CharDataInfo CharDataInfo { get; private set; } = CharDataInfo.Invalid;

    /// <summary>
    /// 清除字符数据
    /// </summary>
    internal void ClearCharDataInfo()
    {
        CharDataInfo = CharDataInfo.Invalid;
    }

    /// <summary>
    /// 基线，相对于字符的左上角，字符坐标系。即无论这个字符放在哪一行哪一段，这个字符的基线都是一样的
    /// </summary>
    public double Baseline => CharDataInfo.Baseline;

    /// <summary>
    /// FrameSize 尺寸，即字外框尺寸。文字外框尺寸
    /// </summary>
    /// 尺寸是可以复用的
    public TextSize Size => CharDataInfo.FrameSize;

    /// <summary>
    /// Character Face Size 字面尺寸，字墨尺寸，字墨大小，字墨量。文字的字身框中，字图实际分布的空间的尺寸。小于等于 <see cref="Size"/> 尺寸
    /// </summary>
    public TextSize FaceSize => CharDataInfo.FaceSize;

    /// <summary>
    /// 是否字符属性是无效的
    /// </summary>
    public bool IsInvalidCharDataInfo => CharDataInfo.IsInvalid;

    /// <summary>
    /// 获取字符的布局范围
    /// </summary>
    /// <returns></returns>
    public TextRect GetBounds() => new TextRect(GetStartPoint(), Size);

    /// <summary>
    /// 调试下的判断逻辑
    /// </summary>
    /// <exception cref="TextEditorDebugException"></exception>
    internal void DebugVerify()
    {
        if (CharLayoutData != null)
        {
            if (!ReferenceEquals(CharLayoutData.CharData, this))
            {
                throw new TextEditorDebugException($"此 CharData 存放的渲染数据对应的字符，不是当前的 CharData 数据");
            }
        }
    }

    /// <summary>
    /// 用于输出调试信息
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        DebugVerify();

        if (IsLineBreakCharData) return "\\r\\n";

        return $"'{CharObject}' {CharLayoutData?.CharLineStartPoint} {(!IsInvalidCharDataInfo ? $"W:{Size.Width:0.###} H:{Size.Height:0.###}" : "")}";
    }
}