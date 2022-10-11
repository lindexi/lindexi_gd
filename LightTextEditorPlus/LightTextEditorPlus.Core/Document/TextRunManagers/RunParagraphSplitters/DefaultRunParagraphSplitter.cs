using System.Collections.Generic;

namespace LightTextEditorPlus.Core.Document;

internal class DefaultRunParagraphSplitter : IRunParagraphSplitter
{
    public IEnumerable<IRun> Split(IRun run)
    {
        // todo 实现分段逻辑
        return new[] { run };
    }
}