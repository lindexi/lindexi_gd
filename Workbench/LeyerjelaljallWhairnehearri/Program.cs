// See https://aka.ms/new-console-template for more information

using System.Text;
using TouchSocket.Core;
using TouchSocket.Http;

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

Console.Read();
Console.WriteLine("Hello, World!");


class MyHttpPlug1:IHttpPlugin
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

        //直接响应文字
        response.StatusCode = 200;
        //response.SetStatus(400, "xxx");

        //response.FromText("qase");
        //response.SetContentLength(0);

        //response.SetContent("123");
        response.IsChunk = true;

        //using (var stream = response.CreateWriteStream())
        //{
        //    using var streamWriter = new StreamWriter(stream);
        //    streamWriter.WriteLine("abcasdasdasdasdasdasdasdaabcasdasdasdasdasdasdasdaabcasdasdasdasdasdasdasdaabcasdasdasdasdasdasdasdaabcasdasdasdasdasdasdasda");
        //}

        var textBuffer = Encoding.UTF8.GetBytes("abcasdasdasdasdasdasdasdaabcasdasdasdasdasdasdasdaabcasdasdasdasdasdasdasdaabcasdasdasdasdasdasdasdaabcasdasdasdasdasdasdasda");
        await response.WriteAsync(textBuffer);

        //await response.AnswerAsync();
        await response.CompleteChunkAsync();

        //await response
        //    .SetStatus(200, "success")
        //    .FromText("Success")
        //    .AnswerAsync();//直接回应
        Console.WriteLine("处理/success");
    }
}