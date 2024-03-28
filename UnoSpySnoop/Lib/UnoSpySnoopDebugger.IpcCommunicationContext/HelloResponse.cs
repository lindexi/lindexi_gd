namespace UnoSpySnoopDebugger.IpcCommunicationContext;

public record HelloResponse(string SnoopVersionText, string ProcessName, long ProcessId, string CommandLine);