using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Utils;

using Microsoft.Win32;

namespace LightTextEditorPlus.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            TextEditor.SetFontSize(60);
            TextEditor.SetFontName("华文中宋");
            TextEditor.SetFontName("微软雅黑");
            TextEditor.SetFontName("Javanese Text");
            TextEditor.TextEditorCore.AppendText("123123");
            ParagraphIndex paragraphIndex = new ParagraphIndex(0);
            TextEditor.TextEditorCore.DocumentManager.SetParagraphProperty(paragraphIndex, TextEditor.TextEditorCore.DocumentManager.GetParagraphProperty(paragraphIndex) with
            {
                LeftIndentation = 5
            });

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var fontFamily = new FontFamily("微软雅黑");
            Typeface typeface = fontFamily.GetTypefaces().First();

            var textRunProperties = new SimpleTextRunProperties(15, Brushes.Black, typeface);
            var text = "abc1,abc";
            var simpleTextSource = new SimpleTextSource(text, textRunProperties);

            var simpleTextParagraphProperties = new SimpleTextParagraphProperties(textRunProperties, firstLineInParagraph: true, FlowDirection.LeftToRight, indent: 0, lineHeight: 0, TextAlignment.Left, null!, TextWrapping.Wrap);

            using var formatter = TextFormatter.Create(TextFormattingMode.Ideal);

            var textStorePosition = 0;
            Point linePosition = default;

            while (textStorePosition < text.Length)
            {
                using TextLine textLine = formatter.FormatLine(simpleTextSource, textStorePosition, paragraphWidth: 30, simpleTextParagraphProperties, previousLineBreak: null);

                textStorePosition += textLine.Length;
                linePosition.Y += textLine.Height;
            }
        }

        class SimpleTextParagraphProperties : TextParagraphProperties
        {
            public SimpleTextParagraphProperties(TextRunProperties defaultTextRunProperties, bool firstLineInParagraph, FlowDirection flowDirection, double indent, double lineHeight, TextAlignment textAlignment, TextMarkerProperties textMarkerProperties, TextWrapping textWrapping)
            {
                DefaultTextRunProperties = defaultTextRunProperties;
                FirstLineInParagraph = firstLineInParagraph;
                FlowDirection = flowDirection;
                Indent = indent;
                LineHeight = lineHeight;
                TextAlignment = textAlignment;
                TextMarkerProperties = textMarkerProperties;
                TextWrapping = textWrapping;
            }

            public override TextRunProperties DefaultTextRunProperties { get; }
            public override bool FirstLineInParagraph { get; }
            public override FlowDirection FlowDirection { get; }
            public override double Indent { get; }
            public override double LineHeight { get; }
            public override TextAlignment TextAlignment { get; }
            public override TextMarkerProperties TextMarkerProperties { get; }
            public override TextWrapping TextWrapping { get; }
        }

        class SimpleTextRunProperties : TextRunProperties
        {
            public SimpleTextRunProperties(double fontSize, Brush foregroundBrush, Typeface typeface)
            {
                FontRenderingEmSize = fontSize;
                FontHintingEmSize = fontSize;

                ForegroundBrush = foregroundBrush;
                Typeface = typeface;
            }

            public override Brush BackgroundBrush => Brushes.Transparent;
            public override CultureInfo CultureInfo => CultureInfo.CurrentUICulture;
            public override double FontHintingEmSize { get; }
            public override double FontRenderingEmSize { get; }
            public override Brush ForegroundBrush { get; }
            public override TextDecorationCollection? TextDecorations { get; }
            public override TextEffectCollection? TextEffects { get; }
            public override Typeface Typeface { get; }
        }

        /// <summary>
        /// 提供一个抽象类，用于指定要由 TextFormatter 对象使用的字符数据和格式设置属性。
        /// 对 TextSource 对象中文本的所有访问都是通过 GetTextRun 方法执行的，该方法旨在允许文本布局客户端按它选择的任意方式虚拟化文本。
        /// TextFormatter 是一种 WPF 文本引擎，用于提供文本格式设置和换行服务。 
        /// TextFormatter 以处理各种文本字符格式和段落样式，并提供对国际文本布局的支持。 
        /// 与传统文本 API 不同的是，TextFormatter 通过一组回调方法来与文本布局客户端交互。 
        /// 它要求客户端在 TextSource 类的实现中提供这些方法。 
        /// 下面介绍了必须重写的三个成员：
        ///     GetTextRun ：检索从指定的 TextSource 位置处开始的 TextRun。
        ///     GetPrecedingText ：检索紧邻指定的 TextSource 位置之前的文本跨距。
        ///     GetTextEffectCharacterIndexFromTextSourceCharacterIndex ：检索一个值，该值将 TextSource 字符索引映射到 TextEffect 字符索引。
        /// 
        /// 这是一个简单的文本源，使用给定的文本及格式供TextFormatter使用
        /// </summary>
        class SimpleTextSource : TextSource
        {
            /// <summary>
            /// 需要渲染的文本
            /// </summary>
            private readonly string _text;
            /// <summary>
            /// 指定渲染<see cref="_text"/>的方式
            /// </summary>
            private readonly TextRunProperties _properties;

            /// <summary>
            /// 使用指定文本和渲染方式创建文本源实例
            /// </summary>
            /// <param name="text">需要渲染的文本</param>
            /// <param name="properties">指定渲染<see cref="_text"/>的方式</param>
            public SimpleTextSource(string text, TextRunProperties properties)
            {
                _text = text;
                _properties = properties;
            }

            /// <summary>
            /// 检索紧邻指定的 TextSource 位置之前的文本跨距。
            /// 如果紧邻 textSourceCharacterIndexLimit 之前的文本跨距未包含任何文本（如内联对象或隐藏运行等），
            /// 则 CultureSpecificCharacterBufferRange 方法会返回一个空 GetPrecedingText。
            /// 如果 textSourceCharacterIndexLimit 之前没有任何值，则此方法会返回一个零长度的文本跨距。
            /// </summary>
            /// <param name="textSourceCharacterIndexLimit">一个 Int32 值，该值指定停止文本检索的字符索引位置。</param>
            /// <returns>一个 CultureSpecificCharacterBufferRange 值，表示紧邻 textSourceCharacterIndexLimit 之前的文本跨距。</returns>
            public override TextSpan<CultureSpecificCharacterBufferRange>? GetPrecedingText(int textSourceCharacterIndexLimit)
            {
                return null;
            }
            /// <summary>
            /// 检索一个值，该值将 TextSource 字符索引映射到 TextEffect 字符索引。
            /// </summary>
            /// <param name="textSourceCharacterIndex">一个 Int32 值，指定要映射的 TextSource 字符索引。</param>
            /// <returns>一个 Int32 值，表示 TextEffect 字符索引。</returns>
            public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
            {
                return -1;
            }

            /// <summary>
            /// 检索从指定的 TextSource 位置处开始的 TextRun。
            /// 文本运行是共享单个属性集的字符序列。
            /// 对格式（如字体系列、字体样式、前景色、文本修饰或其他任何格式设置效果）的任何更改都会断开文本运行。 
            /// TextRun 类是一个类型层次结构的根，该层次结构表示由 TextFormatter 处理的多种类型的文本内容。 
            /// 派生自 TextRun 的每个类都表示不同类型的文本内容。
            /// 类说明:
            ///     TextRun 层次结构的根。 定义一组共享相同的字符属性集的字符。
            ///     TextCharacters 根据不同的物理字样定义字符标志符号的集合。
            ///     TextEmbeddedObject 定义一种文本内容，其中的度量、命中测试以及整个内容的绘制都将作为独立的实体执行。 位于文本行中间的按钮就是这种内容类型的一个示例。
            ///     TextEndOfLine 定义换行符代码。
            ///     TextEndOfParagraph 定义分段符代码。 派生自 TextEndOfLine。
            ///     TextEndOfSegment 定义分节标记。
            ///     TextHidden 定义一系列不可见字符。
            ///     TextModifier 定义修改范围的开头。
            /// </summary>
            /// <param name="textSourceCharacterIndex">指定 TextSource 中检索到 TextRun 的字符索引位置。</param>
            /// <returns>一个值，表示 TextRun 或派生自 TextRun 的对象。</returns>
            public override TextRun GetTextRun(int textSourceCharacterIndex)
            {
                if (textSourceCharacterIndex >= _text.Length)
                {
                    return new TextEndOfParagraph(1);
                }
                return new TextCharacters(_text, textSourceCharacterIndex, _text.Length - textSourceCharacterIndex, _properties);
            }
        }

        //private void InputButton_OnClick(object sender, RoutedEventArgs e)
        //{
        //    TextEditor.TextEditorCore.EditAndReplace(TextBox.Text);
        //}

        private async void DebugButton_OnClick(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS0618
            TextEditor.TextEditorCore.Clear();

            // 给调试使用的按钮，可以在这里编写调试代码
            var count = 0;
            while (count >= 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    TextEditor.TextEditorCore.AppendText(((char) Random.Shared.Next('a', 'z')).ToString());
                    await Task.Delay(10);
                }

                TextEditor.TextEditorCore.AppendText("\r\n");
                await Task.Delay(10);

                count++;

                if (count == 10)
                {
                    TextEditor.TextEditorCore.Clear();

                    count = 0;
                }
            }
#pragma warning restore CS0618
        }

        //private void BackspaceButton_OnClick(object sender, RoutedEventArgs e)
        //{
        //    TextEditor.TextEditorCore.Backspace();
        //}

        private void ShowDocumentBoundsButton_OnClick(object sender, RoutedEventArgs e)
        {
            TextEditor.TextEditorCore.LayoutCompleted -= TextEditorCore_LayoutCompleted;
            RemoveDocumentBoundsDebugBorder();

            if (ShowDocumentBoundsButton.IsChecked is true)
            {
                TextEditor.TextEditorCore.LayoutCompleted += TextEditorCore_LayoutCompleted;
                ShowDocumentBoundsDebugBorder();
            }

            // ReSharper disable once InconsistentNaming
            void TextEditorCore_LayoutCompleted(object? _, LayoutCompletedEventArgs __)
            {
                RemoveDocumentBoundsDebugBorder();
                ShowDocumentBoundsDebugBorder();
            }

            void ShowDocumentBoundsDebugBorder()
            {
                var documentLayoutBounds = TextEditor.TextEditorCore.GetDocumentLayoutBounds();
                var documentBoundsDebugBorder = new DocumentBoundsDebugBorder()
                {
                    Width = documentLayoutBounds.Width,
                    Height = documentLayoutBounds.Height,

                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Cyan,
                };

                DebugCanvas.Children.Add(documentBoundsDebugBorder);
            }

            void RemoveDocumentBoundsDebugBorder()
            {
                var documentBoundsDebugBorder = DebugCanvas.Children.OfType<DocumentBoundsDebugBorder>().FirstOrDefault();
                if (documentBoundsDebugBorder != null)
                {
                    DebugCanvas.Children.Remove(documentBoundsDebugBorder);
                }
            }
        }
    }
}

class DocumentBoundsDebugBorder : Border
{
}
