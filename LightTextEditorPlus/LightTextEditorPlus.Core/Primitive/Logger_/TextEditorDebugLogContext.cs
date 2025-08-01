using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本的调试日志上下文
/// </summary>
public readonly record struct TextEditorDebugLogContext
{
    /// <summary>
    /// 文本的调试日志上下文
    /// </summary>
    /// <param name="textEditor"></param>
    public TextEditorDebugLogContext(TextEditorCore textEditor)
    {
        IsInDebugMode = textEditor.DebugConfiguration.IsInDebugMode;
        if (IsInDebugMode)
        {
            DebugMessageList = new List<string>();
        }
    }

    private List<string>? DebugMessageList { get; }

    /// <summary>
    /// 是否在调试模式
    /// </summary>
    private bool IsInDebugMode { get; }

    /// <summary>
    /// 记录调试信息
    /// </summary>
    /// <param name="message"></param>
    public void RecordDebugMessage([InterpolatedStringHandlerArgument("")] ref HitTestDebugMessageInterpolatedStringHandler message)
    {
        if (IsInDebugMode)
        {
            string formattedText = message.GetFormattedText();

#if DEBUG
            Debug.WriteLine(formattedText);
#endif

            DebugMessageList?.Add(formattedText);
        }
    }

    /// <summary>
    /// 获取调试信息列表
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<string> GetDebugMessageList()
    {
        if (DebugMessageList is { } list)
        {
            return list;
        }

        return Array.Empty<string>();
    }

    /// <summary>
    /// 用于记录命中测试调试信息的字符串处理器
    /// </summary>
    [InterpolatedStringHandler]
    public readonly ref struct HitTestDebugMessageInterpolatedStringHandler
    {
        /// <summary>
        /// 创建用于记录命中测试调试信息的字符串处理器
        /// </summary>
        /// <param name="literalLength"></param>
        /// <param name="formattedCount"></param>
        /// <param name="context"></param>
        /// <param name="isEnable"></param>
        public HitTestDebugMessageInterpolatedStringHandler(int literalLength, int formattedCount,
            TextEditorDebugLogContext context, out bool isEnable)
        {
            bool isInDebugMode = context.IsInDebugMode;
            isEnable = isInDebugMode;

            if (isInDebugMode)
            {
                _stringBuilder = new StringBuilder();
            }
        }

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
}