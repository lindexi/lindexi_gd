# JsonIpcDirectRouted

使用直接路由和 Json 通讯格式的 IPC 通讯方式

## 概念和特点

这是采用直接路由的调度方式的 IPC 通讯，直接路由可以理解为每个路由是一个字符串，要求请求和响应的代码所标识的字符串相同才能被相互识别。整个 IPC 里的通讯格式采用的是 Json 方式，要求传入和传出的对象都可以被 Json 序列化和反序列化

这是对底层 IPC 的上层封装，提供了业务友好的调用方式。可以支持请求响应和单向通知两个模式，通讯上分为服务端和客户端。服务端可以定义响应和通知的处理逻辑，客户端可以发起对服务端的请求

## 顶层使用方法

### 服务端的创建和定义

服务端的创建需要给服务端一个服务名，此服务名需要和客户端约定，可以让客户端通过此服务名连接上此服务端

```csharp
            // 初始化服务端
            var serverName = "JsonIpcDirectRoutedProviderTest_Request_1";
            var serverProvider = new JsonIpcDirectRoutedProvider(serverName);
```

服务名仅要求是一个合法的管道名即可，一般采用大小写英文字符数字下划线组成，长度不超过 256 个字符

在服务端上可以定义响应和通知的处理逻辑，以下代码定义的是对名为 “Foo1” 的直接路由的请求的处理逻辑

```csharp
            serverProvider.AddRequestHandler("Foo1", (FakeArgument arg) =>
            {
                return new FakeResult("Ok");
            });
```

以上定义的逻辑代码即可让客户端将 FakeArgument 类型的对象，通过请求名为 "Foo1" 的路由地址给到服务端处理。服务端处理之后将会返回 FakeResult 类型的对象

服务端定义通知的处理逻辑例子如下，通知只有从客户端发过来的参数，不需要返回任何对象给到客户端，即客户端只是发过来一条通知给到服务端

```csharp
            var routedPath = "FooPath";
            serverProvider.AddNotifyHandler(routedPath, (FakeArgument arg) =>
            {
            });
```

服务端完成了路由事件的定义之后，即可通过 StartServer 方法进行启动

```csharp
            serverProvider.StartServer();
```

从 IPC 的设计上，要求在 StartServer 启动服务之前完成所有对路由事件的定义。在 StartServer 之后，禁止再 AddRequestHandler 或 AddNotifyHandler 添加处理逻辑。此设计是为了保证消息不丢失，防止存在消息在路由事件定义完成之前收到而丢失

以上连在一起的服务端的定义和启动代码如下

```csharp
            // 初始化服务端
            var serverName = "JsonIpcDirectRoutedProviderTest_Request_1";
            var serverProvider = new JsonIpcDirectRoutedProvider(serverName);

            serverProvider.AddRequestHandler("Foo1", (FakeArgument arg) =>
            {
                return new FakeResult("Ok");
            });

            serverProvider.AddRequestHandler("Foo2", (FakeArgument2 arg) =>
            {
                return new FakeResult2("Ok");
            });

            serverProvider.AddRequestHandler("Foo3", (FakeArgument3 arg) =>
            {
                return new FakeResult3("Ok");
            });

            var routedPath = "FooPath";
            serverProvider.AddNotifyHandler(routedPath, (FakeArgument arg) =>
            {
            });

            serverProvider.AddNotifyHandler("FooPath1", (FakeArgument1 arg) =>
            {
            });

            serverProvider.AddNotifyHandler("FooPath2", (FakeArgument2 arg) =>
            {
            });

            serverProvider.StartServer();
```

### 客户端的创建和通讯

本质上的 JsonIpcDirectRouted 依然是 P2P 的方式，而不是 客户端-服务端 的方式。客户端的创建也需要从 JsonIpcDirectRoutedProvider 获取到

```csharp
            // 创建客户端
            // 允许无参数，如果只是做客户端使用的话
            JsonIpcDirectRoutedProvider clientProvider = new();
            // 对于 clientProvider 来说，可选调用 StartServer 方法
            var clientProxy = await clientProvider.GetAndConnectClientAsync(serverName);
```

无论是服务端还是客户端，都需要创建 JsonIpcDirectRoutedProvider 对象。不同点在于，一个纯客户端的业务逻辑可以不给 JsonIpcDirectRoutedProvider 传入服务名，此时意味着只允许连接别人不允许别人主动连接过来

获取客户端时，需要调用 GetAndConnectClientAsync 方法传入服务端的服务名。如果此时的服务端还没启动，将会在 await 里面异步等待服务端启动且连接上服务端

获取到客户端对象之后，即可对服务器发起请求获取响应，也可以单向给服务端发送通知。以下是对服务端发起请求获取响应的例子

```csharp
            var argument = new FakeArgument("TestName", 1);
            FakeResult result = await clientProxy.GetResponseAsync<FakeResult>("Foo1", argument);
```

以上代码的 GetResponseAsync 第一个参数表示的是所请求的路由地址，第二个参数是一个对象，将会被 Json 序列化然后发送给服务端。返回值的 FakeResult 是服务端处理的返回值

以下是发送通知给服务端的例子

```csharp
            var argument = new FakeArgument("TestName", 1);
            await clientProxy.NotifyAsync("FooPath", argument);
```

发送通知时 await 返回只代表服务端收到了通知，不代表服务端处理通知完成

连在一起的客户端创建和通讯的代码如下

```csharp
            // 创建客户端
            // 允许无参数，如果只是做客户端使用的话
            JsonIpcDirectRoutedProvider clientProvider = new();
            // 对于 clientProvider 来说，可选调用 StartServer 方法
            var clientProxy = await clientProvider.GetAndConnectClientAsync(serverName);

            var result = await clientProxy.GetResponseAsync<FakeResult>("Foo1", argument);

            await clientProxy.NotifyAsync("Foo1", argument);
```

## 高级定制

### 控制 IPC 基础服务

在 JsonIpcDirectRoutedProvider 的构造函数可以传入 IpcProvider 对象。于是可以通过此方式对 IpcProvider 对象进行配置，从而实现对 JsonIpcDirectRoutedProvider 进行底层 IPC 服务的配置，如以下代码

```csharp
            // 初始化服务端
            var serverName = "JsonIpcDirectRoutedProviderTest_Notify_3";
            // 这样的创建方式也是对 IPC 连接最高定制的方式
            IpcProvider ipcProvider = new(serverName);
            var serverProvider = new JsonIpcDirectRoutedProvider(ipcProvider);
```

创建 IpcProvider 过程中，可以进行大量的配置逻辑。以及可以通过 IpcProvider 的 Dispose 方法进行手动释放

### 多个服务端对象共用管道

只需让多个 JsonIpcDirectRoutedProvider 对象共用一个 IpcProvider 即可共用管道。允许多个 JsonIpcDirectRoutedProvider 实例共用相同的 IpcProvider 对象，每个服务端对象处理不同的路由事件，如以下代码

```csharp
            // 初始化服务端
            var serverName = "JsonIpcDirectRoutedProviderTest_Notify_3";
            // 这样的创建方式也是对 IPC 连接最高定制的方式
            IpcProvider ipcProvider = new(serverName);
            var serverProvider1 = new JsonIpcDirectRoutedProvider(ipcProvider);

            serverProvider1.AddNotifyHandler("Foo1", (FakeArgument arg) =>
            {
            });

            // 再次开启一个服务，共用相同的 IpcProvider 对象
            var serverProvider2 = new JsonIpcDirectRoutedProvider(ipcProvider);

            serverProvider2.AddNotifyHandler("Foo2", (FakeArgument arg) =>
            {
            });
            serverProvider1.StartServer();
            serverProvider2.StartServer();
```

此时根据客户端发送的通讯的路由地址，将调度到不同的服务端

```csharp
            // 创建客户端
            // 允许无参数，如果只是做客户端使用的话
            JsonIpcDirectRoutedProvider clientProvider = new();
            var clientProxy = await clientProvider.GetAndConnectClientAsync(serverName);
            await clientProxy.NotifyAsync("Foo1", argument);
            // 预期这条消息是在第二个服务处理的
            await clientProxy.NotifyAsync("Foo2", argument);
```

如果有多个服务端对相同的路由地址进行处理，只有先添加的才能收到消息

### 服务端获取客户端的信息

如果服务端需要了解当前的请求或通知是由哪个客户端发起的，可以通过 AddNotifyHandler 和 AddRequestHandler 方法的高级重载拿到 JsonIpcDirectRoutedContext 对象，如以下代码

```csharp
            serverProvider.AddNotifyHandler<FakeArgument>("Foo2", (arg, context) =>
            {
                // 可以获取到客户端名
                var clientName = context.PeerName;
            });
```

如以上代码即可通过 JsonIpcDirectRoutedContext 拿到 PeerName 客户端名