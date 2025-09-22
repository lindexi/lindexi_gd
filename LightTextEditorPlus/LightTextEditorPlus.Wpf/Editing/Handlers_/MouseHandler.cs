using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Rendering;

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Editing;

internal class MouseHandler
{
    public MouseHandler(TextEditor textEditor)
    {
        TextEditor = textEditor;

        textEditor.TextEditorCore.ArrangingTypeChanged += (_, _) => TextEditorHandler.UpdateCursorView();
        textEditor.CursorStylesChanged += (_, _) => TextEditorHandler.UpdateCursorView();
    }

    private TextEditorHandler TextEditorHandler => TextEditor.TextEditorHandler;


    private TextEditor TextEditor { get; }
}
