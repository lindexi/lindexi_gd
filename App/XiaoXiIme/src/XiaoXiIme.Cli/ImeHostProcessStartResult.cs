using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.Cli;

internal sealed record ImeHostProcessStartResult
(
    bool Succeeded,
    string Message,
    string ExecutablePath,
    int? ProcessId = null,
    ImeHostStatus? HostStatus = null
);