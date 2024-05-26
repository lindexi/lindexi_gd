namespace UnoSpySnoopDebugger.IpcCommunicationContext;

public class DependencyPropertyInfo(string name, string? value, string declaringTypeFullName)
{
    public string Name { get; set; } = name;
    public string? Value { get; set; } = value;
    public string DeclaringTypeFullName { get; set; } = declaringTypeFullName;
}
