using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Media;

using LightTextEditorPlus;

namespace SimpleWrite.Business;

/// <summary>
/// 文本编辑器
/// </summary>
internal class SimpleWriteTextEditor : TextEditor
{
    public SimpleWriteTextEditor()
    {
        CaretConfiguration.SelectionBrush = new Color(0x9F, 0x26, 0x3F, 0xC7);
    }
}
