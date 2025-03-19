using System;

namespace LightTextEditorPlus;

// 此文件存放配置相关的方法
[APIConstraint("TextEditor.Configuration.txt")]
partial class TextEditor
{
    public CaretConfiguration CaretConfiguration
    {
        get
        {
            if (SkiaTextEditor.CaretConfiguration is CaretConfiguration caretConfiguration)
            {
                return caretConfiguration;
            }

            caretConfiguration = new CaretConfiguration(SkiaTextEditor.CaretConfiguration);
            SkiaTextEditor.CaretConfiguration = caretConfiguration;
            return caretConfiguration;
        }
        set => SkiaTextEditor.CaretConfiguration = value;
    }
}
