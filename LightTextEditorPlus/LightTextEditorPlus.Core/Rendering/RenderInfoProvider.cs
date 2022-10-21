using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Rendering;

public class RenderInfoProvider
{
    public RenderInfoProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditor TextEditor { get; }


}
