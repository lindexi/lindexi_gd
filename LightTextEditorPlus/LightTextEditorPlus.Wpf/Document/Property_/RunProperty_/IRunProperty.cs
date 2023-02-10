using LightTextEditorPlus.Core.Document;

using System.Windows;

namespace LightTextEditorPlus.Document;

public interface IRunProperty : IReadOnlyRunProperty
{
    ImmutableBrush Foreground { get; }
    ImmutableBrush? Background { get; }
    double Opacity { get; }
    FontStretch Stretch { get; }
    FontWeight FontWeight { get; }
    FontStyle FontStyle { get; }
}