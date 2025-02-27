using System.Runtime.CompilerServices;
using System.Text;

namespace LightTextEditorPlus.Core.Layout.HitTests;

/// <summary>
/// 命中测试的日志上下文
/// </summary>
/// <param name="TextEditor"></param>
readonly record struct HitTestLogContext(TextEditorCore TextEditor)
{
    /// <summary>
    /// 是否在调试模式
    /// </summary>
    private bool IsInDebugMode => TextEditor.DebugConfiguration.IsInDebugMode;

    public void RecordDebugMessage([InterpolatedStringHandlerArgument("")] ref HitTestDebugMessageInterpolatedStringHandler message)
    {
        if (IsInDebugMode)
        {
            string formattedText = message.GetFormattedText();


        }
    }

    /// <summary>
    /// 用于记录命中测试调试信息的字符串处理器
    /// </summary>
    [InterpolatedStringHandler]
    public readonly ref struct HitTestDebugMessageInterpolatedStringHandler
    {
        public HitTestDebugMessageInterpolatedStringHandler(int literalLength, int formattedCount,
            HitTestLogContext context, out bool isEnable)
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