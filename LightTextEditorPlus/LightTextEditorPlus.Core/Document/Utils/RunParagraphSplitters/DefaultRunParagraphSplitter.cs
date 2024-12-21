using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LightTextEditorPlus.Core.Document.Utils;

internal class DefaultRunParagraphSplitter : IRunParagraphSplitter
{
    public IEnumerable<IImmutableRun> Split(IImmutableRun run)
    {
        if (run is TextRun textRun)
        {
            var text = textRun.Text;

            if (text.Contains('\n') || text.Contains('\r'))
            {
                return Split(textRun);
            }
            else
            {
                // 这是一个优化，大部分的文本都没有包含换行的输入，那就返回自身即可，不需要再构建复杂的逻辑
                return new[] { textRun };
            }
        }
        else if (run is SingleCharImmutableRun singleCharImmutableRun)
        {
            return new[] { singleCharImmutableRun };
        }
        else
        {
            return new[] { run };
        }
    }

    private static IEnumerable<IImmutableRun> Split(TextRun textRun)
    {
        var text = textRun.Text;
        foreach (SplitTextResult result in Split(text))
        {
            if (result.IsLineBreak)
            {
                yield return new LineBreakRun(textRun.RunProperty);
            }
            else
            {
                yield return new SpanTextRun(text, result.Start, result.Length, textRun.RunProperty);
                //yield return new TextRun(subText, textRun.RunProperty);
            }
        }
    }

    private readonly record struct SplitTextResult(int Start, int Length)
    {
        public bool IsLineBreak => Length == 0;
    }

    private static IEnumerable<SplitTextResult> Split(string text)
    {
        int position = 0;
        bool endWithBreakLine = false;
        for (int i = 0; i < text.Length; i++)
        {
            var currentChar = text[i];
            // 是否 \r 字符
            bool isCr = currentChar == '\r';
            // 是否 \n 字符
            bool isLf = !isCr && currentChar == '\n';
            if (isCr || isLf)
            {
                if (position == i)
                {
                    yield return new SplitTextResult(i,0);
                }
                else
                {
                    var length = i - position;
                    yield return new SplitTextResult(position, length); //text.Substring(position, length);

                    endWithBreakLine = true;
                }

                // 如果是 \r 情况下，读取下一个字符，判断是否 \n 字符
                if (isCr && i != text.Length - 1)
                {
                    var nextChar = text[i + 1];
                    if (nextChar is '\n')
                    {
                        i++;
                    }
                }

                position = i + 1;
            }
            else
            {
                endWithBreakLine = false;
            }
        }

        if (position < text.Length)
        {
            Debug.Assert(endWithBreakLine is false);

            var length = text.Length - position;
            yield return new SplitTextResult(position, length);
        }

        if (endWithBreakLine)
        {
            yield return new SplitTextResult(text.Length - 1, 0);
        }
    }
}