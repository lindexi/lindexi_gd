using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    }

    private void WidthSizeToContentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.Width = double.NaN;
        TextEditor.SizeToContent = SizeToContent.Width;
    }

    private void HeightSizeToContentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.SizeToContent = SizeToContent.Height;
    }

    private void WidthAndHeightSizeToContentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TextEditor.SizeToContent = SizeToContent.WidthAndHeight;
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
}
