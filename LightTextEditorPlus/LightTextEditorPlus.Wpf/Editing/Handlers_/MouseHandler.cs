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

        textEditor.MouseDown += TextEditor_MouseDown;
        textEditor.MouseMove += TextEditor_MouseMove;
        textEditor.MouseUp += TextEditor_MouseUp;
        textEditor.MouseEnter += TextEditor_MouseEnter;
        textEditor.LostMouseCapture += TextEditor_LostMouseCapture;
    }

    private TextEditorHandler TextEditorHandler => TextEditor.TextEditorHandler;


    private void TextEditor_MouseDown(object sender, MouseButtonEventArgs e)
    {
        TextEditorHandler.OnMouseDown(e);
    }

    private void TextEditor_MouseMove(object sender, MouseEventArgs e)
    {
        TextEditorHandler.OnMouseMove(e);
    }

    private void TextEditor_MouseUp(object sender, MouseButtonEventArgs e)
    {
        TextEditorHandler.OnMouseUp(e);
    }

    private void TextEditor_MouseEnter(object sender, MouseEventArgs e)
    {
        TextEditorHandler.OnMouseEnter(e);
    }

    private void TextEditor_LostMouseCapture(object sender, MouseEventArgs e)
    {
        TextEditorHandler.OnLostMouseCapture(e);
    }

    private TextEditor TextEditor { get; }
}
