using Avalonia.Media;

using LightTextEditorPlus;

using SimpleWrite.Business.ShortcutManagers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Editing;

namespace SimpleWrite.Business.TextEditors;

/// <summary>
/// 文本编辑器
/// </summary>
internal sealed class SimpleWriteTextEditor : TextEditor
{
    public SimpleWriteTextEditor()
    {
        CaretConfiguration.SelectionBrush = new Color(0x9F, 0x26, 0x3F, 0xC7);
    }

    public required ShortcutExecutor ShortcutExecutor { get; init; }

    protected override TextEditorHandler CreateTextEditorHandler()
    {
        return new SimpleWriteTextEditorHandler(this)
        {
            ShortcutExecutor = ShortcutExecutor
        };
    }
}
