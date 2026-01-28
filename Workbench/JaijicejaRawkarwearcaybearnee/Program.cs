// See https://aka.ms/new-console-template for more information

using System.Text;

using TouchSocket.Core;
using TouchSocket.Http;

using HttpClient = System.Net.Http.HttpClient;

var service = new HttpService();
await service.SetupAsync(new TouchSocketConfig()//加载配置
      .SetListenIPHosts(7789)
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

var httpClient = new HttpClient();
var httpResponseMessage = await httpClient.GetAsync("http://127.0.0.1:7789");
// 以上的 httpResponseMessage 永不返回

Console.Read();
Console.WriteLine("Hello, World!");


class MyHttpPlug1 : IHttpPlugin
{
    public void Dispose()
    {

    }

    public bool DisposedValue { get; set; }
    public void Loaded(IPluginManager pluginManager)
    {
    }

    public void Unloaded(IPluginManager pluginManager)
    {
    }

    public async Task OnHttpRequest(IHttpSessionClient client, HttpContextEventArgs e)
    {
        var request = e.Context.Request;//http请求体
        var response = e.Context.Response;//http响应

        response.SetStatus(200, "success");
        // 此时没有自动设置 Chunk
        using (var stream = response.CreateWriteStream())
        {
            using var streamWriter = new StreamWriter(stream);
            await streamWriter.WriteLineAsync("写什么都没用，因为没有设置 IsChunk");
        }

        await response.CompleteChunkAsync();
        // 此时业务开发者，可能还不知道要调用 CompleteChunk 方法
        //response.AnswerAsync();

        Console.WriteLine("处理/success");
    }
}