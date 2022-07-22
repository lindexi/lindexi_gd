using System.Net;
using System.Runtime.Versioning;
using RegistryUtils;

namespace NilerlanaihikaWhurreeberhalur.Proxy;

/// <summary>
/// 自动跟随系统代理的代理
/// </summary>
[SupportedOSPlatform("windows")]
public class DynamicHttpWindowsProxy : IWebProxy,IDisposable
{
    public DynamicHttpWindowsProxy()
    {
        if (HttpWindowsProxy.TryCreate(out var proxy))
        {
            _innerProxy = proxy;
        }
        else
        {
            _innerProxy = new HttpNoProxy();
        }
    }

    /// <summary>
    /// 开始根据注册表变更动态修改代理，需要开启一个线程监听注册表
    /// </summary>
    public void Start()
    {
        RegistryMonitor = new RegistryMonitor(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections");
        RegistryMonitor.RegChanged += RegistryMonitor_RegChanged;
        RegistryMonitor.Start();

        // 启动完成之后，更新一次吧
        UpdateProxy();
    }

    private void RegistryMonitor_RegChanged(object? sender, EventArgs e)
    {
        UpdateProxy();
    }

    private void UpdateProxy()
    {
        if (HttpWindowsProxy.TryCreate(out var proxy))
        {
            InnerProxy = proxy;
        }
        else
        {
            InnerProxy = new HttpNoProxy();
        }
    }

    private RegistryMonitor? RegistryMonitor { set; get; }

    private IWebProxy InnerProxy
    {
        set
        {
            if (ReferenceEquals(_innerProxy, value))
            {
                return;
            }

            if (_innerProxy is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _innerProxy = value;
        }
        get => _innerProxy;
    }

    public ICredentials? Credentials
    {
        get => InnerProxy.Credentials;
        set => InnerProxy.Credentials = value;
    }

    public Uri? GetProxy(Uri destination)
    {
        return InnerProxy.GetProxy(destination);
    }

    public bool IsBypassed(Uri host)
    {
        return InnerProxy.IsBypassed(host);
    }

    public void Dispose()
    {
        if (_innerProxy is IDisposable disposable)
        {
            disposable.Dispose();
        }

        RegistryMonitor?.Dispose();
    }

    private IWebProxy _innerProxy;
}