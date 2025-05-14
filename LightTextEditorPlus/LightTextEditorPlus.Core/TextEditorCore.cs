using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Diagnostics.LogInfos;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.DocumentEventArgs;
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
/// 这是一个底层类型，提供很多定制逻辑，设计上属于功能多但是不可简单使用，上层应该对此进行再次封装。此类型将被业务方作为高级 API 使用。正常情况下，业务方不应该直接使用此类型、不应该直接创建此类型的实例
/// </summary>
/// <remarks> 这个项目的核心和入口就是这个类</remarks>
///
/// 定位： 这是一个比 Word 的富文本差得多的文本编辑工具
///
/// 设计：
/// 设计上这个程序集不依赖具体的平台框架，提供跨平台的文本布局和渲染驱动能力。要求具体平台创建 <see cref="ITextEditorPlatformProvider"/> 平台的具体实现，注入到文本库里
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
public partial class TextEditorCore
{
    /// <summary>
    /// 创建文本编辑控件
    /// </summary>
    /// <param name="platformProvider"></param>
    public TextEditorCore(ITextEditorPlatformProvider platformProvider)
    {
        PlatformProvider = platformProvider;

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

        Logger = platformProvider.BuildTextLogger() ?? new EmptyTextLogger(this);

        DebugConfiguration = new TextEditorDebugConfiguration(Logger);

#if DEBUG
        DebugConfiguration.SetInDebugMode(withLog: false);
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
    public ITextEditorPlatformProvider PlatformProvider { get; }

    #region 字体回滚

    /// <summary>
    /// 提供字体回滚和字体名称管理
    /// </summary>
    public IFontNameManager FontNameManager
    {
        get => _fontNameManager ?? TextContext.FontNameManager;
        set => _fontNameManager = value;
    }

    private IFontNameManager? _fontNameManager;

    #endregion

    #endregion

    #region 光标

    private void CaretManager_InternalCurrentCaretOffsetChanging(object? sender,
        TextEditorValueChangeEventArgs<CaretOffset> args)
    {
        // todo 后续优化事件触发顺序，确保内部监听优先触发
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
    /// 准备重新布局整个文档。如果是空文本、文本初始化时，不会重新布局整个文档
    /// </summary>
    /// <param name="reason"></param>
    internal void RequireDispatchReUpdateAllDocumentLayout(string reason)
    {
        // 是否在文本初始化时进入，如果是文本初始化进入，那就不需要重新布局整个文档，可以等待有具体数据传入再执行，提升性能
        // 如果立刻获取光标信息，此时是可以通过调用布局获取到的
        var isInit = !_isAnyLayoutUpdate;

        if (isInit)
        {
            return;
        }

        IsDirty = true;

        // 整个文档设置都是脏的
        // 这里要用 GetRawParagraphList 方法，防止空文本获取时，创建除了一个空段落，导致不必要的损耗。虽然后续也会创建一个空段落，但是这里不需要
        foreach (var paragraphData in DocumentManager.ParagraphManager.GetRawParagraphList())
        {
            paragraphData.SetDirty();
            //foreach (var lineLayoutData in paragraphData.LineLayoutDataList)
            //{
            //}
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

    private void DocumentManager_DocumentChanged(object? sender, DocumentChangeEventArgs e)
    {
        if (IsUpdatingLayout)
        {
            // 更新布局的过程中，变更了文档。这是允许的，但会导致重新布局，且本次布局结果不可用
            // 且业务方必须确保次数，防止出现无限循环
            Logger.Log(new DocumentChangedWhenUpdatingLayoutLogInfo());
        }

        // 按照 事件触发顺序 需要先触发 DocumentChanged 事件，再触发 LayoutCompleted 事件
        DocumentChanged?.Invoke(this, e);
        if (e.DocumentChangeKind == DocumentChangeKind.Text)
        {
            TextChanged?.Invoke(this, e);
        }

        // 文档变更，更新布局
        Logger.LogDebug($"[TextEditorCore] DocumentChanged 文档变更，更新布局");

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
        if (IsInDebugMode && !IsEmptyInitializingTextEditor())
        {
            throw new TextEditorInnerDebugException($"只有空文本才能调用空文本布局");
        }

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
            // 只有调试模式才能进入此分支
            //Debug.Assert(IsInDebugMode);
            Logger.LogDebug($"[TextEditorCore][UpdateLayout][UpdateReason] 布局原因：{layoutUpdateReasonManager.ReasonText}");
        }

        try
        {
            var result = _layoutManager.UpdateLayout();

            // 布局完成了，文本不是脏的，可以获取布局内容
            IsDirty = false;

            // 特意在布局完成的 IsDirty 设置为 false 之后，再触发日志。防止业务方监听日志从而获取属性的时候，抛出文本还是脏的异常
            Logger.Log(new LayoutCompletedLogInfo(result));
        }
        finally
        {
            IsUpdatingLayout = false;
        }

        Logger.LogDebug($"[TextEditorCore][UpdateLayout] 完成更新布局");

        OnLayoutCompleted();
    }

    private void OnLayoutCompleted()
    {


        Debug.Assert(_renderInfoProvider is null);
        _renderInfoProvider = new RenderInfoProvider(this);

        SetLayoutCompleted();
        LayoutCompleted?.Invoke(this, new LayoutCompletedEventArgs());

        if (IsDirty)
        {
            Debug.Assert(_renderInfoProvider is null, "在 LayoutCompleted 事件里面，再次变更了文本，此时文本是脏的，同时渲染提供应该是空");
            // 在 LayoutCompleted 事件里面重新变更了文本
            Logger.LogDebug($"[TextEditorCore][Layout] 在 LayoutCompleted 事件里面，再次变更了文本，此时不再调用平台渲染，等待下次布局之后再进入");
            Logger.Log(new TextEditorBeDirtyAfterLayoutCompletedLogInfo(this));
            return;
        }

        Debug.Assert(_renderInfoProvider is not null);

        Logger.LogDebug($"[TextEditorCore][Render] 开始调用平台渲染");
        // 布局完成，触发渲染
        var renderManager = PlatformProvider.GetRenderManager();
        renderManager?.Render(_renderInfoProvider);
        Logger.LogDebug($"[TextEditorCore][Render] 完成调用平台渲染");
    }

    #endregion
}
