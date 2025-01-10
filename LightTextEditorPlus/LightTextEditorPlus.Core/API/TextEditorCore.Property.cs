using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core;

// 此文件存放文本属性
partial class TextEditorCore
{
    #region 文本属性

    /// <summary>
    /// 获取或设置文本的垂直对齐方式
    /// </summary>
    public VerticalTextAlignment VerticalTextAlignment
    {
        get => VerticalTextAlignment.Top;
        [Obsolete("当前还没实现，请不要调用")] set => throw new NotSupportedException($"当前还没实现文本的垂直对齐方式");
    }

    /// <summary>
    /// 这个属性在这里只是为了告诉你，文本的水平布局是由段落控制的，不是由整个文本控制的
    /// </summary>
    [Obsolete("这个属性在这里只是为了告诉你，文本的水平布局是由段落控制的，不是由整个文本控制的", error: true)]
    public object TextAlignment => throw new NotSupportedException();

    /// <summary>
    /// 这个属性在这里只是为了告诉你，文本的水平布局是由段落控制的，不是由整个文本控制的
    /// </summary>
    [Obsolete("这个属性在这里只是为了告诉你，文本的水平布局是由段落控制的，不是由整个文本控制的", error: true)]
    public object HorizontalTextAlignment => throw new NotSupportedException();

    /// <summary>
    /// 获取或设置文本框的尺寸自适应模式
    /// </summary>
    public TextSizeToContent SizeToContent
    {
        set
        {
            if (_sizeToContent == value) return;
            _sizeToContent = value;
            RequireDispatchReLayoutAllDocument("SizeToContent Changed");
        }
        get => _sizeToContent;
    }

    private TextSizeToContent _sizeToContent = TextSizeToContent.Manual;

    /// <summary>
    /// 设置当前多倍行距呈现策略
    /// </summary>
    public LineSpacingStrategy LineSpacingStrategy
    {
        set
        {
            if (_lineSpacingStrategy == value) return;

            _lineSpacingStrategy = value;
            RequireDispatchReLayoutAllDocument("LineSpacingStrategy Changed");
        }
        get => _lineSpacingStrategy;
    }

    private LineSpacingStrategy _lineSpacingStrategy = LineSpacingStrategy.FullExpand;

    /// <summary>
    /// 行距算法
    /// </summary>
    public LineSpacingAlgorithm LineSpacingAlgorithm
    {
        set
        {
            if (_lineSpacingAlgorithm == value) return;

            _lineSpacingAlgorithm = value;
            RequireDispatchReLayoutAllDocument("LineSpacingAlgorithm Changed");
        }
        get => _lineSpacingAlgorithm;
    }

    private LineSpacingAlgorithm _lineSpacingAlgorithm = LineSpacingAlgorithm.PPT;

    /// <summary>
    /// 布局方式
    /// </summary>
    public ArrangingType ArrangingType
    {
        set
        {
            if (_arrangingType == value) return;
            var oldArrangingType = _arrangingType;
            _arrangingType = value;

            ArrangingTypeChanged?.Invoke(this,
                new TextEditorValueChangeEventArgs<ArrangingType>(oldArrangingType, value));

            RequireDispatchReLayoutAllDocument("ArrangingType Changed");
        }
        get => _arrangingType;
    }

    private ArrangingType _arrangingType;

    /// <summary>
    /// 文本的当前语言文化，此属性会影响文本的排版或渲染
    /// </summary>
    public CultureInfo CurrentCulture
    {
        get => _cultureInfo ?? CultureInfo.CurrentCulture;
        set
        {
            if (value.Equals(CurrentCulture))
            {
                return;
            }

            _cultureInfo = value;
            // 变更语言文化，需要重新布局
            RequireDispatchReLayoutAllDocument("CurrentCultureChanged");
        }
    }

    private CultureInfo? _cultureInfo;

    #endregion

    #region 状态属性

    // 存放文本当前的状态

    /// <summary>
    /// 是否正在更新布局。更新布局的过程中，不允许修改文档
    /// </summary>
    public bool IsUpdatingLayout { get; private set; }

    /// <summary>
    /// 文本是不是脏的，需要等待布局完成。可选使用 <see cref="WaitLayoutCompletedAsync"/> 等待布局完成
    /// </summary>
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            _isDirty = value;

            if (_renderInfoProvider != null)
            {
                _renderInfoProvider.IsDirty = true;
                _renderInfoProvider = null;
            }
        }
    }

    /// <summary>
    /// 文本是不是脏的
    /// </summary>
    /// 创建出来的文本就是脏的，需要等待布局完成才能获取到布局信息
    /// 根据 README.md 文档约定： “默认创建出来的文本是脏的，需要布局完成之后，才不是脏的”
    private bool _isDirty = true;

    #endregion

    // todo 考虑设置可见范围，用来支持长文本

    #region 调试属性
    // 调试属性放在 TextEditorCore.Diagnostics.cs 文件中
    #endregion

    #region 文本对外属性

    #region 光标和选择

    /// <summary>
    /// 获取或设置当前光标位置
    /// </summary>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public CaretOffset CurrentCaretOffset
    {
        set => CaretManager.CurrentCaretOffset = value;
        get => CaretManager.CurrentCaretOffset;
    }

    /// <summary>
    /// 获取或设置当前的选择范围
    /// </summary>
    /// 这是对外调用的，非框架内使用
    [TextEditorPublicAPI]
    public Selection CurrentSelection
    {
        set => CaretManager.SetSelection(value);
        get => CaretManager.CurrentSelection;
    }

    /// <summary>
    /// 移动光标。如已知 <see cref="CaretOffset"/> 可直接给 <see cref="CurrentCaretOffset"/> 属性赋值
    /// </summary>
    /// <param name="type"></param>
    [TextEditorPublicAPI]
    public void MoveCaret(CaretMoveType type)
    {
        var caretOffset = GetNewCaretOffset(type);
        CaretManager.CurrentCaretOffset = caretOffset;
    }

    /// <summary>
    /// 移动光标
    /// </summary>
    /// <param name="caretOffset"></param>
    [Obsolete(
        "如已知 CaretOffset 的值，则可直接给 CurrentCaretOffset 属性赋值。此方法仅仅只是用来告诉你正确的方法应该是给 CurrentCaretOffset 属性赋值，无需再调用任何方法")]
    public void MoveCaret(CaretOffset caretOffset) => CaretManager.CurrentCaretOffset = caretOffset;

    /// <summary>
    /// 根据键盘操作获取光标导航
    /// </summary>
    /// <param name="caretMoveType"></param>
    /// <returns></returns>
    public CaretOffset GetNewCaretOffset(CaretMoveType caretMoveType)
    {
        return KeyboardCaretNavigationHelper.GetNewCaretOffset(this, caretMoveType);
    }

    #endregion

    #endregion
}
