using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LightTextEditorPlus.Core.Document;

internal class DefaultRunParagraphSplitter : IRunParagraphSplitter
{
    public IEnumerable<IRun> Split(IRun run)
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
        else
        {
            // todo 处理非文本的情况
            throw new NotImplementedException();
        }
    }

    private static IEnumerable<IRun> Split(TextRun textRun)
    {
        var text = textRun.Text;
        foreach (var subText in Split(text))
        {
            if (subText is null)
            {
                yield return new EmptyRun(textRun.RunProperty);
            }
            else
            {
                yield return new TextRun(subText, textRun.RunProperty);
            }
        }
    }

    private static IEnumerable<string?> Split(string text)
    {
        int position = 0;
        bool endWithBreakLine = false;
        for (int i = 0; i < text.Length; i++)
        {
            var currentChar = text[i];
            if (currentChar is '\r' or '\n')
            {
                if (position == i)
                {
                    yield return null;
                }
                else
                {
                    var length = i - position;
                    yield return text.Substring(position, length);
                }

                if (i != text.Length - 1)
                {
                    var nextChar = text[i + 1];
                    if (nextChar is '\n')
                    {
                        i++;
                    }
                }

                position = i + 1;

                endWithBreakLine = true;
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
            yield return text.Substring(position, length);
        }

        if (endWithBreakLine)
        {
            yield return null;
        }
    }
}