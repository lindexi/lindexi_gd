using LightTextEditorPlus.Core.Document.DocumentEventArgs;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 提供文档管理器的回调
/// </summary>
/// 核心作用只是减少 <see cref="DocumentManager"/> 需要外发事件进行通知。事件监听需要创建委托对象，导致文本框创建的时候需要带出去很多个对象，影响内存性能
internal interface IDocumentManagerCallback
{
    void OnDocumentChanging(object sender, DocumentChangeEventArgs args);

    void OnDocumentChanged(object sender, DocumentChangeEventArgs args);
}

internal sealed class EmptyDocumentManagerCallback : IDocumentManagerCallback
{
    public static EmptyDocumentManagerCallback Instance { get; }
    // 不用担心，正常情况下是不用导致初始化的
        = new EmptyDocumentManagerCallback();

    public void OnDocumentChanging(object sender, DocumentChangeEventArgs args)
    {
    }

    public void OnDocumentChanged(object sender, DocumentChangeEventArgs args)
    {
    }
}