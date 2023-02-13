using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
/// 支持光标系统，支持选择模块，支持注入光标渲染实现
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
/// todo 光标系统
/// todo 鼠标命中字符
/// todo 选择模块
/// todo 选择效果视觉反馈
/// todo 下划线模块
/// todo 项目符号
/// todo 上下标
/// todo 支持上加音标
/// todo 支持下加注释
/// todo 支持文档获取 SaveInfo 序列化存储
/// todo 支持设置当前的文本需要等待布局之后才能获取布局信息的属性
public partial class TextEditorCore
{
    public TextEditorCore(IPlatformProvider platformProvider)
    {
        PlatformProvider = platformProvider;

        UndoRedoProvider = platformProvider.BuildTextEditorUndoRedoProvider();

        DocumentManager = new DocumentManager(this);
        CaretManager = new CaretManager(this);
        DocumentManager.InternalDocumentChanging += DocumentManager_InternalDocumentChanging;
        DocumentManager.InternalDocumentChanged += DocumentManager_DocumentChanged;

        _layoutManager = new LayoutManager(this);
        _layoutManager.InternalLayoutCompleted += LayoutManager_InternalLayoutCompleted;

        Logger = platformProvider.BuildTextLogger() ?? new EmptyTextLogger();

#if DEBUG
        IsInDebugMode = true;
#endif
    }

    #region 框架逻辑

    private readonly LayoutManager _layoutManager;
    private RenderInfoProvider? _renderInfoProvider;
    internal CaretManager CaretManager { get; }
    public DocumentManager DocumentManager { get; }

    #region 平台相关

    public IPlatformProvider PlatformProvider { get; }

    internal IPlatformRunPropertyCreator PlatformRunPropertyCreator => PlatformProvider.GetPlatformRunPropertyCreator();

    #endregion

    private void DocumentManager_InternalDocumentChanging(object? sender, EventArgs e)
    {
        IsDirty = true;
        if (_renderInfoProvider != null)
        {
            _renderInfoProvider.IsDirty = true;
            _renderInfoProvider = null;
        }

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
        _layoutUpdateReasonManager?.AddLayoutReason(updateReason);
        PlatformProvider.RequireDispatchUpdateLayout(UpdateLayout);
    }

    private void UpdateLayout()
    {
        IsUpdatingLayout = true;

        // 更新布局完成之后，更新渲染信息
        Logger.LogDebug($"[TextEditorCore][UpdateLayout] 开始更新布局");

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

    // todo FontSize

    /// <summary>
    /// 获取或设置文本框的尺寸自适应模式
    /// </summary>
    /// todo 处理自适应变更的重新更新
    public SizeToContent SizeToContent { set; get; }

    /// <summary>
    /// 设置当前多倍行距呈现策略
    /// </summary>
    /// todo 实现当前多倍行距呈现策略
    public LineSpacingStrategy LineSpacingStrategy { set; get; }

    /// <summary>
    /// 布局方式
    /// </summary>
    /// todo 实现布局方式
    public ArrangingType ArrangingType { set; get; }

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
    public CaretOffset CurrentCaretOffset
    {
        set => CaretManager.CurrentCaretOffset = value;
        get => CaretManager.CurrentCaretOffset;
    }

    /// <summary>
    /// 获取或设置当前的选择范围
    /// </summary>
    public Selection CurrentSelection
    {
        set => CaretManager.SetSelection(value);
        get => CaretManager.CurrentSelection;
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
    public bool IsDirty { get; private set; } = true;

    #endregion

    #region 调试属性

    /// <summary>
    /// 是否正在调试模式
    /// </summary>
    /// 文本库将使用 Release 构建进行分发，但是依然提供调试方法，开启调试模式之后会有更多输出和判断逻辑，以及抛出调试异常。不应该在正式发布时，设置进入调试模式
    public bool IsInDebugMode
    {
        private set => _isInDebugMode = value;
        get => _isInDebugMode || IsAllInDebugMode;
    }
    private bool _isInDebugMode;

    /// <summary>
    /// 设置当前的文本进入调试模式
    /// </summary>
    public void SetInDebugMode()
    {
        IsInDebugMode = true;
        Logger.LogInfo($"文本进入调试模式");
    }

    /// <summary>
    /// 是否全部的文本都进入调试模式
    /// </summary>
    public static bool IsAllInDebugMode { private set; get; }

    /// <summary>
    /// 设置全部的文本都进入调试模式，理论上不能将此调用此方法的代码进行发布
    /// </summary>
    public static void SetAllInDebugMode()
    {
        IsAllInDebugMode = true;
    }

    #endregion

    #endregion

    #region 公开事件

    /// <summary>
    /// 文档开始变更事件
    /// </summary>
    public event EventHandler? DocumentChanging;

    /// <summary>
    /// 文档变更完成事件
    /// </summary>
    public event EventHandler? DocumentChanged;

    /// <summary>
    /// 文档布局完成事件
    /// </summary>
    public event EventHandler<LayoutCompletedEventArgs>? LayoutCompleted;

    // todo 考虑 DocumentLayoutBoundsChanged 事件

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
            Logger.LogDebug($"[TextEditorCore][AddLayoutReason] {reason}");
        }
    }

    private LayoutUpdateReasonManager? _layoutUpdateReasonManager;

    #endregion

    #endregion

    #region UndoRedo

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
    /// 判断当前是否进入撤销恢复模式，如果没有，抛出 <see cref="TextEditorNotInUndoRedoModeException"/> 异常
    /// </summary>
    /// <exception cref="TextEditorNotInUndoRedoModeException"></exception>
    public void VerifyInUndoRedoMode()
    {
        if (!IsUndoRedoMode)
        {
            throw new TextEditorNotInUndoRedoModeException();
        }
    }

    #endregion
}
