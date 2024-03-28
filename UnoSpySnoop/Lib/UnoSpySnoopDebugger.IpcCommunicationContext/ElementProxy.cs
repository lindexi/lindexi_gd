namespace UnoSpySnoopDebugger.IpcCommunicationContext;

public class ElementProxy(ElementBaseInfo elementInfo, List<ElementProxy>? children)
{
    public ElementBaseInfo ElementInfo { get; set; } = elementInfo;
    public List<ElementProxy>? Children { get; set; } = children;
}

