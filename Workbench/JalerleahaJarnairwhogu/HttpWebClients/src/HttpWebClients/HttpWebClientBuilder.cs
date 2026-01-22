using HttpWebClients.HostBackup;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HttpWebClients;

public class HttpWebClientBuilder
{
    public HttpWebClientBuilder UseHost(string host)
    {
        _host = host;
        return this;
    }

    private string? _host;

    private HostBackupManager? _hostBackupManager;

    private List<JsonSerializerContext>? _jsonSerializerContextList;
}