using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Platform;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus;

public partial class TextEditor
{
    public TextEditor()
    {
        var mauiTextEditorPlatformProvider = new MauiTextEditorPlatformProvider(this);
        TextEditorCore = new TextEditorCore(mauiTextEditorPlatformProvider);
    }

    public TextEditorCore TextEditorCore { get; }
}

internal class MauiTextEditorPlatformProvider : PlatformProvider
{
    public MauiTextEditorPlatformProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    private TextEditor TextEditor { get; }
}
