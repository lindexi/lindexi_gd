using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个 人类语言文化 的字符信息，包括字符和对应的字符属性
/// </summary>
public sealed class CharData
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
    }

    internal CharLayoutData? CharLayoutData { set; get; }

    /// <summary>
    /// 获取当前字符的左上角坐标，坐标相对于文本框。此属性必须是在布局完成之后才能获取
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public TextPoint GetStartPoint()
    {
        if (CharLayoutData is null || CharLayoutData.IsInvalidVersion()
            || CharLayoutData.CurrentLine is null)
        {
            throw new InvalidOperationException($"禁止在开始布局之前获取");
        }

        return CharLayoutData.CharLineStartPoint.ToDocumentPoint(CharLayoutData.CurrentLine);
    }

    /// <summary>
    /// 设置当前字符的行内左上角坐标
    /// </summary>
    /// <param name="point"></param>
    /// <exception cref="InvalidOperationException"></exception>
    /// 这是文本排版布局的核心方法，通过此方法即可设置每个字符的位置
    [MemberNotNull(nameof(CharLayoutData))]
    internal void SetLayoutCharLineStartPoint(TextPointInLineCoordinate point/*, TextPoint baselineStartPoint*/)
    {
        if (CharLayoutData is null)
        {
            throw new InvalidOperationException($"禁止在开始布局之前设置");
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
    /// 设置尺寸
    /// </summary>
    /// <param name="textSize"></param>
    /// <param name="baseline">基线，相对于字符的左上角，字符坐标系。即无论这个字符放在哪一行哪一段，这个字符的基线都是一样的</param>
    internal void SetCharDataInfo(TextSize textSize, double baseline)
    {
        Size = textSize;
        Baseline = baseline;
    }

    /// <summary>
    /// 基线，相对于字符的左上角，字符坐标系。即无论这个字符放在哪一行哪一段，这个字符的基线都是一样的
    /// </summary>
    public double Baseline { private set; get; }

    /// <summary>
    /// 尺寸
    /// </summary>
    /// 尺寸是可以复用的
    public TextSize? Size
    {
        private set
        {
            if (_size != null)
            {
                throw new InvalidOperationException($"禁止重复给尺寸赋值");
            }

            _size = value;
        }
        get => _size;
    }

    private TextSize? _size;

    /// <summary>
    /// 获取字符的布局范围
    /// </summary>
    /// <returns></returns>
    public TextRect GetBounds() => new TextRect(GetStartPoint(), Size!.Value);

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

        return $"'{CharObject}' {CharLayoutData?.CharLineStartPoint} {(Size != null ? $"W:{Size.Value.Width:0.00} H:{Size.Value.Height:0.00}" : "")}";
    }
}