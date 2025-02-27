using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

using Brush = System.Windows.Media.Brush;
using Size = System.Windows.Size;

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

        TextEditor.SetFontName(fontFamily);
    }

    private void ApplyFontSizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(FontSizeTextBox.Text, out var fontSize))
        {
            TextEditor.SetFontSize(fontSize);
        }
        else
        {
            // 别逗
        }
    }

    private void ToggleBoldButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.ToggleBold();
    }

    private void ToggleItalicButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.ToggleItalic();
    }

    private void ForegroundButton_OnClick(object sender, RoutedEventArgs e)
    {
        Button button = (Button) sender;
        var brush = (Brush) button.DataContext;
        TextEditor.SetForeground(new ImmutableBrush(brush));
    }

    private void FullExpandButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.TextEditorCore.LineSpacingStrategy = LineSpacingStrategy.FullExpand;
    }

    private void FirstLineShrinkButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.TextEditorCore.LineSpacingStrategy = LineSpacingStrategy.FirstLineShrink;
    }

    #region 布局方式

    private void HorizontalArrangingTypeButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.ArrangingType = ArrangingType.Horizontal;
    }

    private void VerticalArrangingTypeButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.ArrangingType = ArrangingType.Vertical;
    }

    private void MongolianArrangingTypeButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.ArrangingType = ArrangingType.Mongolian;
    }
    #endregion

    #region 行距

    private void LineSpacingButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(LineSpacingTextBox.Text, out var lineSpacing))
        {
            SetCurrentParagraphProperty(GetCurrentParagraphProperty() with
            {
                LineSpacing = TextLineSpacings.MultipleLineSpace(lineSpacing)
            });
        }
        else
        {
            LineSpacingTextBox.Text = 1.ToString();
        }
    }

    private void FixedLineSpacingButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(FixedLineSpacingTextBox.Text, out var fixedLineSpacing))
        {
            SetCurrentParagraphProperty(GetCurrentParagraphProperty() with
            {
                LineSpacing = TextLineSpacings.ExactlyLineSpace(fixedLineSpacing)
            });
        }
        else
        {
            FixedLineSpacingTextBox.Text = string.Empty;
        }
    }

    private void FixedLineSpacingResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        SetCurrentParagraphProperty(GetCurrentParagraphProperty() with
        {
            LineSpacing = TextLineSpacings.SingleLineSpace(),
        });

        FixedLineSpacingTextBox.Text = string.Empty;
    }

    #endregion

    #region 行距算法

    private void WPFLineSpacingAlgorithmButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.UseWpfLineSpacingStyle();
    }

    private void PPTLineSpacingAlgorithmButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.UsePptLineSpacingStyle();
    }
    #endregion

    #region 水平对齐

    private void LeftHorizontalTextAlignmentButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            HorizontalTextAlignment = HorizontalTextAlignment.Left
        });
    }

    private void CenterHorizontalTextAlignmentButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            HorizontalTextAlignment = HorizontalTextAlignment.Center
        });
    }

    private void RightHorizontalTextAlignmentButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            HorizontalTextAlignment = HorizontalTextAlignment.Right
        });
    }
    #endregion

    #region 垂直对齐

    private void TopVerticalTextAlignmentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.VerticalTextAlignment = VerticalAlignment.Top;
    }

    private void CenterVerticalTextAlignmentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.VerticalTextAlignment = VerticalAlignment.Center;
    }

    private void BottomVerticalTextAlignmentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.VerticalTextAlignment = VerticalAlignment.Bottom;
    }

    #endregion

    #region 辅助方法

    private ParagraphProperty GetCurrentParagraphProperty() => TextEditor.GetCurrentCaretOffsetParagraphProperty();

    private void SetCurrentParagraphProperty(ParagraphProperty paragraphParagraph) =>
        TextEditor.SetParagraphProperty(TextEditor.CurrentCaretOffset, paragraphParagraph);

    #endregion

    #region 缩进

    private void IndentButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(IndentTextBox.Text, out var value))
        {
            SetIndent(value);
        }
        else
        {
            // 别逗
            IndentTextBox.Text = 0.ToString();
        }
    }

    private void AddIndentButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(IndentTextBox.Text, out var value))
        {
            value++;
            SetIndent(value);
            IndentTextBox.Text = value.ToString("#.##");
        }
        else
        {
            // 别逗
            IndentTextBox.Text = 0.ToString();
        }
    }

    private void SubtractIndentButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(IndentTextBox.Text, out var value))
        {
            value--;
            SetIndent(value);
            IndentTextBox.Text = value.ToString("#.##");
        }
        else
        {
            // 别逗
            IndentTextBox.Text = 0.ToString();
        }
    }

    private void SetIndent(double indent)
    {
        TextEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            Indent = indent
        });
    }

    private void IndentTypeButton_OnClick(object sender, RoutedEventArgs e)
    {
        string? text = IndentTypeComboBox.SelectionBoxItem.ToString();
        TextEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            IndentType = text switch
            {
                "首行缩进" => IndentType.FirstLine,
                "悬挂缩进" => IndentType.Hanging,
                _ => IndentType.FirstLine
            }
        });
    }

    #endregion

    #region 边距

    private void LeftIndentationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(LeftIndentationTextBox.Text, out var value))
        {
            SetLeftIndentation(value);
        }
        else
        {
            // 别逗
            LeftIndentationTextBox.Text = 0.ToString();
        }
    }

    private void AddLeftIndentationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(LeftIndentationTextBox.Text, out var value))
        {
            value++;
            SetLeftIndentation(value);
            LeftIndentationTextBox.Text = value.ToString("#.##");
        }
        else
        {
            // 别逗
            LeftIndentationTextBox.Text = 0.ToString();
        }
    }

    private void SubtractLeftIndentationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(LeftIndentationTextBox.Text, out var value))
        {
            value--;
            SetLeftIndentation(value);
            LeftIndentationTextBox.Text = value.ToString("#.##");
        }
        else
        {
            // 别逗
            LeftIndentationTextBox.Text = 0.ToString();
        }
    }

    private void SetLeftIndentation(double leftIndentation)
    {
        TextEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            LeftIndentation = leftIndentation
        });
    }

    private void RightIndentationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(RightIndentationTextBox.Text, out var value))
        {
            SetRightIndentation(value);
        }
        else
        {
            // 别逗
            RightIndentationTextBox.Text = 0.ToString();
        }
    }

    private void AddRightIndentationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(RightIndentationTextBox.Text, out var value))
        {
            value++;
            SetRightIndentation(value);
            RightIndentationTextBox.Text = value.ToString("#.##");
        }
        else
        {
            // 别逗
            RightIndentationTextBox.Text = 0.ToString();
        }
    }

    private void SubtractRightIndentationButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(RightIndentationTextBox.Text, out var value))
        {
            value--;
            SetRightIndentation(value);
            RightIndentationTextBox.Text = value.ToString("#.##");
        }
        else
        {
            // 别逗
        }
    }

    private void SetRightIndentation(double rightIndentation)
    {
        TextEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            RightIndentation = rightIndentation
        });
    }
    #endregion

    #region 自适应
    private void ManualSizeToContentButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.SizeToContent = SizeToContent.Manual;
        TextEditor.HorizontalAlignment = HorizontalAlignment.Stretch;
        TextEditor.VerticalAlignment = VerticalAlignment.Stretch;
    }

    private void WidthSizeToContentButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.SizeToContent = SizeToContent.Width;
        TextEditor.HorizontalAlignment = HorizontalAlignment.Left;
        TextEditor.Width = double.NaN;
    }

    private void HeightSizeToContentButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.SizeToContent = SizeToContent.Height;
        TextEditor.VerticalAlignment = VerticalAlignment.Top;
        TextEditor.Height = double.NaN;
    }

    private void WidthAndHeightSizeToContentButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextEditor.SizeToContent = SizeToContent.WidthAndHeight;
        TextEditor.HorizontalAlignment = HorizontalAlignment.Left;
        TextEditor.VerticalAlignment = VerticalAlignment.Top;
        TextEditor.Width = double.NaN;
        TextEditor.Height = double.NaN;
    }
    #endregion
}
