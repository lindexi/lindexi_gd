using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Threading;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Layers.Caret_;

/// <summary>
/// 选择和光标的一层
/// </summary>
/// 状态：
/// 光标模式->选择模式：切换选择范围
/// 光标模式->非编辑模式：隐藏光标
/// 选择模式->选择范围变更：更新选择范围
/// 选择模式->光标模式：不显示选择范围，显示为光标
/// 选择模式->光标模式->立即进入非编辑模式：删除选择范围
class SelectionAndCaretLayer : DrawingVisual, ICaretManager, ILayer
{
    public SelectionAndCaretLayer(TextEditor textEditor)
    {
        _textEditor = textEditor;
        textEditor.TextEditorCore.CurrentCaretOffsetChanged += (sender, args) =>
        {
            if (_isBlinkShown)
            {
                HideBlink();
                _isBlinkShown = false;
            }

            if (!_textEditor.IsInEditingInputMode || !_textEditor.TextEditorCore.CurrentSelection.IsEmpty)
            {
                return;
            }

            StartBlink();
        };

        textEditor.TextEditorCore.CurrentSelectionChanged += (sender, args) =>
        {
            var currentSelection = _textEditor.TextEditorCore.CurrentSelection;

            if (currentSelection.IsEmpty)
            {
                _textEditor.Logger.LogDebug($"选择变更为空，回到光标闪烁");
                // 回到光标闪烁
                if (!_textEditor.IsInEditingInputMode)
                {
                    StartBlink();
                }
            }
            else
            {
                ShowSelection(_textEditor.TextEditorCore.CurrentSelection);
                _caretBlinkTimer?.Stop();
            }
        };

        textEditor.IsInEditingInputModeChanged += (sender, args) =>
        {
            if (_textEditor.IsInEditingInputMode)
            {
                StartBlink();
            }
        };
    }

    private readonly TextEditor _textEditor;

    /// <summary>
    /// 更新光标和选择
    /// </summary>
    public void UpdateSelectionAndCaret(RenderInfoProvider renderInfoProvider)
    {
        var currentSelection = _textEditor.TextEditorCore.CurrentSelection;
        if (currentSelection.IsEmpty)
        {
            // 闪烁光标
            if (!_textEditor.IsInEditingInputMode)
            {
                // 如果没有在编辑模式，那就不需要闪烁光标，但是需要隐藏光标
                if (_isBlinkShown || _isShowingSelection)
                {
                    StopBlink();
                }
            }
            else
            {
                _renderInfoProvider = renderInfoProvider;

                StartBlink();
            }
        }
        else
        {
            ShowSelection(currentSelection);

            _caretBlinkTimer?.Stop();
        }
    }

    private RenderInfoProvider? _renderInfoProvider;

    #region Caret

    /// <summary>
    /// 用来控制光标的 <see cref="DispatcherTimer"/> 类型
    /// </summary>
    private CaretBlinkDispatcherTimer? _caretBlinkTimer;

    /// <summary>
    /// 开始闪烁光标
    /// </summary>
    private void StartBlink()
    {
        _textEditor.Logger.LogDebug("StartBlink 开始闪烁光标");

        // 一旦调用 开始闪烁光标 就需要第一次显示光标
        _isBlinkShown = false;

        if (_caretBlinkTimer is null)
        {
            _caretBlinkTimer = new CaretBlinkDispatcherTimer(this)
            {
                Interval = _textEditor.CaretConfiguration.CaretBlinkTime
            };
        }
        else
        {
            _caretBlinkTimer.Stop();
        }

        _caretBlinkTimer.Start();

        if (_caretBlinkTimer.Interval > TimeSpan.FromMilliseconds(16))
        {
            // 如果超过 16 毫秒才开始闪烁，那先调用显示光标，否则可能需要等待一会
            ICaretManager caretManager = this;
            caretManager.OnTick();
        }
    }

    /// <summary>
    /// 停止闪烁光标
    /// </summary>
    private void StopBlink()
    {
        _textEditor.Logger.LogDebug("StopBlink 停止闪烁光标");
        Debug.Assert(_caretBlinkTimer != null);
        _caretBlinkTimer?.Stop();

        HideCaret();
    }

    void ICaretManager.OnTick()
    {
        if (!_textEditor.IsInEditingInputMode)
        {
            // 如果没有进入编辑模式，按照文本库的规定，那就是不需要显示光标
            StopBlink();
            return;
        }

        if (_textEditor.TextEditorCore.IsDirty)
        {
            // 如果布局还没完成，那就啥也不用干，直接隐藏光标即可
            if (_isBlinkShown)
            {
                HideBlink();
                _isBlinkShown = false;
            }

            return;
        }

        var currentSelection = _textEditor.TextEditorCore.CurrentSelection;
        if (currentSelection.IsEmpty)
        {
            Blinking(currentSelection);
        }
        else
        {
            ShowSelection(currentSelection);

            _caretBlinkTimer?.Stop();
        }
    }

    /// <summary>
    /// 闪烁光标
    /// </summary>
    /// <param name="currentSelection"></param>
    private void Blinking(Selection currentSelection)
    {
        // 没有选择的情况，绘制和闪烁光标
        if (_isBlinkShown)
        {
            HideBlink();
        }
        else
        {
            // 由于判断了 _textEditor.TextEditorCore.IsDirty 因此不需要再等待布局完成
            //await _textEditor.TextEditorCore.WaitLayoutCompletedAsync();

            if (_renderInfoProvider is null)
            {
                _renderInfoProvider = _textEditor.TextEditorCore.GetRenderInfo();
            }

            // 获取光标的坐标
            var caretRenderInfo = _renderInfoProvider.GetCaretRenderInfo(currentSelection.FrontOffset);

            var foreground = _textEditor.CaretConfiguration.CaretBrush ??
                             _textEditor.CurrentCaretRunProperty.Foreground.Value;

            var rectangle = caretRenderInfo.GetCaretBounds(_textEditor.CaretConfiguration.CaretWidth).ToWpfRect();
            var drawingContext = RenderOpen();
            using (drawingContext)
            {
                drawingContext.DrawRectangle(foreground, null, rectangle);
            }
        }

        _isBlinkShown = !_isBlinkShown;
        _isShowingSelection = false;
    }

    private void ShowSelection(in Selection currentSelection)
    {
        if (_renderInfoProvider is null)
        {
            _renderInfoProvider = _textEditor.TextEditorCore.GetRenderInfo();
        }

        using var drawingContext = RenderOpen();

        foreach (var rect in _renderInfoProvider.GetSelectionBoundsList(currentSelection))
        {
            drawingContext.DrawRectangle(_textEditor.CaretConfiguration.SelectionBrush, null, rect.ToWpfRect());
        }

        _isShowingSelection = true;
    }

    /// <summary>
    /// 闪烁光标-隐藏
    /// </summary>
    private void HideBlink()
    {
        var currentSelection = _textEditor.TextEditorCore.CurrentSelection;

        if (!currentSelection.IsEmpty && _textEditor.CaretConfiguration.ShowSelectionWhenNotInEditingInputMode)
        {
            // 选择内容就无需隐藏了
            return;
        }

        // 当前光标已经显示，那就是需要隐藏光标即可。啥都不显示
        var drawingContext = RenderOpen();
        using (drawingContext)
        {
            // 啥都不需要做，这就是清空
        }
    }

    /// <summary>
    /// 光标闪烁显示
    /// </summary>
    private bool _isBlinkShown;

    private bool _isShowingSelection;

    /// <summary>
    /// 隐藏光标
    /// </summary>
    private void HideCaret()
    {
        HideBlink();
    }

    #endregion
}