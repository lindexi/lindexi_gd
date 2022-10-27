using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Document;
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Rendering;

public class RenderInfoProvider
{
    public RenderInfoProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditor TextEditor { get; }

    public IEnumerable<ParagraphRenderInfo> GetParagraphRenderInfoList()
    {
        var paragraphManager = TextEditor.DocumentManager.TextRunManager.ParagraphManager;
        var list = paragraphManager.GetParagraphList();
        for (var index = 0; index < list.Count; index++)
        {
            TextEditor.VerifyNotDirty();
            var paragraphData = list[index];
            yield return new ParagraphRenderInfo(index, paragraphData);
        }
    }
}

public readonly struct ParagraphRenderInfo
{
    internal ParagraphRenderInfo(int index, ParagraphData paragraphData)
    {
        Index = index;
        _paragraphData = paragraphData;
    }
    public int Index { get; }
    private readonly ParagraphData _paragraphData;

    public IEnumerable<ParagraphLineRenderInfo> GetLineRenderInfoList()
    {
        for (var i = 0; i < _paragraphData.LineVisualDataList.Count; i++)
        {
            LineVisualData lineVisualData = _paragraphData.LineVisualDataList[i];

            var argument = lineVisualData.GetLineDrawnArgument();

            _paragraphData.ParagraphManager.TextEditor.VerifyNotDirty();

            yield return new ParagraphLineRenderInfo(i,argument)
            {
                LineVisualData = lineVisualData
            };
        }
    }
}

public readonly record struct ParagraphLineRenderInfo(int Index, LineDrawnArgument Argument)
{
    internal LineVisualData LineVisualData { init; get; } = null!;

    public void SetDrawnResult(in LineDrawnResult lineDrawnResult)
    {
        LineVisualData.SetDrawn(lineDrawnResult);
    }
}