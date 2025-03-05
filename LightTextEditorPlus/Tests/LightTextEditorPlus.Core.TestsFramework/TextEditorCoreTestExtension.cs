using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Core.TestsFramework;

public static class TextEditorCoreTestExtension
{
    public static TextEditorCore UseFixedLineSpacing(this TextEditorCore textEditorCore, double fixedLineSpacing = 15)
    {
        textEditorCore.DocumentManager.SetStyleParagraphProperty(textEditorCore.DocumentManager.StyleParagraphProperty with
        {
            LineSpacing = TextLineSpacings.ExactlyLineSpace(fixedLineSpacing)
        });

        return textEditorCore;
    }
}
