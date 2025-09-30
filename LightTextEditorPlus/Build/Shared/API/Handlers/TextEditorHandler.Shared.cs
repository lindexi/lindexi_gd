#if USE_AllInOne || !USE_MauiGraphics && !USE_SKIA

using System;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Editing;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils.Patterns;
using LightTextEditorPlus.Utils;

using System.Diagnostics;
using LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Editing;

/// <summary>
/// 文本编辑器的交互处理器
/// </summary>
/// 这个类型的作用在于方便业务端重写，用于控制一些交互行为
/// 例如：鼠标、键盘、文本输入、剪贴板等
// todo 考虑命名为 interaction 交互处理器
public partial class TextEditorHandler
{
    /// <summary>
    /// 创建文本编辑器的交互处理器
    /// </summary>
    /// <param name="textEditor"></param>
    public TextEditorHandler(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    private TextEditor TextEditor { get; }
    private TextEditorCore TextEditorCore => TextEditor.TextEditorCore;

    #region 鼠标相关

    /// <summary>
    /// 处理单击事件
    /// </summary>
    /// <param name="clickPoint"></param>
    /// <returns></returns>
    protected virtual bool HandleSingleClick(in TextPoint clickPoint)
    {
        TextEditor
            .TextEditorPlatformProvider
            .EnsureLayoutUpdated();
        if (TextEditorCore.TryHitTest(in clickPoint, out var result))
        {
            // 是否命中到选择。条件： 当前有选择（!TextEditorCore.CurrentSelection.IsEmpty），且选择内容包含命中的光标（TextEditorCore.CurrentSelection.Contains(result.HitCaretOffset)）
            _isHitSelection = !TextEditorCore.CurrentSelection.IsEmpty && TextEditorCore.CurrentSelection.Contains(result.HitCaretOffset);

            if (!_isHitSelection)
            {
                // 没有命中到选择，那就设置当前光标
                TextEditorCore.CurrentCaretOffset = result.HitCaretOffset;
            }
            return true;
        }
        else
        {
            Debug.Fail("理论上一定能命中成功");
            return false;
        }
    }

    /// <summary>
    /// 处理双击事件
    /// </summary>
    /// <param name="clickPoint"></param>
    /// <returns></returns>
    protected virtual bool HandleDoubleClick(in TextPoint clickPoint)
    {
        // 默认行为是双击全选，你想选词？那就不好玩了哦
        TextEditor.TextEditorCore.SelectAll();
        // 选词需要分词算法，请参阅：
        // [UWP WinRT 使用系统自带的分词库对字符串文本进行分词](https://blog.lindexi.com/post/UWP-WinRT-%E4%BD%BF%E7%94%A8%E7%B3%BB%E7%BB%9F%E8%87%AA%E5%B8%A6%E7%9A%84%E5%88%86%E8%AF%8D%E5%BA%93%E5%AF%B9%E5%AD%97%E7%AC%A6%E4%B8%B2%E6%96%87%E6%9C%AC%E8%BF%9B%E8%A1%8C%E5%88%86%E8%AF%8D.html )
        // [dotnet 简单使用 ICU 库进行分词和分行 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18622917 )
        return true;
    }

    /// <summary>
    /// 获取传入光标所在的单词选择范围
    /// </summary>
    /// <returns></returns>
    /// 此方法允许业务端重写，以注入分词算法
    protected virtual Selection GetCaretWord(in CaretOffset caretOffset)
    {
        IWordDivider wordDivider = TextEditor.TextEditorPlatformProvider.GetWordDivider();
        var result = wordDivider.GetCaretWord(new GetCaretWordArgument(caretOffset, TextEditorCore));
        return result.WordSelection;
    }

    /// <summary>
    /// 处理拖拽文本。拖动当前选择的文本到别处
    /// </summary>
    /// <param name="textPoint"></param>
    /// <returns></returns>
    protected virtual bool HandleDragText(in TextPoint textPoint)
    {
        // todo HandleDragText(); 拖拽文本支持
        return false;
    }

    /// <summary>
    /// 处理拖拽选择
    /// </summary>
    /// <param name="textPoint"></param>
    /// <returns></returns>
    protected virtual bool HandleDragSelect(in TextPoint textPoint)
    {
        if (_inputGesture.ClickCount % 2 == 0)
        {
            // 双击不处理拖动
            return false;
        }

        var startOffset = TextEditorCore.CurrentSelection.StartOffset;
        if (TextEditorCore.TryHitTest(textPoint, out var result))
        {
            if (result.IsOutOfTextCharacterBounds)
            {
                // 如果拖动过程超过文本了，那应该忽略，而不是获取文档末尾的 HitCaretOffset 值
            }
            else
            {
                var endOffset = result.HitCaretOffset;
                TextEditorCore.CurrentSelection = new Selection(startOffset, endOffset);
            }

            return true;
        }
        else
        {
            Debug.Fail("理论上一定能命中成功");
            return false;
        }
    }

    private bool _isMouseDown;

    /// <summary>
    /// 是不是点到选择范围
    /// </summary>
    private bool _isHitSelection;

    #region InputGestureInfo

    private readonly InputGestureInfo _inputGesture = new InputGestureInfo();

    #endregion

    #endregion

    #region 键盘相关

    /// <inheritdoc cref="TextEditorCore.Delete"/>
    protected internal virtual void Delete() => TextEditorCore.Delete();

    /// <inheritdoc cref="TextEditorCore.Backspace"/>
    protected internal virtual void Backspace() => TextEditorCore.Backspace();

    /// <inheritdoc cref="SwitchOvertypeMode"/>
    /// <remarks>完全等同于 <see cref="SwitchOvertypeMode"/> 方法</remarks>
    protected internal virtual void ToggleInsert() => SwitchOvertypeMode();

    /// <summary>
    /// 输入 Insert 键的处理，切换插入/覆盖模式
    /// </summary>
    /// <remarks>完全等同于 <see cref="ToggleInsert"/> 方法</remarks>
    protected internal virtual void SwitchOvertypeMode()
    {
        if (TextEditor.CheckFeaturesDisableWithLog(TextFeatures.OvertypeModeEnable))
        {
            return;
        }

        TextEditor.IsOvertypeMode = !TextEditor.IsOvertypeMode;
    }

    #endregion

    #region 文本输入

    /// <summary>
    /// 换行
    /// </summary>
    protected virtual void BreakLine()
    {
        PerformInput("\n");
    }

    /// <summary>
    /// 换行
    /// </summary>
    [Obsolete($"这个方法的存在是用来告诉你，正确的调用方式应该是调用 {nameof(BreakLine)} 方法", true)]
    public void AddNewLine()
    {
        BreakLine();
    }

    /// <summary>
    /// 换行
    /// </summary>
    [Obsolete($"这个方法的存在是用来告诉你，正确的调用方式应该是调用 {nameof(BreakLine)} 方法", true)]
    public void InsertNewLine()
    {
        BreakLine();
    }

    /// <summary>
    /// 收到原始文本输入，可能此时需要考虑处理 Emoji 等情况。可考虑只重写 <see cref="PerformInput"/> 方法，获取更上层的支持
    /// </summary>
    /// <param name="text"></param>
    protected virtual void RawTextInput(string text)
    {
        //如果是由两个Unicode码组成的Emoji的其中一个Unicode码，则等待第二个Unicode码的输入后合并成一个字符串作为一个字符插入
        if (RegexPatterns.Utf16SurrogatesPattern.ContainInRange(text))
        {
            if (string.IsNullOrEmpty(_emojiCache))
            {
                _emojiCache += text;
            }
            else
            {
                _emojiCache += text;

                PerformInput(_emojiCache);
                _emojiCache = string.Empty;
            }
        }
        else
        {
            _emojiCache = string.Empty;
            PerformInput(text);
        }
    }

    /// <summary>
    /// 处理输入的文本，直接插入到文本中
    /// </summary>
    /// <param name="text"></param>
    protected virtual void PerformInput(string text)
    {
        Selection? selection = null;
        if (TextEditor.IsOvertypeMode)
        {
            selection = TextEditorCore.GetCurrentOvertypeModeSelection(text.Length);
        }

        TextEditorCore.EditAndReplace(text, selection);
    }

    /// <summary>
    /// 如果是由两个Unicode码组成的Emoji的其中一个Unicode码，则等待第二个Unicode码的输入后合并成一个字符串作为一个字符插入
    /// 用于接收第一个字符
    /// </summary>
    private string _emojiCache = string.Empty;
    #endregion

    #region 剪贴板

    /// <summary>
    /// 当粘贴纯文本
    /// </summary>
    /// <param name="text"></param>
    protected void OnPastePlainText(string text)
    {
        PerformInput(text);
    }

    #endregion

    /// <summary>
    /// 移动光标
    /// </summary>
    /// <param name="type"></param>
    protected internal virtual partial void MoveCaret(CaretMoveType type);
}
#endif
