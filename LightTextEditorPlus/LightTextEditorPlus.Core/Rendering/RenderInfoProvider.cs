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
            var paragraphData = list[index];
            yield return new ParagraphRenderInfo(index, paragraphData);
        }
    }
}

public class ParagraphRenderInfo
{
    internal ParagraphRenderInfo(int index, ParagraphData paragraphData)
    {
        
    }
    // todo 考虑作为结构体

}