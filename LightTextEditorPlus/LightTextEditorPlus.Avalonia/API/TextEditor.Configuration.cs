using System;

namespace LightTextEditorPlus;

// 此文件存放配置相关的方法
[APIConstraint("TextEditor.Configuration.txt")]
partial class TextEditor
{
    public CaretConfiguration CaretConfiguration
    {
        get => new CaretConfiguration(SkiaTextEditor.CaretConfiguration);
        set => SkiaTextEditor.CaretConfiguration = value;
    }
}
