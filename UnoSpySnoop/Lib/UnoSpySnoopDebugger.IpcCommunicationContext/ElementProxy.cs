namespace UnoSpySnoopDebugger.IpcCommunicationContext;

public record ElementProxy(ElementBaseInfo ElementInfo, List<ElementProxy>? Children)
{
}