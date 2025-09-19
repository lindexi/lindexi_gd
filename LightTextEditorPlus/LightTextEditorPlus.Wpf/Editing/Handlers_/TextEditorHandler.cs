using LightTextEditorPlus.Core.Editing;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LightTextEditorPlus.Editing;

public partial class TextEditorHandler
{
    public virtual void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Insert)
        {
            SwitchOvertypeMode();
            e.Handled = true;
            return;
        }
        else if (e.Key == Key.Enter)
        {
            BreakLine();
            e.Handled = true;
            return;
        }
    }

    public virtual void OnTextInput(TextCompositionEventArgs e)
    {
        if (e.Handled ||
            string.IsNullOrEmpty(e.Text) ||
            e.Text == "\x1b" ||
            // 退格键 \b 键
            e.Text == "\b" ||
            //emoji包围符
            e.Text == "\ufe0f")
            return;

        RawTextInput(e.Text);
    }
}
