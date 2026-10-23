using System.Net;

namespace AgentLib.Core;

internal sealed class DynamicWebProxy : IWebProxy
{
    public IWebProxy? Proxy { get; set; }

    public ICredentials? Credentials
    {
        get => Proxy?.Credentials;
        set
        {
            if (Proxy is not null)
            {
                Proxy.Credentials = value;
            }
        }
    }

    public Uri GetProxy(Uri destination)
    {
        return Proxy?.GetProxy(destination) ?? destination;
    }

    public bool IsBypassed(Uri host)
    {
        return Proxy?.IsBypassed(host) ?? true;
    }
}
