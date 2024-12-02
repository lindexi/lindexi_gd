using System;
using System.Diagnostics;

using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core;

/// <summary>
/// 文本编辑控件
/// <para></para>
/// 支持复杂文本编辑，支持添加扩展字符包括图片等到文本
/// <para></para>
/// 这是一个底层类型，提供很多定制逻辑，设计上属于功能多但是不可简单使用，上层应该对此进行再次封装
/// </summary>
/// <remarks> 这个项目的核心和入口就是这个类</remarks>
///
/// 定位： 这是一个比 Word 的富文本差得多的文本编辑工具
///
/// 设计：
/// 设计上这个程序集不依赖具体的平台框架，提供跨平台的文本布局和渲染驱动能力。要求具体平台创建 <see cref="IPlatformProvider"/> 平台的具体实现，注入到文本库里
/// 从而让文本库可以调用到具体的平台的文本布局辅助和文本渲染能力
///
/// 文本库支持实现简单轻量的富文本功能，支持设置文本的通用富文本属性，
/// 在属性系统上支持添加自己定制的额外的文本属性
/// 支持文本根据属性系统进行额外排版定义
/// 支持文本注入分段换行规则
/// 支持文本注入分词系统，同时内建默认简单分词系统
/// 支持混排，包括图文混排，公式混排，可交互元素混排
///
/// 支持段落项目符号，支持自定义扩展项目符号属性设置，通过文本属性系统给项目符号进行业务方定义
///
/// 支持光标系统，支持选择模块，支持业务端实现光标渲染
///
/// 支持事件系统，支持日志注入和埋点支持
///
/// 支持命中机制，支持超链接系统
///
/// 支持长文本排版，支持设置可见范围
///
///
/// 
/// 此文件放置框架内的逻辑
/// 其他 API 相关的，放入到 API 文件夹里
///
/// 事件触发顺序：
/// - DocumentChanging
/// - DocumentChanged
/// - 释放 <see cref="WaitLayoutCompletedAsync"/> 等待
///   - 可以获取到 <see cref="RenderInfoProvider"/> 内容
/// - <see cref="LayoutCompleted"/>
/// - 触发平台渲染
/// todo 决定哪些内容应该放在此文件里面，哪些应该放在 API 文件夹里面
public partial class TextEditorCore
{
    /// <summary>
    /// 创建文本编辑控件
    /// </summary>
    /// <param name="platformProvider"></param>
    public TextEditorCore(IPlatformProvider platformProvider)
    {
        PlatformProvider = platformProvider;

        UndoRedoProvider = platformProvider.BuildTextEditorUndoRedoProvider();

        DocumentManager = new DocumentManager(this);
        DocumentManager.InternalDocumentChanging += DocumentManager_InternalDocumentChanging;
        DocumentManager.InternalDocumentChanged += DocumentManager_DocumentChanged;

        CaretManager = new CaretManager(this);
        CaretManager.InternalCurrentCaretOffsetChanging +=
            CaretManager_InternalCurrentCaretOffsetChanging;
        CaretManager.InternalCurrentCaretOffsetChanged +=
            CaretManager_InternalCurrentCaretOffsetChanged;
        CaretManager.InternalCurrentSelectionChanging +=
            CaretManager_InternalCurrentSelectionChanging;
        CaretManager.InternalCurrentSelectionChanged += CaretManager_InternalCurrentSelectionChanged;

        _layoutManager = new LayoutManager(this);
        _layoutManager.InternalLayoutCompleted += LayoutManager_InternalLayoutCompleted;

        Logger = platformProvider.BuildTextLogger() ?? new EmptyTextLogger(this);

        DebugConfiguration = new TextEditorDebugConfiguration(Logger);

#if DEBUG
        DebugConfiguration.SetInDebugMode();
#endif
    }

    #region 框架逻辑

    private readonly LayoutManager _layoutManager;
    private RenderInfoProvider? _renderInfoProvider;
    internal CaretManager CaretManager { get; }

    /// <inheritdoc cref="T:LightTextEditorPlus.Core.Document.DocumentManager"/>
    public DocumentManager DocumentManager { get; }

    #region 平台相关

    /// <inheritdoc cref="T:LightTextEditorPlus.Core.Platform.IPlatformProvider"/>
    public IPlatformProvider PlatformProvider { get; }

    #endregion

    #region 光标

    private void CaretManager_InternalCurrentCaretOffsetChanging(object? sender,
        TextEditorValueChangeEventArgs<CaretOffset> args)
    {
        CurrentCaretOffsetChanging?.Invoke(sender, args);
    }

    private void CaretManager_InternalCurrentCaretOffsetChanged(object? sender,
        TextEditorValueChangeEventArgs<CaretOffset> args)
    {
        CurrentCaretOffsetChanged?.Invoke(sender, args);
    }

    private void CaretManager_InternalCurrentSelectionChanging(object? sender,
        TextEditorValueChangeEventArgs<Selection> args)
    {
        CurrentSelectionChanging?.Invoke(sender, args);
    }

    private void CaretManager_InternalCurrentSelectionChanged(object? sender,
        TextEditorValueChangeEventArgs<Selection> args)
    {
        CurrentSelectionChanged?.Invoke(sender, args);
    }

    #endregion

    /// <summary>
    /// 准备重新布局整个文档
    /// </summary>
    /// <param name="reason"></param>
    internal void RequireDispatchReLayoutAllDocument(string reason)
    {
        // 是否在文本初始化时进入，如果是文本初始化进入，那就不需要重新布局整个文档，可以等待有具体数据传入再执行，提升性能
        // 如果立刻获取光标信息，此时是可以通过调用布局获取到的
        var isInit = !_isAnyLayoutUpdate;

        if (isInit)
        {
            return;
        }

        IsDirty = true;

        if (DocumentManager.CharCount != 0)
        {
            // 整个文档设置都是脏的
            foreach (var paragraphData in DocumentManager.ParagraphManager.GetParagraphList())
            {
                paragraphData.SetDirty();
                //foreach (var lineLayoutData in paragraphData.LineLayoutDataList)
                //{
                //}
            }
        }

        RequireDispatchUpdateLayoutInner(reason);
    }

    /// <summary>
    /// 是否发生过一次布局更新
    /// </summary>
    // ReSharper disable once RedundantDefaultMemberInitializer
    private bool _isAnyLayoutUpdate = false;

    private void DocumentManager_InternalDocumentChanging(object? sender, EventArgs e)
    {
        if (IsUpdatingLayout)
        {
            throw new ChangeDocumentOnUpdatingLayoutException(this);
        }

        IsDirty = true;

        // 文档开始变更
        DocumentChanging?.Invoke(this, e);
    }

    private void DocumentManager_DocumentChanged(object? sender, EventArgs e)
    {
        // 按照 事件触发顺序 需要先触发 DocumentChanged 事件，再触发 LayoutCompleted 事件
        DocumentChanged?.Invoke(this, e);

        // 文档变更，更新布局
        Logger.LogDebug($"[TextEditorCore] 文档变更，更新布局");

        // todo 考虑在需要立刻获取布局信息时，直接更新布局
        RequireDispatchUpdateLayout("DocumentChanged");
    }

    internal void RequireDispatchUpdateLayout(string updateReason)
    {
        RequireDispatchUpdateLayoutInner(updateReason);
    }

    private void RequireDispatchUpdateLayoutInner(string updateReason)
    {
        AddLayoutReason(updateReason);
        PlatformProvider.RequireDispatchUpdateLayout(UpdateLayout);
    }

    /// <summary>
    /// 空文本布局。在文本没有更改的时候，有逻辑需要获取渲染信息
    /// </summary>
    /// 特别加一个方法，用来方便调试是哪个业务调用
    private void LayoutEmptyTextEditor()
    {
        AddLayoutReason(nameof(LayoutEmptyTextEditor) + "空文本布局");
        PlatformProvider.InvokeDispatchUpdateLayout(UpdateLayout);
    }

    private void UpdateLayout()
    {
        // 设置更新过布局
        _isAnyLayoutUpdate = true;

        IsUpdatingLayout = true;

        // 更新布局完成之后，更新渲染信息
        Logger.LogDebug($"[TextEditorCore][UpdateLayout] 开始更新布局");
        if (_layoutUpdateReasonManager is { } layoutUpdateReasonManager)
        {
            Logger.LogDebug(
                $"[TextEditorCore][UpdateLayout][UpdateReason] 布局原因：{layoutUpdateReasonManager.ReasonText}");
        }

        try
        {
            _layoutManager.UpdateLayout();
        }
        finally
        {
            IsUpdatingLayout = false;
        }

        Logger.LogDebug($"[TextEditorCore][UpdateLayout] 完成更新布局");
    }

    private void LayoutManager_InternalLayoutCompleted(object? sender, EventArgs e)
    {
        // 布局完成了，文本不是脏的，可以获取布局内容
        IsDirty = false;

        Debug.Assert(_renderInfoProvider is null);
        _renderInfoProvider = new RenderInfoProvider(this);

        SetLayoutCompleted();

        LayoutCompleted?.Invoke(this, new LayoutCompletedEventArgs());

        Logger.LogDebug($"[TextEditorCore][Render] 开始调用平台渲染");

        // 布局完成，触发渲染
        var renderManager = PlatformProvider.GetRenderManager();
        renderManager?.Render(_renderInfoProvider);
        Logger.LogDebug($"[TextEditorCore][Render] 完成调用平台渲染");
    }

    #endregion

    #region 公开属性

    #region 文本属性

    /// <summary>
    /// 获取或设置文本框的尺寸自适应模式
    /// </summary>
    public SizeToContent SizeToContent
    {
        set
        {
            if (_sizeToContent == value) return;
            _sizeToContent = value;
            RequireDispatchReLayoutAllDocument("SizeToContent Changed");
        }
        get => _sizeToContent;
    }

    private SizeToContent _sizeToContent = SizeToContent.Manual;

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
    /// 日志
    /// </summary>
    public ITextLogger Logger { get; }

    // todo 考虑设置可见范围，用来支持长文本

    #endregion

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
    [Obsolete("如已知 CaretOffset 的值，则可直接给 CurrentCaretOffset 属性赋值。此方法仅仅只是用来告诉你正确的方法应该是给 CurrentCaretOffset 属性赋值，无需再调用任何方法")]
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
    /// 默认情况下是非脏的，使用预设的值
    private bool _isDirty = false;
    #endregion

    #region 调试属性

    /// <summary>
    /// 这个文本的调试名，用于在各个抛出的异常等，方便记录调试日志或埋点上报了解是哪个文本框抛出的。默认是空将取文本的前15个字符
    /// </summary>
    public string? DebugName
    {
        set => _debugName = value;
        get
        {
            if (_debugName is null)
            {
                return "\"" + DocumentManager.ParagraphManager.GetText().LimitTrim(15) + "\"";
            }

            return _debugName;
        }
    }

    private string? _debugName;

    /// <inheritdoc />
    public override string ToString() => $"[{nameof(TextEditorCore)}] {DebugName}";

    /// <summary>
    /// 文本调试的配置
    /// </summary>
    public TextEditorDebugConfiguration DebugConfiguration { get; }

    /// <inheritdoc cref="TextEditorDebugConfiguration.IsInDebugMode"/>
    public bool IsInDebugMode => DebugConfiguration.IsInDebugMode;

    /// <inheritdoc cref="TextEditorDebugConfiguration.SetInDebugMode"/>
    public void SetInDebugMode() => DebugConfiguration.SetInDebugMode();

    /// <inheritdoc cref="TextEditorDebugConfiguration.SetAllInDebugMode"/>
    public static void SetAllInDebugMode() => TextEditorDebugConfiguration.SetAllInDebugMode();

    #endregion

    #endregion

    #region 公开事件

    /// <summary>
    /// 文档开始变更事件
    /// </summary>
    /// 内部使用 <see cref="LightTextEditorPlus.Core.Document.DocumentManager.InternalDocumentChanging"/> 事件
    [TextEditorPublicAPI]
    public event EventHandler? DocumentChanging;

    /// <summary>
    /// 文档变更完成事件
    /// </summary>
    /// 内部使用 <see cref="LightTextEditorPlus.Core.Document.DocumentManager.InternalDocumentChanged"/> 事件
    [TextEditorPublicAPI]
    public event EventHandler? DocumentChanged;

    /// <summary>
    /// 文档布局完成事件
    /// </summary>
    public event EventHandler<LayoutCompletedEventArgs>? LayoutCompleted;

    // todo 考虑 DocumentLayoutBoundsChanged 事件

    #region 光标

    /// <summary>
    /// 当前光标开始变更事件
    /// </summary>
    public event EventHandler<TextEditorValueChangeEventArgs<CaretOffset>>?
        CurrentCaretOffsetChanging;

    /// <summary>
    /// 当前光标已变更事件
    /// </summary>
    public event EventHandler<TextEditorValueChangeEventArgs<CaretOffset>>?
        CurrentCaretOffsetChanged;

    /// <summary>
    /// 当前选择范围开始变更事件
    /// </summary>
    public event EventHandler<TextEditorValueChangeEventArgs<Selection>>? CurrentSelectionChanging;

    /// <summary>
    /// 当前选择范围已变更事件。当光标变更或选择范围变更时，会触发此事件。即 <see cref="CurrentCaretOffsetChanged"/> 触发时，一定会随后触发此事件
    /// </summary>
    public event EventHandler<TextEditorValueChangeEventArgs<Selection>>? CurrentSelectionChanged;

    #endregion

    /// <summary>
    /// 布局变更后触发的事件
    /// </summary>
    public event EventHandler<TextEditorValueChangeEventArgs<ArrangingType>>? ArrangingTypeChanged;

    #endregion

    #region 公开方法

    #endregion

    #region 框架内使用的属性

    #region 更新布局原因

    /// <summary>
    /// 添加触发布局的原因，仅仅设置 <see cref="IsInDebugMode"/> 有效
    /// </summary>
    /// <param name="reason"></param>
    public void AddLayoutReason(string reason)
    {
        if (IsInDebugMode)
        {
            _layoutUpdateReasonManager ??= new LayoutUpdateReasonManager(this);
            _layoutUpdateReasonManager.AddLayoutReason(reason);
            Logger.LogDebug($"[TextEditorCore][AddLayoutReason] 添加布局理由 {reason}");
        }
    }

    private LayoutUpdateReasonManager? _layoutUpdateReasonManager;

    #endregion

    #endregion

    #region UndoRedo

    /// <inheritdoc cref="T:LightTextEditorPlus.Core.Document.UndoRedo.ITextEditorUndoRedoProvider"/>
    public ITextEditorUndoRedoProvider UndoRedoProvider { get; }

    /// <summary>
    /// 进入撤销恢复模式
    /// </summary>
    /// 由于撤销恢复需要一些绕过安全的步骤，因此需要先设置开关才能调用。同时撤销重做需要处理在撤销或恢复过程产生动作
    public void EnterUndoRedoMode()
    {
        IsUndoRedoMode = true;
    }

    /// <summary>
    /// 退出撤销恢复模式
    /// </summary>
    public void QuitUndoRedoMode()
    {
        IsUndoRedoMode = false;
    }

    /// <summary>
    /// 获取文本是否进入撤销恢复模式
    /// </summary>
    public bool IsUndoRedoMode { private set; get; }

    /// <summary>
    /// 撤销恢复是否可用
    /// </summary>
    public bool EnableUndoRedo { private set; get; } = true;

    /// <summary>
    /// 设置撤销恢复是否可用
    /// </summary>
    /// <param name="isEnable"></param>
    /// <param name="debugReason">调试使用的设置撤销恢复的理由</param>
    /// <returns></returns>
    public void SetUndoRedoEnable(bool isEnable, string debugReason)
    {
        Logger.LogDebug($"[SetUndoRedoEnable] Enable={isEnable};Reason={debugReason}");
        EnableUndoRedo = isEnable;
    }

    /// <summary>
    /// 是否应该插入撤销恢复。等同于不在撤销恢复模式且撤销恢复可用
    /// </summary>
    internal bool ShouldInsertUndoRedo => !IsUndoRedoMode && EnableUndoRedo;

    /// <summary>
    /// 判断当前是否进入撤销恢复模式，如果没有，抛出 <see cref="TextEditorNotInUndoRedoModeException"/> 异常
    /// </summary>
    /// <exception cref="TextEditorNotInUndoRedoModeException"></exception>
    public void VerifyInUndoRedoMode()
    {
        if (!IsUndoRedoMode)
        {
            throw new TextEditorNotInUndoRedoModeException(this);
        }
    }

    #endregion
}