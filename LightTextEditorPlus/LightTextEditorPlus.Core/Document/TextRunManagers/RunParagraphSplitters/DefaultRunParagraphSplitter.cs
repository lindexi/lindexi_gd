using System;
using System.Collections.Generic;

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

    private IEnumerable<IRun> Split(TextRun textRun)
    {
        var text = textRun.Text;
        foreach (var subText in Split(text))
        {
            yield return new TextRun(subText, textRun.RunProperty);
        }
    }

    private IEnumerable<string> Split(string text)
    {
        throw new NotImplementedException();
    }
}