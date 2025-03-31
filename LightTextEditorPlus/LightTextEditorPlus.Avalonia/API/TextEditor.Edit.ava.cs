using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Events;
using Avalonia.Media;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Utils;
using ReactiveUI;
using SkiaSharp;
using Avalonia.Layout;
using Avalonia.Skia;

namespace LightTextEditorPlus
{
    // 此文件存放编辑相关的方法
    [APIConstraint("TextEditor.Edit.Style.txt")]
    [APIConstraint("TextEditor.Edit.Input.txt")]
    [APIConstraint("TextEditor.Edit.txt")]
    partial class TextEditor
    {
        #region Text

        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text
        {
            get
            {
                if (_cacheText is null)
                {
                    _cacheText = TextEditorCore.GetText();
                }

                return _cacheText;
            }
            set
            {
                _isSettingsTextProperty = true;
                TextEditorCore.EditAndReplace(value, TextEditorCore.GetAllDocumentSelection());
                _isSettingsTextProperty = false;
            }
        }

        private string? _cacheText;

        /// <summary>
        /// 文本内容属性
        /// </summary>
        public static readonly DirectProperty<TextEditor, string> TextProperty = AvaloniaProperty.RegisterDirect<TextEditor, string>(nameof(Text),
            editor => editor.Text, (editor, value) => editor.Text = value);

        /// <summary>
        /// 是否正在设置 Text 属性。正在设置 Text 属性时，不触发 TextProperty 变更事件
        /// </summary>
        private bool _isSettingsTextProperty;

        /// <summary>
        /// 是否应该触发 Text 属性变更事件
        /// </summary>
        /// <remarks>
        /// 由于 Avalonia 机制缺失，导致无法只触发 TextProperty 变更而延迟求值，再加上对 Text 求值是一个耗时操作，所以这里提供一个属性，用于控制是否触发 TextProperty 变更事件。默认为 false，不触发 TextProperty 变更事件。如果设置为 true，将会触发 TextProperty 变更事件，但会带来额外的性能开销
        /// </remarks>
        public bool ShouldRaiseTextPropertyChanged { get; set; } = false;

        private void TextEditorCore_TextChanged(object? sender, EventArgs e)
        {
            if (ShouldRaiseTextPropertyChanged)
            {
                if (!_isSettingsTextProperty)
                {
                    // Can I raise the DirectProperty changed without set the value?
                    // One of my properties can be bound, but most of the time, users don't pay attention to this property, meaning that changes to this property are not of interest to all users. However, assigning a value to this property is time-consuming. I would like to simply notify about the change of this property and perform a lazy evaluation when the property's getter is accessed. I noticed that in Avalonia there are only SetAndRaise and RaisePropertyChanged methods, both of which require the new value to be passed immediately. Is there a way in Avalonia to notify about a property change without having to assign the value immediately?
                    _cacheText = null;
                    SetAndRaise(TextProperty, ref _cacheText!, TextEditorCore.GetText());
                }
            }
        }

        #endregion

        #region 编辑模式

        /// <summary>
        /// 是否进入用户编辑模式。进入用户编辑模式将闪烁光标，支持输入法输入
        /// </summary>
        public bool IsInEditingInputMode
        {
            set
            {
                if (_isInEditingInputMode == value)
                {
                    return;
                }

                if (value is true && IsEditable is false)
                {
                    Logger.LogDebug("设置进入用户编辑模式，但当前文本禁用编辑，设置进入用户编辑模式失效");

                    value = false;
                }

                //EnsureEditInit();

                Logger.LogDebug(value ? "进入用户编辑模式" : "退出用户编辑模式");

                _isInEditingInputMode = value;

                if (value)
                {
                    Focus();
                }

                IsInEditingInputModeChanged?.Invoke(this, EventArgs.Empty);

                InvalidateVisual();
            }
            get => _isInEditingInputMode;
        }

        private bool _isInEditingInputMode = false;

        /// <summary>
        /// 是否进入编辑的模式变更完成事件
        /// </summary>
        public event EventHandler? IsInEditingInputModeChanged;

        /// <summary>
        /// 是否自动根据是否获取焦点设置是否进入编辑模式
        /// </summary>
        public bool IsAutoEditingModeByFocus { get; set; } = true;

        #endregion

        #region Style

        #region RunProperty

        /// <inheritdoc cref="LightTextEditorPlus.Core.Document.DocumentManager.CurrentCaretRunProperty"/>
        public SkiaTextRunProperty CurrentCaretRunProperty => (SkiaTextRunProperty) TextEditorCore.DocumentManager.CurrentCaretRunProperty;

        /// <inheritdoc cref="DocumentManager.StyleRunProperty"/>
        public SkiaTextRunProperty StyleRunProperty => (SkiaTextRunProperty) TextEditorCore.DocumentManager.StyleRunProperty;

        /// <summary>
        /// 创建一个新的 RunProperty 对象
        /// </summary>
        /// <param name="createRunProperty">传入默认的 <see cref="StyleRunProperty"/> 字符属性，返回创建的新的字符属性。方法等同于直接拿 <see cref="StyleRunProperty"/> 字符属性带 with 关键字创建新的属性</param>
        /// <returns></returns>
        /// 这是一个多余的方法，但是可以比较方便让大家找到创建字符属性的方法
        public SkiaTextRunProperty CreateRunProperty(CreateRunProperty createRunProperty) =>
            createRunProperty(StyleRunProperty);

        /// <inheritdoc cref="DocumentManager.SetStyleTextRunProperty{T}"/>
        public void SetStyleTextRunProperty(ConfigRunProperty config)
        {
            TextEditorCore.DocumentManager.SetStyleTextRunProperty((SkiaTextRunProperty property) => config(property));
        }

        /// <inheritdoc cref="DocumentManager.SetCurrentCaretRunProperty{T}"/>
        public void SetCurrentCaretRunProperty(ConfigRunProperty config)
            => TextEditorCore.DocumentManager.SetCurrentCaretRunProperty((SkiaTextRunProperty property) => config(property));

        /// <summary>
        /// 设置字体名
        /// </summary>
        /// <param name="fontName">如果字体不存在，将会自动回滚</param>
        /// <param name="selection"></param>
        public void SetFontName(string fontName, Selection? selection = null)
            => SetFontName(new FontName(fontName), selection);

        /// <summary>
        /// 设置字体名
        /// </summary>
        /// <param name="fontName">如果字体不存在，将会自动回滚</param>
        /// <param name="selection"></param>
        public void SetFontName(FontName fontName, Selection? selection = null)
        {
            SetRunProperty(p => p with { FontName = fontName }, PropertyType.FontName, selection);
        }

        /// <summary>
        /// 设置字体大小
        /// </summary>
        /// <param name="fontSize">使用Avalonia单位</param>
        /// <param name="selection"></param>
        public void SetFontSize(double fontSize, Selection? selection = null)
        {
            SetRunProperty(p => p with { FontSize = fontSize }, PropertyType.FontSize, selection);
        }

        /// <summary>
        /// 设置前景色
        /// </summary>
        /// <param name="foreground"></param>
        /// <param name="selection"></param>
        public void SetForeground(SolidColorBrush foreground, Selection? selection = null)
        {
            SetForeground(foreground.Color.ToSKColor(), selection);
        }

        /// <inheritdoc cref="SetForeground(SKColor,Selection?)"/>
        public void SetForeground(IImmutableSolidColorBrush foreground, Selection? selection = null)
        {
            SetForeground(foreground.Color.ToSKColor(), selection);
        }

        /// <summary>
        /// 设置前景色
        /// </summary>
        /// <param name="foreground"></param>
        /// <param name="selection"></param>
        public void SetForeground(SKColor foreground, Selection? selection = null)
        {
            SetRunProperty(p => p with { Foreground = foreground }, PropertyType.Foreground, selection);
        }

        /// <summary>
        /// 开启或关闭文本斜体
        /// </summary>
        public void ToggleItalic(Selection? selection = null)
        {
            FontStyle fontStyle;

            SKFontStyleSlant normalStyle = FontStyle.Normal.ToSKFontStyleSlant();
            if (AreAllRunPropertiesMatch(property => property.FontStyle == normalStyle, selection))
            {
                // 字体倾斜 Italic 和 Oblique 的差别
                // 使用 Italic 是字体提供的斜体，可以和正常字体有不同的界面
                // 使用 Oblique 只是将正常的字体进行倾斜
                // 如果一个字体没有斜体，那 Italic 和 Oblique 视觉效果相同
                // 详细请看 [WPF 字体 FontStyle 的 Italic 和 Oblique 的区别](https://blog.lindexi.com/post/WPF-%E5%AD%97%E4%BD%93-FontStyle-%E7%9A%84-Italic-%E5%92%8C-Oblique-%E7%9A%84%E5%8C%BA%E5%88%AB.html )
                fontStyle = FontStyle.Italic;
            }
            else
            {
                fontStyle = FontStyle.Normal;
            }

            SetFontStyle(fontStyle, selection);
        }

        /// <summary>
        /// 设置字体样式
        /// </summary>
        /// <param name="fontStyle"></param>
        /// <param name="selection"></param>
        public void SetFontStyle(FontStyle fontStyle, Selection? selection = null)
        {
            SetRunProperty(p => p with { FontStyle = fontStyle.ToSKFontStyleSlant() }, PropertyType.FontStyle, selection);
        }

        /// <summary>
        /// 开启或关闭文本加粗
        /// </summary>
        public void ToggleBold(Selection? selection = null)
        {
            FontWeight fontWeight;
            SKFontStyleWeight normalWeight = FontWeight.Normal.ToSKFontStyleWeight();
            if (AreAllRunPropertiesMatch(property => property.FontWeight == normalWeight, selection))
            {
                fontWeight = FontWeight.Bold;
            }
            else
            {
                fontWeight = FontWeight.Normal;
            }

            SetFontWeight(fontWeight, selection);
        }

        private bool AreAllRunPropertiesMatch(Predicate<SkiaTextRunProperty> predicate, Selection? selection)
        {
            return TextEditorCore.DocumentManager.AreAllRunPropertiesMatch(predicate, selection);
        }

        /// <summary>
        /// 设置字重
        /// </summary>
        /// <param name="fontWeight"></param>
        /// <param name="selection">如果未设置，将采用当前文本选择。文本未选择则设置当前光标属性</param>
        public void SetFontWeight(FontWeight fontWeight, Selection? selection = null)
        {
            SetRunProperty(p => p with { FontWeight = fontWeight.ToSKFontStyleWeight() }, PropertyType.FontWeight, selection);
        }

        /// <summary>
        /// 设置字符属性。如果传入的 <paramref name="selection"/> 是空，将会使用当前选择。当前选择是空将会修改当前光标字符属性。修改当前光标字符属性样式，只触发 StyleChanging 和 StyleChanged 事件，不触发布局变更
        /// </summary>
        /// <param name="config"></param>
        /// <param name="property"></param>
        /// <param name="selection"></param>
        internal void SetRunProperty(ConfigRunProperty config, PropertyType property, Selection? selection)
        {
            // 如果是在编辑模式，那就使用当前选择。如果非编辑模式，且当前没有选择任何内容，那就是对整个文本
            if (IsInEditingInputMode)
            {
                // 如果是在编辑模式，那就使用当前选择
                selection ??= TextEditorCore.CurrentSelection;
            }
            else
            {
                // 如果非编辑模式，且当前没有选择任何内容，那就是对整个文本
                if (selection is null)
                {
                    selection = TextEditorCore.GetAllDocumentSelection();
                }
                else
                {
                    // 有传入的话，使用传入的选择范围
                }
            }

            if (!selection.Value.IsEmpty)
            {
                // 选择范围不为空，那就是一定有变更内容，记录布局变更原因
                TextEditorCore.AddLayoutReason($"SetRunPropertyAva PropertyType={property} Selection={selection.Value.FrontOffset.Offset}-{selection.Value.BehindOffset.Offset}");
            }

            OnStyleChanging(new StyleChangeEventArgs(selection.Value, property, TextEditorCore.IsUndoRedoMode));

            TextEditorCore.DocumentManager.SetRunProperty<SkiaTextRunProperty>(runProperty => config(runProperty), selection);

            OnStyleChanged(new StyleChangeEventArgs(selection.Value, property, TextEditorCore.IsUndoRedoMode));
        }

        #endregion

        #region 文本属性

        /// <summary>
        /// 获取或设置文本的垂直对齐方式 
        /// </summary>
        public VerticalAlignment VerticalTextAlignment
        {
            get => TextEditorCore.VerticalTextAlignment switch
            {
                LightTextEditorPlus.Core.Primitive.VerticalTextAlignment.Top => VerticalAlignment.Top,
                LightTextEditorPlus.Core.Primitive.VerticalTextAlignment.Center => VerticalAlignment.Center,
                LightTextEditorPlus.Core.Primitive.VerticalTextAlignment.Bottom => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Top
            };
            set => TextEditorCore.VerticalTextAlignment = value switch
            {
                VerticalAlignment.Top => LightTextEditorPlus.Core.Primitive.VerticalTextAlignment.Top,
                VerticalAlignment.Center => LightTextEditorPlus.Core.Primitive.VerticalTextAlignment.Center,
                VerticalAlignment.Bottom => LightTextEditorPlus.Core.Primitive.VerticalTextAlignment.Bottom,
                // 文本里面没有 Stretch 的概念，映射为 Top 算了
                VerticalAlignment.Stretch => LightTextEditorPlus.Core.Primitive.VerticalTextAlignment.Top,
                _ => Core.Primitive.VerticalTextAlignment.Top
            };
        }

        /// <summary>
        /// 获取或设置文本的垂直对齐方式。此属性仅仅只是为了兼容其他控件的设置属性而已，更加正确的是使用 <see cref="VerticalTextAlignment"/> 属性。此属性和 <see cref="VerticalTextAlignment"/> 完全等价
        /// </summary>
        /// <remarks>完全等价于 <see cref="VerticalTextAlignment"/> 属性</remarks>
        public VerticalAlignment VerticalContentAlignment
        {
            get => VerticalTextAlignment;
            [Obsolete("当前还没实现，请不要调用")]
            set => VerticalTextAlignment = value;
        }

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.SizeToContent"/>
        public SizeToContent SizeToContent
        {
            get => TextEditorCore.SizeToContent switch
            {
                TextSizeToContent.Width => SizeToContent.Width,
                TextSizeToContent.Height => SizeToContent.Height,
                TextSizeToContent.Manual => SizeToContent.Manual,
                TextSizeToContent.WidthAndHeight => SizeToContent.WidthAndHeight,
                var t => (SizeToContent) t,
            };
            set
            {
                if (SizeToContent == value)
                {
                    return;
                }

                TextEditorCore.SizeToContent = value switch
                {
                    SizeToContent.Width => TextSizeToContent.Width,
                    SizeToContent.Height => TextSizeToContent.Height,
                    SizeToContent.Manual => TextSizeToContent.Manual,
                    SizeToContent.WidthAndHeight => TextSizeToContent.WidthAndHeight,
                    var t => (TextSizeToContent) t,
                };

                InvalidateMeasure();
            }
        }

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.ArrangingType"/>
        public ArrangingType ArrangingType
        {
            set => TextEditorCore.ArrangingType = value;
            get => TextEditorCore.ArrangingType;
        }

        #endregion

        #endregion

        #region 输入

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.AppendText"/>
        public void AppendText(string text) => SkiaTextEditor.AppendText(text);

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.AppendRun"/>
        public void AppendRun(SkiaTextRun textRun) => SkiaTextEditor.AppendRun(textRun);

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.EditAndReplace"/>
        public void EditAndReplace(string text, Selection? selection = null) => SkiaTextEditor.EditAndReplace(text, selection);

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.EditAndReplaceRun"/>
        public void EditAndReplaceRun(SkiaTextRun textRun, Selection? selection = null) => SkiaTextEditor.EditAndReplaceRun(textRun, selection);

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Backspace"/>
        public void Backspace() => SkiaTextEditor.Backspace();

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Delete"/>
        public void Delete() => SkiaTextEditor.Delete();

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Remove"/>
        public void Remove(in Selection selection) => SkiaTextEditor.Remove(in selection);

        #endregion

        #region 事件
        /// <summary>
        /// 当设置样式时触发
        /// </summary>
        public event EventHandler<StyleChangeEventArgs>? StyleChanging;

        /// <summary>
        /// 当设置样式后触发
        /// </summary>
        public event EventHandler<StyleChangeEventArgs>? StyleChanged;

        internal void OnStyleChanging(StyleChangeEventArgs styleChangeEventArgs)
        {
            StyleChanging?.Invoke(this, styleChangeEventArgs);
        }

        internal void OnStyleChanged(StyleChangeEventArgs styleChangeEventArgs)
        {

            StyleChanged?.Invoke(this, styleChangeEventArgs);
        }

        /// <summary>
        /// 请使用 AvaloniaLayoutUpdated 事件。此事件为 Avalonia 框架里的控件布局更新事件，不是文本库的排版更新事件 Occurs when a layout pass completes for the control. 
        /// </summary>
        [Obsolete("请使用 AvaloniaLayoutUpdated 事件。此事件为 Avalonia 框架里的控件布局更新事件，不是文本库的排版更新事件")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler LayoutUpdated
        {
            add => base.LayoutUpdated += value;
            remove => base.LayoutUpdated -= value;
        }

        /// <summary>
        /// Occurs when a layout pass completes for the control. 
        /// </summary>
        public event EventHandler AvaloniaLayoutUpdated
        {
            add => base.LayoutUpdated += value;
            remove => base.LayoutUpdated -= value;
        }
        #endregion
    }
}

public delegate SkiaTextRunProperty ConfigRunProperty(SkiaTextRunProperty runProperty);

public delegate SkiaTextRunProperty CreateRunProperty(SkiaTextRunProperty styleRunProperty);
