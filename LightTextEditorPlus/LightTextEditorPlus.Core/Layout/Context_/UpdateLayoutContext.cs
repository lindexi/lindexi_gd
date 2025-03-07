using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 更新布局的上下文
/// </summary>
public class UpdateLayoutContext : ICharDataLayoutInfoSetter
{
    internal UpdateLayoutContext(ArrangingLayoutProvider arrangingLayoutProvider)
    {
        ArrangingLayoutProvider = arrangingLayoutProvider;

        IReadOnlyList<ParagraphData> paragraphList = TextEditor.DocumentManager.ParagraphManager.GetParagraphList();
        InternalParagraphList = paragraphList;
        ParagraphList = TextEditor.ParagraphList;
    }

    /// <summary>
    /// 段落列表
    /// </summary>
    public ReadOnlyParagraphList ParagraphList { get; set; }

    /// <summary>
    /// 内部使用的段落列表
    /// </summary>
    internal IReadOnlyList<ParagraphData> InternalParagraphList { get; }

    internal ArrangingLayoutProvider ArrangingLayoutProvider { get; }

    /// <summary>
    /// 文本编辑器
    /// </summary>
    public TextEditorCore TextEditor => ArrangingLayoutProvider.TextEditor;

    /// <summary>
    /// 平台提供者
    /// </summary>
    public ITextEditorPlatformProvider PlatformProvider => TextEditor.PlatformProvider;

    /// <summary>
    /// 调试配置
    /// </summary>
    public TextEditorDebugConfiguration DebugConfiguration => TextEditor.DebugConfiguration;

    /// <summary>
    /// 是否在调试模式
    /// </summary>
    public bool IsInDebugMode => DebugConfiguration.IsInDebugMode;

    /// <summary>
    /// 日志
    /// </summary>
    public ITextLogger Logger => TextEditor.Logger;

    /// <summary>
    /// 记录布局过程的调试信息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="category"></param>
    public void RecordDebugLayoutInfo([InterpolatedStringHandlerArgument("")] ref LayoutDebugMessageInterpolatedStringHandler message, LayoutDebugCategory category)
    {
        if (!message.IsInDebugMode)
        {
            return;
        }
        string formattedText = message.GetFormattedText();
        var layoutDebugMessage = new LayoutDebugMessage(category, formattedText);
#if DEBUG
        var padCount = category switch
        {
            LayoutDebugCategory.Document => 0,
            LayoutDebugCategory.FindDirty => 1,

            LayoutDebugCategory.PreDocument => 1,
            LayoutDebugCategory.PreParagraph => 2,
            LayoutDebugCategory.PreWholeLine => 3,
            LayoutDebugCategory.PreSingleCharLine => 4,
            LayoutDebugCategory.PreDivideWord => 5,

            LayoutDebugCategory.FinalDocument => 1,
            LayoutDebugCategory.FinalParagraph => 2,
            LayoutDebugCategory.FinalLine => 3,

            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
        var debugMessage = $"{"".PadLeft(padCount)}[{category}] {formattedText}";
        Debug.WriteLine(debugMessage);
#endif

        LayoutDebugMessageList ??= [];
        LayoutDebugMessageList.Add(layoutDebugMessage);
    }

    /// <summary>
    /// 获取布局调试信息列表
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<LayoutDebugMessage> GetLayoutDebugMessageList()
    {
        if (LayoutDebugMessageList is { } list)
        {
            return list;
        }

        return Array.Empty<LayoutDebugMessage>();
    }

    private List<LayoutDebugMessage>? LayoutDebugMessageList { get; set; }

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
    public void SetLayoutStartPoint(CharData charData, TextPointInLine point)
    {
        if (IsCurrentLayoutCompleted)
        {
            throw new InvalidOperationException($"只有在布局过程才能设置 {nameof(charData)} 的布局属性");
        }

        charData.SetLayoutCharLineStartPoint(point);
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
    public readonly ref struct LayoutDebugMessageInterpolatedStringHandler
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

    /// <inheritdoc />
    public override string? ToString()
    {
        if (LayoutDebugMessageList != null)
        {
            return LayoutDebugMessageList.Last().ToString();
        }

        return base.ToString();
    }

    #region 缓存
    // 这里存放一些布局过程中的缓存，用于提升性能，在布局完成后自动 GC 释放。减少太多的静态对象

    /// <summary>
    /// 布局过程的默认 CharObject 对象
    /// </summary>
    public ICharObject LayoutDefaultCharData
        => _layoutDefaultCharData ??= new SingleCharObject(TextContext.DefaultChar);

    private ICharObject? _layoutDefaultCharData;

    #endregion
}