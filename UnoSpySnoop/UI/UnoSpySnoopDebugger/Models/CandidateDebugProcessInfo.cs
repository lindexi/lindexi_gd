using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;

namespace UnoSpySnoopDebugger.Models;

public class CandidateDebugProcessInfo
{
    public string? ProcessName { get; set; }
    public string? ProcessId { get; set; }
    public string? CommandLine { get; set; }
    public JsonIpcDirectRoutedClientProxy? Client { get; set; }
}
