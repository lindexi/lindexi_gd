namespace LightTextEditorPlus.Core.Primitive;

/// <summary>Specifies the vertical position of a <see cref="T:LightTextEditorPlus.Core.Document.Decorations.ITextEditorDecoration" /> object.</summary>
/// Copy From WPF
public enum TextEditorDecorationLocation
{
    /// <summary>The vertical position of an underline. This is the default value.</summary>
    Underline,
   
    /// <summary>The vertical position of an overline.</summary>
    OverLine,

    /// <summary>The vertical position of a strikethrough.</summary>
    Strikethrough,

    /// <summary>The vertical position of a baseline.</summary>
    Baseline,
}