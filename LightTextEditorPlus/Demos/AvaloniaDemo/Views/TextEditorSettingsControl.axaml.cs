using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

using SkiaSharp;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class TextEditorSettingsControl : UserControl
{
    private TextEditor _textEditor = null!;

    public TextEditorSettingsControl()
    {
        InitializeComponent();
        Loaded += TextEditorSettingsControl_Loaded;
    }

    private void TextEditorSettingsControl_Loaded(object? sender, RoutedEventArgs e)
    {
        List<string> list = SKFontManager.Default.FontFamilies.ToList();
        list.Sort();
        list.Insert(0, "仓耳小丸子");

        FontNameComboBox.ItemsSource = list;
    }

    private void TextEditorCore_CurrentCaretOffsetChanged(object? sender, TextEditorValueChangeEventArgs<CaretOffset> e)
    {
        SetFeedback();
    }

    private void SetFeedback()
    {
        SkiaTextRunProperty currentCaretRunProperty = TextEditor.CurrentCaretRunProperty;

        FontNameComboBox.SelectedValue = currentCaretRunProperty.FontName.UserFontName;

        FontSizeTextBox.Text = currentCaretRunProperty.FontSize.ToString("0");
    }

    public TextEditor TextEditor
    {
        get => _textEditor;
        set
        {
            _textEditor = value;

            _textEditor.CurrentCaretOffsetChanged += TextEditorCore_CurrentCaretOffsetChanged;
            SetFeedback();
        }
    }

    private void FontNameComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
        {
            string? fontName = e.AddedItems[0]?.ToString();
            if (!string.IsNullOrEmpty(fontName))
            {
                TextEditor.SetFontName(fontName, GetSelection());
            }
        }
    }

    private void ApplyFontSizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(FontSizeTextBox.Text, out var fontSize))
        {
            TextEditor.SetFontSize(fontSize, GetSelection());
        }
    }

    private void ManualSizeToContentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.SizeToContent = SizeToContent.Manual;
        TextEditor.HorizontalAlignment = HorizontalAlignment.Stretch;
        TextEditor.VerticalAlignment = VerticalAlignment.Stretch;
    }

    private void WidthSizeToContentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.Width = double.NaN;
        TextEditor.HorizontalAlignment = HorizontalAlignment.Left;
        TextEditor.SizeToContent = SizeToContent.Width;
    }

    private void HeightSizeToContentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.SizeToContent = SizeToContent.Height;
        TextEditor.Height = double.NaN;
        TextEditor.VerticalAlignment = VerticalAlignment.Top;
    }

    private void WidthAndHeightSizeToContentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.SizeToContent = SizeToContent.WidthAndHeight;
        TextEditor.HorizontalAlignment = HorizontalAlignment.Left;
        TextEditor.VerticalAlignment = VerticalAlignment.Top;
        TextEditor.Width = double.NaN;
        TextEditor.Height = double.NaN;
    }

    private void ColorEllipse_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control control && control.DataContext is SolidColorBrush solidColorBrush)
        {
            TextEditor.SetForeground(solidColorBrush);
        }
    }

    private Selection? GetSelection()
    {
        if (ApplyStyleToSelectionRadioButton.IsChecked is true)
        {
            return TextEditor.CurrentSelection;
        }
        else if (ApplyStyleToTextEditorRadioButton.IsChecked is true)
        {
            return TextEditor.TextEditorCore.GetAllDocumentSelection();
        }

        return null;
    }

    private void ToggleBoldButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.ToggleBold(GetSelection());
    }

    private void ToggleItalicButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.ToggleItalic(GetSelection());
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

    private void FullExpandButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.TextEditorCore.LineSpacingStrategy = LineSpacingStrategy.FullExpand;
    }

    private void FirstLineShrinkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.TextEditorCore.LineSpacingStrategy = LineSpacingStrategy.FirstLineShrink;
    }

    private void LineSpacingButton_OnClick(object? sender, RoutedEventArgs e)
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

    private void FixedLineSpacingButton_OnClick(object? sender, RoutedEventArgs e)
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
            FixedLineSpacingTextBox.Text = null;
        }
    }

    private void FixedLineSpacingResetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SetCurrentParagraphProperty(GetCurrentParagraphProperty() with
        {
            LineSpacing = TextLineSpacings.SingleLineSpace(),
        });

        FixedLineSpacingTextBox.Text = null;
    }

    #endregion

    private ParagraphProperty GetCurrentParagraphProperty() => TextEditor.TextEditorCore.DocumentManager.GetParagraphProperty(TextEditor.CurrentCaretOffset);

    private void SetCurrentParagraphProperty(ParagraphProperty paragraphParagraph) =>
        TextEditor.TextEditorCore.DocumentManager.SetParagraphProperty(TextEditor.CurrentCaretOffset, paragraphParagraph);

    #region 水平对齐

    private void LeftHorizontalTextAlignmentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            HorizontalTextAlignment = HorizontalTextAlignment.Left
        });
    }

    private void CenterHorizontalTextAlignmentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            HorizontalTextAlignment = HorizontalTextAlignment.Center
        });
    }

    private void RightHorizontalTextAlignmentButton_OnClick(object? sender, RoutedEventArgs e)
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
}
