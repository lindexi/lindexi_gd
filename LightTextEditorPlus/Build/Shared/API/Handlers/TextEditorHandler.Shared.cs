#if USE_AllInOne || !USE_MauiGraphics && !USE_SKIA

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Editing;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils.Patterns;
using LightTextEditorPlus.Utils;

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
    /// 处理双击事件
    /// </summary>
    /// <param name="clickPoint"></param>
    /// <returns></returns>
    public virtual bool HandleDoubleClick(in TextPoint clickPoint)
    {
        // 默认行为是双击全选，你想选词？那就不好玩了哦
        TextEditor.TextEditorCore.SelectAll();
        // 选词需要分词算法，请参阅：
        // [UWP WinRT 使用系统自带的分词库对字符串文本进行分词](https://blog.lindexi.com/post/UWP-WinRT-%E4%BD%BF%E7%94%A8%E7%B3%BB%E7%BB%9F%E8%87%AA%E5%B8%A6%E7%9A%84%E5%88%86%E8%AF%8D%E5%BA%93%E5%AF%B9%E5%AD%97%E7%AC%A6%E4%B8%B2%E6%96%87%E6%9C%AC%E8%BF%9B%E8%A1%8C%E5%88%86%E8%AF%8D.html )
        // [dotnet 简单使用 ICU 库进行分词和分行 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18622917 )
        return true;
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
    public virtual void Delete() => TextEditorCore.Delete();

    /// <inheritdoc cref="TextEditorCore.Backspace"/>
    public virtual void Backspace() => TextEditorCore.Backspace();

    /// <summary>
    /// 输入 Insert 键的处理，切换插入/覆盖模式
    /// </summary>
    protected virtual void SwitchOvertypeMode()
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
        RawTextInput("\n");
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



    #endregion

    /// <summary>
    /// 移动光标
    /// </summary>
    /// <param name="type"></param>
    public virtual partial void MoveCaret(CaretMoveType type);
}
#endif
