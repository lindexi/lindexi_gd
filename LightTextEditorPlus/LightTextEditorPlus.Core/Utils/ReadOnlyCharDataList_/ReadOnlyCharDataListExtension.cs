using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Utils;

/// <summary>
/// 只读的字符列表扩展方法
/// </summary>
public static class ReadOnlyCharDataListExtension
{
    /// <summary>
    /// 将传入的字符列表，根据判断条件，分割为一个个相邻的字符列表
    /// </summary>
    /// <param name="charDataList"></param>
    /// <param name="predicate">判断给定两个 <see cref="CharData"/> 是否符合条件</param>
    /// <returns></returns>
    public static IEnumerable<TextReadOnlyListSpan<CharData>> SplitContinuousCharData(this TextReadOnlyListSpan<CharData> charDataList,
        Func<CharData, CharData, bool> predicate)
    {
        CharData lastCharData = null!;
        int length = 0;
        for (var i = 0; i < charDataList.Count; i++)
        {
            var charData = charDataList[i];

            if (i == 0)
            {
                length++;
            }
            else
            {
                var result = predicate(lastCharData, charData);
                if (result)
                {
                    // 如果是相同的
                    length++;
                }
                else
                {
                    // 如果是不相同的，那就将当前的列表返回
                    var startIndex = i - length;

                    yield return charDataList.Slice(startIndex, length);

                    // 当前列表返回了，那就剩下当前这个一个
                    // 例子，如只有两个元素，且两个元素不相同。那第一个元素返回之后，还剩下一个元素
                    length = 1;
                }
            }

            if (i == charDataList.Count - 1)
            {
                if (length != 0)
                {
                    var startIndex = i - length + 1;

                    yield return charDataList.Slice(startIndex, length);
                }
            }

            lastCharData = charData;
        }
    }

    /// <summary>
    /// 转换为 char 列表，这个过程尽可能使用池化的方式
    /// </summary>
    /// <param name="list"></param>
    /// <param name="arrayPool"></param>
    /// <returns></returns>
    public static CharDataListToCharSpanResult ToCharSpan(this TextReadOnlyListSpan<CharData> list, ArrayPool<char>? arrayPool = null)
    {
        arrayPool ??= ArrayPool<char>.Shared;

        var buffer = arrayPool.Rent(list.Count * 2);

        var length = 0;

        foreach (CharData charData in list)
        {
            var index = length;
            Span<char> currentSpan = buffer.AsSpan(index);

            Rune rune = charData.CharObject.CodePoint.Rune;
            int writtenLength = rune.EncodeToUtf16(currentSpan);
            length += writtenLength;
        }

        return new CharDataListToCharSpanResult(buffer, length, arrayPool);
    }

    /// <summary>
    /// 将字符数据列表转换为文本
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static string ToText(this TextReadOnlyListSpan<CharData> list)
    {
        var stringBuilder = new StringBuilder();
        foreach (CharData charData in list)
        {
            stringBuilder.Append(charData.CharObject.ToText());
        }
        return stringBuilder.ToString();
    }

    /// <summary>
    /// 转换为限制字符数量的文本
    /// </summary>
    /// <param name="list"></param>
    /// <param name="limitCharCount"></param>
    /// <param name="replaceText"></param>
    /// <param name="saveStartAndEnd">保留前后，删除中间</param>
    /// <returns></returns>
    internal static string ToLimitText(this TextReadOnlyListSpan<CharData> list, int limitCharCount, string? replaceText = null, bool saveStartAndEnd = true)
    {
        // 这个方法现在只有调试下调用进来，先不管其性能
        string text = list.ToText();
        return text.LimitTrim(limitCharCount, replaceText, saveStartAndEnd);
    }

    /// <inheritdoc cref="GetFirstCharSpanContinuous"/>
    public static IEnumerable<TextReadOnlyListSpan<CharData>> GetCharSpanContinuous(this TextReadOnlyListSpan<CharData> charList, CheckCharDataContinuous? checker = null)
    {
        var currentList = charList;
        while (currentList.Count > 0)
        {
            var firstSpan = currentList.GetFirstCharSpanContinuous(checker);
            yield return firstSpan;
            currentList = currentList.Slice(start: firstSpan.Count);
        }
    }

    /// <summary>
    /// 获取首段连续的字符数据。从传入的 <paramref name="charList"/> 里截取首段连续的字符数据
    /// </summary>
    /// <param name="charList"></param>
    /// <param name="checker">默认不传将只判断 <see cref="CharData.RunProperty"/> 部分</param>
    /// <returns></returns>
    public static TextReadOnlyListSpan<CharData> GetFirstCharSpanContinuous(this TextReadOnlyListSpan<CharData> charList, CheckCharDataContinuous? checker = null)
    {
        if (charList.Count == 0)
        {
            return charList;
        }

        var count = 1;
        var current = charList[0];
        for (int i = 1; i < charList.Count; i++)
        {
            var next = charList[i];
            if (checker is null)
            {
                if (current.RunProperty != next.RunProperty)
                {
                    break;
                }
            }
            else
            {
                if (!checker(current, next))
                {
                    break;
                }
            }
            count++;
            current = next; // 这步有些多余，但是为了代码的可读性，还是加上了。为什么多余？因此此时 current 必定和 next 相等
        }

        return charList.Slice(0, count);
    }
}

/// <summary>
/// 判断两个字符数据是否连续，即判断是否属性等相同或可以作为连续的字符
/// </summary>
/// <returns></returns>
public delegate bool CheckCharDataContinuous(CharData current, CharData next);