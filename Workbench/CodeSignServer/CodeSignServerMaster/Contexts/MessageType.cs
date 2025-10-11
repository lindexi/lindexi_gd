using System.Runtime.InteropServices;

namespace CodeSignServerMaster.Contexts;

struct MessageType()
{
    public int HeadLength { get; init; } = 100;
    public long Header { get; init; }
    public int Type { get; init; }

    public const long DefaultHeader = 0x6e67695365646f43;
    // 0x6e67695365646f43
    //var header = MemoryMarshal.Read<long>("CodeSignServer"u8);
}

record SignTaskRequest(string TraceId, string FileUrl, string SignName);
record SignTaskResponse(string TraceId, string FileUrl, string Message);

record FetchSignTaskRequest(SignTaskRequest? SignTaskRequest, string Message);
record FetchSignTaskResponse(SignTaskResponse? SignTaskResponse);

record SignSlaveInfo(string Name);