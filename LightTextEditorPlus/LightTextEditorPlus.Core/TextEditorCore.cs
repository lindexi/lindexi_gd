using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core.Document.DocumentManagers;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

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
/// - LayoutCompleted
/// todo 光标系统
/// todo 鼠标命中字符
/// todo 选择模块
/// todo 选择效果视觉反馈
/// todo 下划线模块
/// todo 项目符号
/// todo 上下标
/// todo 支持上加音标
/// todo 支持下加注释
/// todo 撤销重做
/// todo 支持文档获取 SaveInfo 序列化存储
public partial class TextEditorCore
{
    public TextEditorCore(IPlatformProvider platformProvider)
    {
        PlatformProvider = platformProvider;

        DocumentManager = new DocumentManager(this);
        DocumentManager.InternalDocumentChanging += DocumentManager_InternalDocumentChanging;
        DocumentManager.InternalDocumentChanged += DocumentManager_DocumentChanged;

        _layoutManager = new LayoutManager(this);
        _layoutManager.InternalLayoutCompleted += LayoutManager_InternalLayoutCompleted;

        Logger = platformProvider.BuildTextLogger() ?? new EmptyTextLogger();
    }

    #region 框架逻辑

    private readonly LayoutManager _layoutManager;

    public DocumentManager DocumentManager { get; }
    public IPlatformProvider PlatformProvider { get; }

    private void DocumentManager_InternalDocumentChanging(object? sender, EventArgs e)
    {
        // 文档开始变更
        DocumentChanging?.Invoke(this, e);
    }

    private void DocumentManager_DocumentChanged(object? sender, EventArgs e)
    {
        // 按照 事件触发顺序 需要先触发 DocumentChanged 事件，再触发 LayoutCompleted 事件
        DocumentChanged?.Invoke(this, e);

        // 文档变更，更新布局
        Logger.LogDebug($"[TextEditorCore] 文档变更，更新布局");
        PlatformProvider.RequireDispatchUpdateLayout(UpdateLayout);
    }

    private void UpdateLayout()
    {
        // 更新布局完成之后，更新渲染信息
        Logger.LogDebug($"[TextEditorCore][UpdateLayout] 开始更新布局");
        _layoutManager.UpdateLayout();
        Logger.LogDebug($"[TextEditorCore][UpdateLayout] 完成更新布局");
    }

    private void LayoutManager_InternalLayoutCompleted(object? sender, EventArgs e)
    {
        LayoutCompleted?.Invoke(this,e);
    }

    #endregion

    #region 公开属性

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
    public event EventHandler? LayoutCompleted;

    #endregion

    #region 公开方法

    /// <summary>
    /// 追加一段文本，追加的文本按照段末的样式
    /// </summary>
    /// 其实这个方法不应该放在这里
    public void AppendText(string text)
    {
        DocumentManager.AppendText(text);
    }

    #endregion
}
