using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;
using LightTextEditorPlus.Core.Primitive;

using TextVisionComparer;

using Path = System.IO.Path;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class TextEditorTestView : UserControl
{
    public TextEditorTestView()
    {
        InitializeComponent();
        TextEditorSettingsControl.TextEditor = TextEditor;
        _richTextCaseProvider = new RichTextCaseProvider(() => TextEditor);

        TestCaseListBox.ItemsSource = _richTextCaseProvider.RichTextCases;

        Loaded += TextEditorTestView_Loaded;

        TestCaseListBox.SelectionChanged += TestCaseListBox_SelectionChanged;
    }

    private void TestCaseListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 1)
        {
            if (e.AddedItems[0] is IRichTextCase richTextCase)
            {
                _richTextCaseProvider.Run(richTextCase);
                UpdateOriginImage();
            }
        }
    }

    private void TextEditorTestView_Loaded(object? sender, RoutedEventArgs e)
    {
        _richTextCaseProvider.Debug();

        UpdateOriginImage();
    }

    private readonly RichTextCaseProvider _richTextCaseProvider;

    private void SettingToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SettingBorder.IsVisible = SettingToggleButton.IsChecked is true;
    }

    private void ShowDiffToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        UpdateOriginImage();
    }

    private void DebugButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _richTextCaseProvider.Debug();
    }

    private async void UpdateOriginImage()
    {
        if (_richTextCaseProvider.CurrentRichTextCase is { } richTextCase)
        {
            var testFolder = Path.Join(AppContext.BaseDirectory, "Assets", "RichTextCaseImages");
            var imageFile = Path.Join(testFolder, $"{richTextCase.Name}.png");

            if (File.Exists(imageFile))
            {
                OriginImage.Source = new Bitmap(imageFile);

                while (TextEditor.IsDirty)
                {
                    await Task.Delay(100);
                }

                TextRect documentLayoutBounds = TextEditor.TextEditorCore.GetDocumentLayoutBounds();
                using RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(new PixelSize((int) documentLayoutBounds.Width, (int) documentLayoutBounds.Height));
                renderTargetBitmap.Render(TextEditor);
                var renderImageFile = Path.Join(AppContext.BaseDirectory, $"{richTextCase.Name}_{Path.GetRandomFileName()}.png");
                renderTargetBitmap.Save(renderImageFile);

                var visionComparer = new VisionComparer();
                var result = visionComparer.Compare(new FileInfo(imageFile), new FileInfo(renderImageFile));
                LogTextBlock.Text = result.ToString();
                if (result.Success)
                {
                    OriginImageDebugCanvas.Children.Clear();
                    DebugCanvas.Children.Clear();

                    if (ShowDiffOriginToggleButton.IsChecked is true)
                    {
                        DrawRect(OriginImageDebugCanvas);
                    }

                    if (ShowDiffToggleButton.IsChecked is true)
                    {
                        DrawRect(DebugCanvas);
                    }
                }

                void DrawRect(Canvas canvas)
                {
                    foreach (VisionCompareRect visionCompareRect in result.CompareRectList)
                    {
                        Rectangle rectangle = new Rectangle
                        {
                            Width = visionCompareRect.Width,
                            Height = visionCompareRect.Height,
                            Stroke = Brushes.Red,
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(rectangle, visionCompareRect.X);
                        Canvas.SetTop(rectangle, visionCompareRect.Y);
                        canvas.Children.Add(rectangle);
                    }
                }
            }
        }
    }

    private void ShowDiffOriginToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        UpdateOriginImage();
    }
}
