using System.Text.Json.Serialization;

namespace WinRemoteShell.Shared;

public sealed record ExecRequest(IReadOnlyList<string> Arguments, int? TimeoutSeconds);

[JsonSerializable(typeof(ExecRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
