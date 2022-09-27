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
/// 此文件放置框架内的逻辑
/// 其他 API 相关的，放入到 API 文件夹里
public partial class TextEditorCore
{
    public TextEditorCore(IPlatformProvider platformProvider)
    {
        PlatformProvider = platformProvider;

        DocumentManager = new DocumentManager(this);
        DocumentManager.InternalDocumentChanging += DocumentManager_InternalDocumentChanging;
        DocumentManager.InternalDocumentChanged += DocumentManager_DocumentChanged;

        _layoutManager = new LayoutManager(this);

        Logger = platformProvider.BuildTextLogger() ?? new EmptyTextLogger();
    }

    private void DocumentManager_InternalDocumentChanging(object? sender, EventArgs e)
    {
        // 文档开始变更
        DocumentChanging?.Invoke(this, e);
    }

    #region 框架逻辑

    private readonly LayoutManager _layoutManager;

    public DocumentManager DocumentManager { get; }
    public IPlatformProvider PlatformProvider { get; }

    private void DocumentManager_DocumentChanged(object? sender, EventArgs e)
    {
        // 文档变更，更新布局
        Logger.LogDebug($"[TextEditorCore] 文档变更，更新布局");
        PlatformProvider.RequireDispatchUpdateLayout(UpdateLayout);

        DocumentChanged?.Invoke(this, e);
    }

    private void UpdateLayout()
    {
        // 更新布局完成之后，更新渲染信息
        Logger.LogDebug($"[TextEditorCore][UpdateLayout] 开始更新布局");
        _layoutManager.UpdateLayout();
        Logger.LogDebug($"[TextEditorCore][UpdateLayout] 完成更新布局");
    }

    #endregion

    #region 公开属性

    /// <summary>
    /// 日志
    /// </summary>
    public ITextLogger Logger { get; }

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
