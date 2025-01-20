using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 更新布局的上下文
/// </summary>
public class UpdateLayoutContext : ICharDataLayoutInfoSetter
{
    internal UpdateLayoutContext(ArrangingLayoutProvider arrangingLayoutProvider)
    {
        ArrangingLayoutProvider = arrangingLayoutProvider;
    }

    internal ArrangingLayoutProvider ArrangingLayoutProvider { get; }

    /// <summary>
    /// 文本编辑器
    /// </summary>
    public TextEditorCore TextEditor => ArrangingLayoutProvider.TextEditor;

    /// <summary>
    /// 是否在调试模式
    /// </summary>
    public bool IsInDebugMode => TextEditor.DebugConfiguration.IsInDebugMode;

    /// <summary>
    /// 记录布局过程的调试信息
    /// </summary>
    /// <param name="message"></param>
    public void RecordDebugLayoutInfo([InterpolatedStringHandlerArgument("")] LayoutDebugMessageInterpolatedStringHandler message)
    {
        if (!message.IsInDebugMode)
        {
            return;
        }

        LayoutDebugMessageList ??= new List<string>();
        LayoutDebugMessageList.Add(message.GetFormattedText());
    }

    /// <summary>
    /// 获取布局调试信息列表
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<string> GetLayoutDebugMessageList()
    {
        if (LayoutDebugMessageList is { } list)
        {
            return list;
        }

        return Array.Empty<string>();
    }

    private List<string>? LayoutDebugMessageList { get; set; }

    /// <summary>
    /// 当前布局是否已经完成
    /// </summary>
    public bool IsCurrentLayoutCompleted { get; private set; }

    internal void SetLayoutCompleted()
    {
        IsCurrentLayoutCompleted = true;
    }

    #region ICharDataLayoutInfoSetter

    /// <inheritdoc />
    public void SetLayoutStartPoint(CharData charData, TextPoint point)
    {
        if (IsCurrentLayoutCompleted)
        {
            throw new InvalidOperationException($"只有在布局过程才能设置 {nameof(charData)} 的布局属性");
        }

        charData.SetLayoutStartPoint(point);
    }

    /// <inheritdoc />
    public void SetCharDataInfo(CharData charData, TextSize textSize, double baseline)
    {
        if (IsCurrentLayoutCompleted)
        {
            throw new InvalidOperationException($"只有在布局过程才能设置 {nameof(charData)} 的布局属性");
        }

        charData.SetCharDataInfo(textSize, baseline);
    }

    #endregion

    #region InterpolatedStringHandler

    /// <summary>
    /// 用于记录布局调试信息的字符串处理器
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct LayoutDebugMessageInterpolatedStringHandler
    {
        /// <summary>
        /// 创建用于记录布局调试信息的字符串处理器
        /// </summary>
        /// <param name="literalLength"></param>
        /// <param name="formattedCount"></param>
        /// <param name="context"></param>
        /// <param name="isEnable"></param>
        public LayoutDebugMessageInterpolatedStringHandler(int literalLength, int formattedCount, UpdateLayoutContext context, out bool isEnable)
        {
            bool isInDebugMode = context.IsInDebugMode;
            isEnable = isInDebugMode;
            if (isInDebugMode)
            {
                _stringBuilder = new StringBuilder();
            }
            IsInDebugMode = isInDebugMode;
        }

        internal bool IsInDebugMode { get; }

        /// <summary>
        /// 添加字面量
        /// </summary>
        /// <param name="s"></param>
        public void AppendLiteral(string s)
        {
            _stringBuilder?.Append(s);
        }

        /// <summary>
        /// 添加格式化字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void AppendFormatted<T>(T value)
        {
            _stringBuilder?.Append(value);
        }

        /// <summary>
        /// 添加格式化字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="format"></param>
        public void AppendFormatted<T>(T value, string format)
        {
            _stringBuilder?.AppendFormat(format, value);
        }

        private readonly StringBuilder? _stringBuilder;

        internal string GetFormattedText() => _stringBuilder?.ToString() ?? string.Empty;
    }

    #endregion
}

/// <summary>
/// 用于设置字符布局信息，这是一个辅助接口，核心只是为了让 <see cref="CharData"/> 不要开放一些方法而已。限定只有在布局的时候才能设置
/// </summary>
public interface ICharDataLayoutInfoSetter
{
    /// <inheritdoc cref="CharData.SetLayoutStartPoint"/>
    void SetLayoutStartPoint(CharData charData, TextPoint point /*, TextPoint baselineStartPoint*/);

    /// <inheritdoc cref="CharData.SetCharDataInfo"/>
    void SetCharDataInfo(CharData charData, TextSize textSize, double baseline);
}