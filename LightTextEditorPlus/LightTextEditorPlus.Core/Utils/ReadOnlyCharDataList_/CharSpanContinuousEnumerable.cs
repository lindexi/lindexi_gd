using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Utils;

/// <summary>
/// 连续的字符数据枚举器
/// </summary>
public readonly struct CharSpanContinuousEnumerable
{
    internal CharSpanContinuousEnumerable(TextReadOnlyListSpan<CharData> charList, CheckCharDataContinuous? checker)
    {
        _charList = charList;
        _checker = checker;
    }

    private readonly TextReadOnlyListSpan<CharData> _charList;

    private readonly CheckCharDataContinuous? _checker;

    /// <summary>
    /// 获取枚举器
    /// </summary>
    /// <returns></returns>
    public CharSpanContinuousEnumerator GetEnumerator()
    {
        return new CharSpanContinuousEnumerator(_charList, _checker);
    }

    /// <summary>
    /// 获取首段连续的字符数据的迭代器
    /// </summary>
    public struct CharSpanContinuousEnumerator
    {
        internal CharSpanContinuousEnumerator(TextReadOnlyListSpan<CharData> charList, CheckCharDataContinuous? checker)
        {
            _currentList = charList;
            _checker = checker;
        }

        private TextReadOnlyListSpan<CharData> _currentList;

        private readonly CheckCharDataContinuous? _checker;

        /// <summary>
        /// 当前连续的字符数据
        /// </summary>
        public TextReadOnlyListSpan<CharData> Current { get; private set; }

        /// <summary>
        /// 获取下一个连续的字符数据
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (_currentList.Count > 0)
            {
                var firstSpan = _currentList.GetFirstCharSpanContinuous(_checker);
                Current = firstSpan;

                _currentList = _currentList.Slice(start: firstSpan.Count);
                return true;
            }

            return false;
        }
    }
}