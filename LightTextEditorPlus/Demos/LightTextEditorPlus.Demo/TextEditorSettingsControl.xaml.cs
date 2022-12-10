using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Demo;
/// <summary>
/// TextEditorSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class TextEditorSettingsControl : UserControl
{
    public TextEditorSettingsControl()
    {
        InitializeComponent();

        FontNameComboBox.ItemsSource = Fonts.SystemFontFamilies
            .Where(t => t.FamilyNames.Values is not null)
            .SelectMany(t => t.FamilyNames.Values!).Distinct();
    }

    public static readonly DependencyProperty TextEditorProperty = DependencyProperty.Register(
        nameof(TextEditor), typeof(TextEditor), typeof(TextEditorSettingsControl), new PropertyMetadata(default(TextEditor)));

    public TextEditor TextEditor
    {
        get { return (TextEditor) GetValue(TextEditorProperty); }
        set { SetValue(TextEditorProperty, value); }
    }

    private void FontNameComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var fontFamily = (string) e.AddedItems[0]!;

        TextEditor.SetRunProperty(runProperty =>
        {
            runProperty.FontName = new FontName(fontFamily);
        });
    }

    private void ApplyFontSizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(FontSizeTextBox.Text, out var fontSize))
        {
            TextEditor.SetRunProperty(runProperty =>
            {
                runProperty.FontSize = fontSize;
            });
        }
        else
        {
            // 别逗
        }
    }
}
