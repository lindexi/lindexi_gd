using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Platform;

namespace LightTextEditorPlus.AvaloniaDemo.Views.Controls;

/// <summary>
/// 控制台文本编辑器
/// </summary>
public class ConsoleTextEditor : TextEditor
{
    public ConsoleTextEditor() : base(new Builder())
    {
    }
}

file class Builder : AvaloniaSkiaTextEditorPlatformProviderBuilder
{
    public override AvaloniaSkiaTextEditorPlatformProvider Build(TextEditor avaloniaTextEditor)
    {
        return new Provider(avaloniaTextEditor);
    }
}

file class Provider : AvaloniaSkiaTextEditorPlatformProvider
{
    public Provider(TextEditor avaloniaTextEditor) : base(avaloniaTextEditor)
    {
    }

    public override ICharInfoMeasurer GetCharInfoMeasurer()
    {
        return new CharInfoMeasurer();
    }
}

file class CharInfoMeasurer : ICharInfoMeasurer
{
    public void MeasureAndFillSizeOfRun(in FillSizeOfRunArgument argument)
    {
        
    }
}