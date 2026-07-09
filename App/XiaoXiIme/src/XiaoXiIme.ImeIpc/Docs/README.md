# dotnetCampus.Ipc

本机内多进程通讯库

| Build | NuGet |
|--|--|
|![](https://github.com/dotnet-campus/dotnetCampus.Ipc/workflows/.NET%20Core/badge.svg)|[![](https://img.shields.io/nuget/v/dotnetCampus.Ipc.svg)](https://www.nuget.org/packages/dotnetCampus.Ipc)|


## 使用方法

库中提供了较为底层的通信方案，也提供了高级的封装方案（基于Json数据格式的通信方案），完整文档可参阅：

- [使用 .NET Remoting 模式的对象远程调用的 IPC 通讯方式](./docs/IpcRemotingObject.md)
- [使用直接路由和 Json 通讯格式的 IPC 通讯方式](./docs/JsonIpcDirectRouted.md)

### 案例：直接路由Json通信（需要2.0.0-alpha版本以上）

#### 步骤一

导入nuget包 **dotnetCampus.Ipc**（需要2.0.0-alpha版本以上），并引入所需要的命名空间；

``` C#
using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;
```

#### 步骤二

创建实际负责IPC通信的代理对象

``` C#

/// <summary>
/// 根据<paramref name="pipeName"/>创建一个 JsonIpcDirectRoutedProvider 对象
/// </summary>
/// <param name="pipeName">不同的IPC对象所使用的管道名称，一个管道名称只能被用于一个IPC对象</param>
/// <returns></returns>
private JsonIpcDirectRoutedProvider CreateJsonIpcDirectRoutedProvider(string pipeName)
{
    // 创建一个 IpcProvider，实际创建管道，进行IPC通信的底层对象
    // 可在 IpcConfiguration 进行详细的配置，包括配置断线重连、日志等级、线程池等等
    var ipcProvider = new IpcProvider(pipeName, new IpcConfiguration());

    // 创建一个 JsonIpcDirectRoutedProvider，封装了通信中的Json数据解析、简化方法调用
    var ipcDirectRoutedProvider = new JsonIpcDirectRoutedProvider(ipcProvider);

    return ipcDirectRoutedProvider;
}

```

#### 步骤三

向IPC对象注册接受到指定消息后的处理函数（如果该IPC对象只负责发送消息，则它不需要注册消息处理回调）

``` C#

var ipcDirectRoutedProvider = CreateJsonIpcDirectRoutedProvider("我是接收消息的IPC对象");

//对无参的通知消息注册回调函数
ipcDirectRoutedProvider.AddNotifyHandler("通知消息A", () => 
{
    Console.WriteLine("我是进程A，我收到了通知消息B，该消息无参数");
});

//对参数类型为ParamType的通知消息注册回调函数
ipcDirectRoutedProvider.AddNotifyHandler<ParamType>("通知消息B", param => 
{
    Console.WriteLine($"我是进程A，我收到了通知消息B，该消息参数：{param.Message}");
});

//对参数类型为ParamType的请求注册回调函数并返回响应数据（可以异步处理响应、也可以无参）
ipcDirectRoutedProvider.AddRequestHandler("请求消息C", (ParamType argument) =>
{
    //处理请求消息C
    var response = new IpcResponse
    {
        Message = $"我是进程A，我收到了请求消息C，该消息参数：{argument.Message}"
    };

    //返回响应数据
    return response;
});

```

#### 步骤四

启动服务

``` C#

var ipcDirectRoutedProvider = CreateJsonIpcDirectRoutedProvider("我是接收消息的IPC对象");

/**
一些消息注册（如果该IPC对象只负责发送消息，则它不需要注册消息处理回调；接受消息的一方需要注册接收到消息后的处理函数）
……
**/

//启动该服务
ipcDirectRoutedProvider.StartServer();

```

#### 步骤五

发送消息（如果该IPC对象只负责接收和处理消息，则它不需要发送消息）

``` C#

var ipcDirectRoutedProvider = CreateJsonIpcDirectRoutedProvider("我是发送消息的IPC对象");
//启动该服务
ipcDirectRoutedProvider.StartServer();
//根据接收方的管道名，获取需要接受到消息的IPC对象，并发送通知
var ipcReceivingObjectA = await ipcDirectRoutedProvider.GetAndConnectClientAsync("我是接收消息的IPC对象");
await ipcReceivingObjectA.NotifyAsync("通知消息A");
await ipcReceivingObjectA.NotifyAsync("通知消息B", new ParamType { Message = "我发送的通知消息是XXX" });
var response = await ipcReceivingObjectA.GetResponseAsync<IpcResponse>("请求消息C", new ParamType { Message = "我发送的请求消息XXX" });

```

#### 调用关系图

![](./docs/image/README/zh-CN/sample0.png)

*更多案例详见：* [Demo](https://github.com/dotnet-campus/dotnetCampus.Ipc/tree/master/demo)

### FAQ

#### AOT 支持

Q: 此 Ipc 库支持 AOT 吗？

A: 此 Ipc 库支持 AOT，但需要注意以下几点：

- 如果是完全使用 byte[] 作为数据传输格式，则不需要任何额外的配置，直接就支持 AOT 了
- 如果是采用 Json 通讯系列，则需要在使用 Json 序列化时，使用 `JsonSerializerOptions` 的 `TypeInfoResolver` 属性来指定类型解析器。具体的配置可以参考 [JsonSerializerOptions](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions?view=dotnet-plat-ext-7.0) 的文档。一般而言，可采用封装好的 UseSystemJsonIpcObjectSerializer 扩展方法辅助传入 `System.Text.Json.Serialization.JsonSerializerContext` 对象，如以下示例代码所示

``` C#
    IpcConfiguration ipcConfiguration = new IpcConfiguration()
    {
        // 进行设置其他配置
    }.UseSystemJsonIpcObjectSerializer(SourceGenerationContext.Default);

    var ipcProvider = new IpcProvider(pipeName, ipcConfiguration);
```

或者注入 IpcConfiguration 的 IpcObjectSerializer 属性，进行更加灵活的序列化配置。此时将不仅限于使用 System.Text.Json 进行序列化，也可以使用其他的序列化方式，如二进制序列化等等

Q: 采用 直接路由 Json 通信（JsonIpcDirectRoutedProvider）时，如果改造让其支持 AOT 编译？

A：如上问所述，可在 IpcConfiguration 里面设置 IpcObjectSerializer 属性，或调用 UseSystemJsonIpcObjectSerializer 扩展辅助方法。如以下示代码所示

``` C#
    // 创建一个 IpcProvider，实际创建管道，进行IPC通信的底层对象
    // 可在 IpcConfiguration 进行详细的配置，包括配置断线重连、日志等级、线程池等等
    IpcConfiguration ipcConfiguration = new IpcConfiguration()
    {
        // 进行设置其他配置
    }.UseSystemJsonIpcObjectSerializer(SourceGenerationContext.Default);
    var ipcProvider = new IpcProvider(pipeName, ipcConfiguration);

    // 创建一个 JsonIpcDirectRoutedProvider，封装了通信中的Json数据解析、简化方法调用
    var ipcDirectRoutedProvider = new JsonIpcDirectRoutedProvider(ipcProvider);
```


## 项目结构图

![](./docs/image/README/zh-CN/Architecture0.png)

## 特点

- 采用两个半工命名管道
- 采用 P2P 方式，每个端都是服务端也是客户端
- 提供 PeerProxy 机制，利用这个机制可以进行发送和接收某个对方的信息
- 追求稳定，而不追求高性能

## 功能

- [x] 通讯建立
- [x] 消息收到回复机制
- [x] 断线重连功能
- [x] 大量异常处理

- [x] 支持裸数据双向传输方式
- [x] 支持裸数据请求响应模式
- [x] 支持字符串消息协议
- [x] 支持远程对象调用和对象存根传输方式
- [x] 支持 NamedPipeStreamForMvc (NamedPipeMvc) 客户端服务器端 MVC 模式
- [x] 支持直接路由的 Json 数据通讯方式


## 感谢

- [jacqueskang/IpcServiceFramework](https://github.com/jacqueskang/IpcServiceFramework)
- [https://github.com/dotnet/aspnetcore](https://github.com/dotnet/aspnetcore) for PipeMVC

## 踩过的坑

- [2019-12-1-构造PipeAccessRule时请不要使用字符串指定Identity - huangtengxiao](https://huangtengxiao.gitee.io/post/%E6%9E%84%E9%80%A0PipeAccessRule%E6%97%B6%E8%AF%B7%E4%B8%8D%E8%A6%81%E4%BD%BF%E7%94%A8%E5%AD%97%E7%AC%A6%E4%B8%B2%E6%8C%87%E5%AE%9AIdentity.html)
