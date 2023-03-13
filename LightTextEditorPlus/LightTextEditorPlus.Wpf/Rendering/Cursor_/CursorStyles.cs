using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LightTextEditorPlus.Rendering;

/// <summary>
/// 光标样式
/// </summary>
public record CursorStyles(Cursor Cursor)
{
    public Cursor? VerticalCursor
    {
        init;
        get;
    }
}
