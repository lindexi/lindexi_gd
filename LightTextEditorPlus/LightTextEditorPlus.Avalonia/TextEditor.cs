using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;
using LightTextEditorPlus.Core;

namespace LightTextEditorPlus.Avalonia;

public partial class TextEditor : Control
{
    public TextEditor()
    {
        SkiaTextEditor = new SkiaTextEditor();
    }

    public SkiaTextEditor SkiaTextEditor { get; }
    public TextEditorCore TextEditorCore => SkiaTextEditor.TextEditorCore;
}
