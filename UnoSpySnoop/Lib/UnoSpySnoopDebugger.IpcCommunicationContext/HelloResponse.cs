namespace UnoSpySnoopDebugger.IpcCommunicationContext;

public record HelloResponse(string VersionText, string ProcessName, long ProcessId, string CommandLine);