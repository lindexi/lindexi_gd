// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Sockets;
using HttpClient = System.Net.Http.HttpClient;

var manualResetEventSlim = new ManualResetEventSlim(initialState:false);
_ = Task.Run(() =>
{
    manualResetEventSlim.Wait();

});

await Foo();
try
{
    await Foo();
}
catch (Exception e)
{
    if (e is SocketException socketException)
    {
        // 通常每个套接字地址(协议/网络地址/端口)只允许使用一次
        Debug.Assert(socketException.ErrorCode == 10048);
    }

    Console.WriteLine(e);
}

using var httpClient = new HttpClient();
using var httpResponseMessage = await httpClient.GetAsync("http://127.0.0.1:7799/success");
var responseText = await httpResponseMessage.Content.ReadAsStringAsync();

Console.WriteLine("Hello, World!");
Console.ReadLine();

async Task Foo()
{
    var service = new HttpService();
    await service.SetupAsync(new TouchSocketConfig()//加载配置
        .SetListenOptions(list =>
        {
            list.Add(new TcpListenOption()
            {
                IpHost = new IPHost(IPAddress.Loopback, 7799)
            });
        })
        //.SetListenIPHosts(7789)
        .ConfigureContainer(a =>
        {
            a.AddConsoleLogger();
        })
        .ConfigurePlugins(a =>
        {
            a.Add<MyHttpPlug1>();
            //default插件应该最后添加，其作用是
            //1、为找不到的路由返回404
            //2、处理 header 为Option的探视跨域请求。
            a.UseDefaultHttpServicePlugin();
        }));
    await service.StartAsync();
}

class MyHttpPlug1 : PluginBase, IHttpPlugin
{
    public async Task OnHttpRequest(IHttpSessionClient client, HttpContextEventArgs e)
    {
        #region HttpContext生命周期
        var request = e.Context.Request;//http请求体
        var response = e.Context.Response;//http响应体
        #endregion

        if (request.IsGet() && request.UrlEquals("/success"))
        {
            //直接响应文字
            await response
                .SetStatus(200, "success")
                .FromText("Success成功的输出信息")
                .AnswerAsync();//直接回应
            Console.WriteLine("处理/success");
            return;
        }


        //无法处理，调用下一个插件
        await e.InvokeNext();
    }
}