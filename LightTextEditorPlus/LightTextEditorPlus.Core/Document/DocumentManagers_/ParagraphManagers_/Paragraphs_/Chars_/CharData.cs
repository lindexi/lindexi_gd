using System;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个 人类语言文化 的字符信息
/// <para>
/// 有一些字符，如表情，是需要使用两个 char 表示。这里当成一个处理
/// </para>
/// </summary>
public class CharData
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
    public IReadOnlyRunProperty RunProperty { get; }

    internal CharLayoutData? CharLayoutData { set; get; }

    /// <summary>
    /// 获取当前字符的左上角坐标，坐标相对于文本框。此属性必须是在布局完成之后才能获取
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Point GetStartPoint()
    {
        if (CharLayoutData is null || CharLayoutData.IsInvalidVersion())
        {
            throw new InvalidOperationException($"禁止在开始布局之前获取");
        }

        return CharLayoutData.StartPoint;
    }

    /// <summary>
    /// 设置当前字符的左上角坐标，坐标相对于文本框
    /// </summary>
    /// <param name="point"></param>
    /// <exception cref="InvalidOperationException"></exception>
    internal void SetStartPoint(Point point)
    {
        if (CharLayoutData is null)
        {
            throw new InvalidOperationException($"禁止在开始布局之前设置");
        }

        CharLayoutData.StartPoint = point;

        IsSetStartPointInDebugMode = true;
    }

    /// <summary>
    /// 是否已经设置了此字符的起始（左上角）坐标。这是一个调试属性，仅调试下有用
    /// </summary>
    public bool IsSetStartPointInDebugMode { set; get; }

    /// <summary>
    /// 尺寸
    /// </summary>
    /// 尺寸是可以复用的
    public Size? Size
    {
        internal set
        {
            if (_size != null)
            {
                throw new InvalidOperationException($"禁止重复给尺寸赋值");
            }

            _size = value;
        }
        get => _size;
    }

    private Size? _size;

    /// <summary>
    /// 获取字符的布局范围
    /// </summary>
    /// <returns></returns>
    public Rect GetBounds() => new Rect(GetStartPoint(), Size!.Value);

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

        return $"'{CharObject}' {(CharLayoutData != null?$"X:{CharLayoutData.StartPoint.X:0.00} Y:{CharLayoutData.StartPoint.Y:0.00}":"")} {(Size!=null?$"W:{Size.Value.Width:0.00} H:{Size.Value.Height:0.00}":"")}";
    }
}