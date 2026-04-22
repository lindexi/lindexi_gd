using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;

using System.ClientModel;
using System.Text;
using System.Xml;
using System.Xml.Linq;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
});

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");
var agent = chatClient.AsIChatClient()
    .AsBuilder()
    .BuildAIAgent(new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                //AIFunctionFactory.Create(OpenApplication),
                //AIFunctionFactory.Create(WriteFileInfo)
            ]
        }
    });

var prompt =
    $"""
    请你阅读我的以下几篇博客内容，总结出模仿我博客的编写习惯和编写方法，准备用来让你编写新的博客内容
    
    [博客1开始]
    $(Blog1)
    [博客1结束]
    
    [博客2开始]
    $(Blog2)
    [博客2结束]
    
    [博客3开始]
    $(Blog3)
    [博客3结束]
    
    请先按照我的要求编写新的博客内容，完成编写之后，你需要回顾我给你的博客内容，审查你新写的博客是否符合我的博客编写风格
    
    以下是我的博客需求：
    $(Input)
    """;

var blog1 =
    """"
    # dotnet 对接豆包模型时如何控制是否进入思考模式
    
    本文记录使用 OpenAI 的 API 和豆包进行对接的时候，如何控制豆包是否进入思考模式。本文内容可直接与 Microsoft Agent Framework 对接
    
    <!--more-->
    
    按照豆包官方文档可以知道，通过 `thinking` 的类型可以控制豆包是否进入思考模式，官方文档请看： <https://www.volcengine.com/docs/82379/1449737>
    
    可选参数如下：
    
    - enabled：强制开启，强制开启深度思考能力。
    - disabled：强制关闭深度思考能力。
    - auto：模型自行判断是否进行深度思考
    
    按照 GitHub 上的 <https://github.com/openai/openai-dotnet/issues/132> 可了解到对应的 OpenAI 的调用方法是通过 ChatCompletionOptions 的 Patch 属性设置，代码如下
    
    ```csharp
    var chatCompletionOptions = new ChatCompletionOptions();
    
    #pragma warning disable SCME0001
    
    // https://www.volcengine.com/docs/82379/1449737
    // 提供 thinking 字段控制是否关闭深度思考能力，实现“复杂任务深度推理，简单任务高效响应”的精细控制，获得成本、效率收益
    // 取值说明：
    // enabled：强制开启，强制开启深度思考能力。
    // disabled：强制关闭深度思考能力。
    // auto：模型自行判断是否进行深度思考。
    chatCompletionOptions.Patch.Set("$.thinking"u8, BinaryData.FromString("""{ "type": "disabled" }"""));
    ```
    
    这里需要明确使用 `#pragma warning disable SCME0001` 开启实验性功能。这是因为 Patch 属性现在官方还没考虑好，还不确定是否如此开放，因此被标记了实验性功能
    
    在调用 CompleteChatStreamingAsync 方法的时候，将以上的 ChatCompletionOptions 传递进去就可以了，代码如下
    
    ```csharp
    await foreach (var streamingChatCompletionUpdate in chatClient.CompleteChatStreamingAsync([new UserChatMessage("你好")], chatCompletionOptions))
    {
    }
    ```
    
    以上方法可以配合在 Microsoft Agent Framework 的 `Microsoft.Agents.AI.OpenAI` 库使用，对应调用豆包的 `https://ark.cn-beijing.volces.com/api/v3` 地址即可
    
    全部演示代码如下
    
    ```csharp
    using OpenAI;
    using OpenAI.Chat;
    using System;
    
    var keyFile = @"C:\lindexi\Work\Doubao.txt";
    var key = File.ReadAllText(keyFile); // 请换成你自己的 key 哦
    
    var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
    {
        Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
    
    });
    
    var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg"); // 请换成你自己的模型
    var chatCompletionOptions = new ChatCompletionOptions();
    chatCompletionOptions.Patch.Set("$.thinking"u8, BinaryData.FromString("""{ "type": "disabled" }"""));
    
    await foreach (var streamingChatCompletionUpdate in chatClient.CompleteChatStreamingAsync([new UserChatMessage("你好")], chatCompletionOptions))
    {
        ...
    }
    ```
    
    请自行尝试切换 `thinking` 的模式，运行测试控制台输出内容
    """";

var blog2 =
    """
    # Avalonia 实现离屏渲染能力
    
    本文将告诉大家如何在 Avalonia 实现跨平台的离屏渲染能力
    
    <!--more-->
    
    我的需求是拿 Avalonia 当成一些图形画面渲染的框架，准备在 Linux 和 Windows 设备上使用。刚好 Avalonia 做好了图形画面渲染的平台隔离能力，再有提供类 WPF 的布局方式，可以让我制作一些精妙的界面内容
    
    我开始在 GitHub 上搜到 <https://github.com/AvaloniaUI/Avalonia/issues/2174> 这个帖子，一开始我按照 @maxkatz6 介绍的方法，顺利地在 Windows 上使用了 EmbeddableControlRoot 进行离屏渲染
    
    然而以上方法在 Linux 上将会抛出 NotSupportedException 异常，导致完全不可用
    
    我仔细阅读了 <https://github.com/AvaloniaUI/Avalonia/issues/2174> 这个帖子，按照 @kekekeks 提供的方法，尝试自己实现 ITopLevelImpl 接口的方式实现了在 Linux 上也能支持离屏渲染能力
    
    实现的做法如下：如 @kekekeks 所教的方法，咱需要先在 csproj 项目文件里面使用 `<AvaloniaAccessUnstablePrivateApis>true</AvaloniaAccessUnstablePrivateApis>` 用于解决构建问题。随后编写一个名为 OffscreenTopLevelImpl 的类型，继承自 Avalonia.Controls.Embedding.Offscreen.OffscreenTopLevelImplBase 类型，其代码如下
    
    ```csharp
    class OffscreenTopLevelImpl : OffscreenTopLevelImplBase, ITopLevelImpl
    {
        public override IEnumerable<object> Surfaces { get; } = [];
        public override IMouseDevice MouseDevice { get; } = new MouseDevice();
    }
    ```
    
    最后将以上定义的 OffscreenTopLevelImpl 放入到 EmbeddableControlRoot 的构造函数里。请记得为 OffscreenTopLevelImpl 的 ClientSize 赋上合适的尺寸，否则你将得到一张空白渲染的图片内容
    
    后续的部分就和 @maxkatz6 介绍的那样做了，代码如下
    
    ```csharp
            var imageFilePath = Path.Join(Path.GetTempPath(), $"{Path.GetRandomFileName()}.png");
    
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var taskCompletionSource = new TaskCompletionSource();
    
                // https://github.com/AvaloniaUI/Avalonia/issues/2174#issuecomment-3030306384
                var offscreenTopLevelImpl = new OffscreenTopLevelImpl()
                {
                    ClientSize = new Size(1000, 600)
                };
                var embeddableControlRoot = new EmbeddableControlRoot(offscreenTopLevelImpl);
                embeddableControlRoot.Width = 1000;
                embeddableControlRoot.Height = 600;
                var mainView = new MainView();
                mainView.Loaded += (sender, args) =>
                {
                    using var renderTargetBitmap = new RenderTargetBitmap(new PixelSize(1000, 600));
                    renderTargetBitmap.Render(mainView);
                    renderTargetBitmap.Save(imageFilePath);
                    taskCompletionSource.SetResult();
                };
                embeddableControlRoot.Content = mainView;
    
                // 准备离屏渲染工作
                embeddableControlRoot.Prepare(); // 调用此方法会触发 Loaded 事件
                embeddableControlRoot.StartRendering();
    
                await taskCompletionSource.Task;
    
                embeddableControlRoot.StopRendering();
                embeddableControlRoot.Dispose();
            });
    
            return imageFilePath;
    ```
    
    通过以上方式，就能够在 Linux 和 Windows 上实现离屏渲染的能力。请特别记得给 OffscreenTopLevelImpl 赋值上尺寸，否则将拿到一张空白的图片。
    """;

var blog3 =
    """
    # dotnet C# 高性能配置文件读写库 dotnetCampus.Configurations 简介
    
    在应用程序运行的时，需要根据不同的配置执行不同的内容。有很多根据配置而初始化的功能往往是在应用程序启动的时候需要执行。对于很多类型的应用程序，特别是客户端的应用程序，启动的性能特别重要。也因此，在启动过程中需要依赖配置文件的不同配置而启动不同的功能时，就对配置文件的读写和解析性能提出了很高的要求
    
    本文来和大家简单介绍我团队开源的 dotnetCampus.Configurations 高性能配置文件读写库。这个库不仅包含了配置文件的读取解析，还包括了自定义配置文件格式，也就是 COIN 硬币格式的配置文件。提供了多线程和多进程的读写安全的功能和毫秒级的配置文件读取解析性能，以及最低支持到 .NET Framework 4.5 框架
    
    <!--more-->
    
    ## 背景
    
    我有很多个客户端 .NET 应用程序，我需要在客户端启动的过程中，读取一些配置文件，包括机器级配置和用户级配置。原本一开始我的应用程序都是采用先启动通用逻辑，将通用界面显示出来，接着慢慢去读取配置文件，根据配置文件展开不同的功能
    
    后面产品变心了，加了一些有趣的功能。如换肤等功能，此时就需要在第一个界面显示出来之前就需要读取配置了。我写了另一篇博客 [C# 配置文件存储 各种序列化算法性能比较](https://blog.lindexi.com/post/C-%E9%85%8D%E7%BD%AE%E6%96%87%E4%BB%B6%E5%AD%98%E5%82%A8-%E5%90%84%E7%A7%8D%E5%BA%8F%E5%88%97%E5%8C%96%E7%AE%97%E6%B3%95%E6%80%A7%E8%83%BD%E6%AF%94%E8%BE%83.html) 告诉大家各个配置文件的读取性能和序列化解析性能
    
    但是现在通用的 XML 或 JSON 或 INI 等格式的性能，尽管看起来足够快了，但放在启动过程这个业务里面，依然显得性能不够。在启动的流程，每一个毫秒都是非常重要的。于是我所在的 CBB 公共组件团队就对配置文件的读取和解析有了性能上的要求，在基准测试机器人，能够在 10 毫秒内完全读取完成一份基准的配置文件。然而对于通用如上几个格式的文件来说，几乎没有一个能在小于 90 毫秒内完成。这就使得我需要去寻找一个更快的配置文件读写方式
    
    在后续的产品迭代中，有几个产品的应用是允许用户多开的，开启多个进程的时候，也需要进行读写相同的一个配置文件。此时就出现另一个问题，如何保证配置的读写是进程级安全的
    
    综合考虑了之后，在太子的带领下，开发和开源了 dotnetCampus.Configurations 硬币格式的高性能配置文件读写库
    
    为什么叫硬币 COIN 呢，原因是取自 `COIN = Configuration\n` 即“配置+换行符”，因默认使用“\n”作为换行符而得名
    
    ## 开源
    
    这是基于最友好的 MIT 协议的在 GitHub 完全开源的仓库，请看 [https://github.com/dotnet-campus/dotnetCampus.Configurations](https://github.com/dotnet-campus/dotnetCampus.Configurations)
    
    此配置文件库完全百分百使用 C# 编写，支持如下 .NET 框架
    
    - netstandard2.0
    - net45
    - netcoreapp3.0
    
    等等 .NET 5 和 .NET 6 呢？在 .NET 5 或更高版本将会自动使用 .NET Core 3.0 的库，放心，这是完全 IL 级兼容的。为什么要有 .NET Standard 2.0 的？ 因为还要给 Xamarin 做兼容哦。对于 .NET Framework 系列的，最低要求是 .NET Framework 4.5 版本，对于更高的 .NET Framework 版本，也将会自动引用 .NET Framework 4.5 版本，放心，这也是完全 IL 级兼容的
    
    本库已在超过 500 万台设备上稳定运行超过一年时间，还请放心使用
    
    ## 使用方法
    
    介绍了那么多，是时候来看看此配置文件库的使用方法
    
    按照惯例，在使用 .NET 库只需要两步，第一是通过 NuGet 安装，第二是开始使用。本文的硬币格式的高性能配置库也是通过 NuGet 分发的，包含了两个分支版本，分别是传统的 DLL 版本的 NuGet 和源代码两个版本。为了方便起见，咱先来介绍传统的 DLL 版本的使用方法
    
    右击项目管理 NuGet 程序包，在浏览里面搜寻 dotnetCampus.Configurations 进行安装
    
    在命令行使用如下代码即可给项目安装上硬币格式的高性能配置文件读写库
    
    ```
    dotnet add package dotnetCampus.Configurations
    ```
    
    除了使用命令行安装之外，对于 SDK 风格的新 csproj 项目格式的项目，可以编辑 csproj 文件，在 csproj 文件上添加如下代码进行安装
    
    ```xml
    <PackageReference Include="dotnetCampus.Configurations" Version="1.6.8" />
    ```
    
    使用硬币格式的高性能配置文件读写库时，需要传入配置文件所在的路径，如以下代码
    
    ```csharp
    // 使用一个文件路径创建默认配置的实例。文件可以存在也可以不存在，甚至其所在的文件夹也可以不需要提前存在。
    // 这里的配置文件后缀名 coin 是 Configuration\n，即 “配置+换行符” 的简称。你也可以使用其他扩展名，因为它实际上只是 UTF-8 编码的纯文本而已。
    var configs = DefaultConfiguration.FromFile(@"C:\Users\lvyi\Desktop\walterlv.coin");
    ```
    
    在获取到 configs 变量之后，即可对此变量进行读写，如下面代码
    
    获取值：
    
    ```csharp
    // 获取配置 Foo 的字符串值。
    // 这里的 value 一定不会为 null，如果文件不存在或者没有对应的配置项，那么为空字符串。
    string value0 = configs["Foo"];
    
    // 获取字符串值的时候，如果文件不存在或者没有对应的配置项，那么会使用默认值（空传递运算符 ?? 可以用来指定默认值）。
    string value1 = configs["Foo"] ?? "anonymous";
    ```
    
    设置值：
    
    ```csharp
    // 设置配置 Foo 的字符串值。
    configs["Foo"] = "lvyi";
    
    // 可以设置为 null，但你下次再次获取值的时候却依然保证不会返回 null 字符串。
    configs["Foo"] = null;
    
    // 可以设置为空字符串，效果与设置为 null 是等同的。
    configs["Foo"] = "";
    ```
    
    ## 在大型项目中使用
    
    实际应用中，应该将 configs 缓存起来，而不是每次使用的时候，都通过 DefaultConfiguration.FromFile 去创建新的对象
    
    初始化：
    
    ```csharp
    // 这里是大型项目配置初始化处的代码。
    // 此类型中包含底层的配置读写方法，而且所有读写全部是异步的，防止影响启动性能。
    var configFileName = @"C:\Users\lvyi\Desktop\walterlv.coin";
    var config = ConfigurationFactory.FromFile(configFileName);
    
    // 如果你需要对整个应用程序公开配置，那么可以公开 CreateAppConfigurator 方法返回的新实例。
    // 这个实例的所有配置读写全部是同步方法，这是为了方便其他模块使用。
    // 以下是 Container 即是容器，放入到容器中相当于全局单例
    Container.Set<IAppConfigurator>(config.CreateAppConfigurator());
    ```
    
    在业务模块中定义类型安全的配置类：
    
    ```csharp
    internal class StateConfiguration : Configuration
    {
        /// <summary>
        /// 获取或设置整型。
        /// </summary>
        internal int? Count
        {
            get => GetInt32();
            set => SetValue(value);
        }
    
        /// <summary>
        /// 获取或设置带默认值的整型。
        /// </summary>
        internal int Length
        {
            get => GetInt32() ?? 2;
            set => SetValue(Equals(value, 2) ? null : value);
        }
    
        /// <summary>
        /// 获取或设置布尔值。
        /// </summary>
        internal bool? State
        {
            get => GetBoolean();
            set => SetValue(value);
        }
    
        /// <summary>
        /// 获取或设置字符串。
        /// </summary>
        internal string Value
        {
            get => GetString();
            set => SetValue(value);
        }
    
        /// <summary>
        /// 获取或设置带默认值的字符串。
        /// </summary>
        internal string Host
        {
            get => GetString() ?? "https://localhost:17134";
            set => SetValue(Equals(value, "https://localhost:17134") ? null : value);
        }
    
        /// <summary>
        /// 获取或设置非基元值类型。
        /// </summary>
        internal Rect? Screen
        {
            get => this.GetValue<Rect>();
            set => this.SetValue<Rect>(value);
        }
    }
    ```
    
    在业务模块中使用：
    
    ```csharp
    private readonly IAppConfiguration _config = Container.Get<IAppConfigurator>(); // 从 Container 容器获取，相当于从单例获取对象
    
    // 读取配置。
    private void Restore()
    {
        var config = _config.Of<StateConfiguration>();
        var bounds = config.Screen;
        if (bounds != null)
        {
            // 恢复窗口位置和尺寸。
        }
    }
    
    // 写入配置。
    public void Update()
    {
        var config = _config.Of<StateConfiguration>();
        config.Screen = new Rect(0, 0, 3840, 2160);
    }
    ```
    
    ## 配置文件格式
    
    配置文件读写库的性能，除了代码层面的影响，更重要的是配置文件格式的影响。为了做到尽可能的高性能，于是重新设置了一套配置文件格式，这就是 COIN 硬币配置文件格式
    
    配置格式如下
    
    配置文件以行为单位，将行首是 `>` 字符的行作为注释，在 `>` 后面的内容将会被忽略。在第一个非 `>` 字符开头的行作为 `Key` 值，在此行以下直到文件结束或下一个 `>` 字符开始的行之间内容作为 `Value` 值
    
    ```
    > 配置文件
    > 版本 1.0
    State.BuildLogFile
    xxxxx
    > 注释内容
    Foo
    这是第一行
    这是第二行
    >
    > 配置文件结束
    ```
    
    此配置文件格式不支持树型结构，而是 Key-Value 方式。作为配置文件是足够的，但是作为存储文件格式却是不适合的，这就是和 XML 和 JSON 最大的差别
    
    ## 特性
    
    1. 高性能读写
        - 在初始化阶段使用全异步处理，避免阻塞主流程。
        - 使用特别为高性能读写而设计的配置文件格式。
        - 多线程和多进程安全高性能读写
    1. 无异常设计
        - 所有配置项的读写均为“无异常设计”，你完全不需要在业务代码中处理任何异常。
        - 为防止业务代码中出现意料之外的 `NullReferenceException`，所有配置项的返回值均不为实际意义的 `null`。
            - 值类型会返回其对应的 `Nullable<T>` 类型，这是一个结构体，虽然有 `null` 值，但不会产生空引用。
            - 引用类型仅提供字符串，返回 `Nullable<ConfigurationString>` 类型，这也是一个结构体，你可以判断 `null`，但实际上不可能为 `null`。
    1. 全应用程序统一的 API
        - 在大型应用中开放 API 时记得使用 `CreateAppConfigurator()` 来开放，这会让整个应用程序使用统一的一套配置读写 API，且完全的 IO 无感知。
    """;

var message = prompt.Replace("$(Blog1)", blog1)
    .Replace("$(Blog2)", blog2)
    .Replace("$(Blog3)", blog3);

var xElement = new XElement("Content", message);
var xmlText = xElement.ToString(SaveOptions.None);

var input =
    """"
    根据我提供的代码内容和知识点，帮我编写一篇关于使用 OpenILink.SDK 制作微信聊天机器人且与豆包大模型对接的博客
    
    知识点如下：
    
    OpenILink.SDK 是一个第三方的开源项目，支持以下框架版本：
    
    - `net462`
    - `netstandard2.0`
    - `net8.0`
    
    开源地址是： https://github.com/openilink/openilink-sdk-csharp/
    
    代码我拉下来看了，感觉 AI 贡献了很多，但总体质量还行。我的评价是优于 树上的小猫咪 的项目的质量
    
    同一个 NuGet 包可以同时用于老的 `.NET Framework`、`.NET Core`
    和现代 .NET。
    
    官方帮助文档：
    
    ## 快速开始
    
    ```csharp
    using OpenILink.SDK;
    
    var tokenPath = "bot_token.txt";
    var bufferPath = "get_updates_buf.txt";
    
    // 这里是不要求 tokenPath 文件一定存在的。这个 tokenPath 存在的意义只在于如果存在的话，可以让你跳过扫码登录的步骤，直接使用之前登录成功后保存的 token 来登录
    // 首次运行的话，肯定是不存在tokenPath 文件的，自然就会进入到 if (string.IsNullOrWhiteSpace(client.Token)) 分支，让你扫码登录。登录成功后 SDK 会自动将 token 写入到 tokenPath 文件里，这样下次运行的时候就可以直接使用了
    using var client = OpenILinkClient.Create(ReadText(tokenPath));
    
    if (string.IsNullOrWhiteSpace(client.Token))
    {
        var login = await client.LoginWithQrAsync(ShowQrCode, OnScanned);
        if (!login.Connected)
        {
            Console.Error.WriteLine($"登录失败: {login.Message}");
            return;
        }
    
        File.WriteAllText(tokenPath, login.BotToken ?? string.Empty);
    }
    
    await client.MonitorAsync(HandleMessageAsync, new MonitorOptions
    {
        InitialBuffer = ReadText(bufferPath),
        OnBufferUpdated = SaveBuffer,
        OnError = ReportError,
        OnSessionExpired = ReportSessionExpired
    });
    
    Task HandleMessageAsync(WeixinMessage message)
    {
        var text = message.ExtractText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.CompletedTask;
        }
    
        Console.WriteLine($"[{message.FromUserId}] {text}");
        return client.ReplyTextAsync(message, $"echo: {text}");
    }
    
    void ShowQrCode(string qrCodeImageUrl)
    {
       // 这里拿到的 qrCodeImageUrl 是一个 URL 地址，指向了一个微信扫描的页面，这个页面里面没有直接的二维码图片，而是使用 canvas 的方式绘制了一张二维码。因此直接和程序对接也是不方便的，微信在这一点上还是很坑的
        Console.WriteLine(qrCodeImageUrl);
    }
    
    void OnScanned()
    {
        Console.WriteLine("已扫码，请在微信端确认。");
    }
    
    void SaveBuffer(string buffer)
    {
        // 这里的 Buffer 的作用是在下次启动的时候，可以作为 InitialBuffer 进行传递。这样就可以减少收到的消息。按照微信的设计，如果没有 Buffer 的话，SDK 会从最近一段时间的消息里去拉取未读消息，这样就可能会收到很多不必要的消息。而如果有 Buffer 的话，SDK 就会从 Buffer 里记录的消息 ID 开始去拉取未读消息，这样就可以避免收到过多的历史消息了
        File.WriteAllText(bufferPath, buffer);
    }
    
    void ReportError(Exception exception)
    {
        Console.Error.WriteLine(exception.Message);
    }
    
    void ReportSessionExpired()
    {
        Console.Error.WriteLine("会话过期，请重新登录。");
    }
    
    static string ReadText(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
    }
    ```
    
    ## 创建客户端
    
    最常用的三种写法：
    
    ```csharp
    using var client = OpenILinkClient.Create(token);
    ```
    
    ```csharp
    using var client = new OpenILinkClient(token);
    ```
    
    ```csharp
    using var httpClient = new HttpClient();
    
    using var client = OpenILinkClient.Builder()
        .Token(token)
        .BaseUri("https://ilinkai.weixin.qq.com/")
        .CdnBaseUri("https://novac2c.cdn.weixin.qq.com/c2c/")
        .RouteTag("gray-route")
        .HttpClient(httpClient)
        .ApiTimeout(TimeSpan.FromSeconds(15))
        .LongPollingTimeout(TimeSpan.FromSeconds(35))
        .Build();
    ```
    
    如果你是从配置系统里读参数，直接用 `OpenILinkClientOptions`：
    
    ```csharp
    var options = new OpenILinkClientOptions(token)
    {
        BaseUri = new Uri("https://ilinkai.weixin.qq.com/"),
        CdnBaseUri = new Uri("https://novac2c.cdn.weixin.qq.com/c2c/"),
        RouteTag = "gray-route",
        LoginTimeout = TimeSpan.FromMinutes(8)
    };
    
    using var client = new OpenILinkClient(options);
    ```
    
    ## 登录
    
    首次启动通常没有 `bot_token`，直接扫码：
    
    ```csharp
    var login = await client.LoginWithQrAsync(ShowQrCode, OnScanned, OnExpired);
    
    if (login.Connected)
    {
        File.WriteAllText("bot_token.txt", login.BotToken ?? string.Empty);
    }
    
    void ShowQrCode(string qrCodeImage)
    {
        Console.WriteLine(qrCodeImage);
    }
    
    void OnScanned()
    {
        Console.WriteLine("已扫码，请确认。");
    }
    
    void OnExpired(int attempt, int maxAttempt)
    {
        Console.WriteLine($"二维码过期，正在刷新 ({attempt}/{maxAttempt})");
    }
    ```
    
    登录成功后 SDK 会自动更新：
    
    - `client.Token`
    - `client.BaseUri`
    
    下次启动时直接复用 `bot_token` 即可。
    
    ## 接收消息
    
    ```csharp
    await client.MonitorAsync(HandleMessageAsync, new MonitorOptions
    {
        InitialBuffer = ReadText("get_updates_buf.txt"),
        OnBufferUpdated = buffer => File.WriteAllText("get_updates_buf.txt", buffer),
        OnError = exception => Console.Error.WriteLine(exception.Message),
        OnSessionExpired = () => Console.Error.WriteLine("会话过期")
    });
    
    Task HandleMessageAsync(WeixinMessage message)
    {
        var text = message.ExtractText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.CompletedTask;
        }
    
        return client.ReplyTextAsync(message, $"收到: {text}");
    }
    ```
    
    `MonitorAsync` 会自动：
    
    - 重试和退避
    - 跟进服务端返回的 `longpolling_timeout_ms`
    - 缓存每个用户的 `contextToken`
    - 推进 `get_updates_buf`
    
    ## 回复和主动推送
    
    收到消息后，优先直接回复：
    
    ```csharp
    await client.ReplyTextAsync(message, "你好");
    ```
    
    需要主动推送时：
    
    ```csharp
    if (client.CanPushTo(userId))
    {
        await client.PushTextAsync(userId, "这是一条主动消息");
    }
    ```
    
    也可以显式读取缓存的上下文：
    
    ```csharp
    var contextToken = client.GetContextToken(userId);
    ```
    
    ## 输入状态和 Bot 配置
    
    ```csharp
    var config = await client.GetConfigAsync(userId, contextToken);
    await client.SendTypingAsync(userId, config.TypingTicket ?? string.Empty, TypingStatus.Typing);
    ```
    
    ## 媒体上传和发送
    
    最省心的写法：
    
    ```csharp
    var bytes = File.ReadAllBytes("photo.jpg");
    await client.SendMediaFileAsync(toUserId, contextToken, bytes, "photo.jpg", "看看这张图");
    ```
    
    需要手动控制上传和发送时：
    
    ```csharp
    var bytes = File.ReadAllBytes("photo.jpg");
    var upload = await client.UploadFileAsync(bytes, toUserId, UploadMediaType.Image);
    
    await client.SendImageAsync(toUserId, contextToken, upload);
    ```
    
    同理也可以调用：
    
    - `SendVideoAsync`
    - `SendFileAttachmentAsync`
    
    ## 下载文件和语音
    
    下载文件：
    
    ```csharp
    var plaintext = await client.DownloadFileAsync(
        media.EncryptQueryParam ?? string.Empty,
        media.AesKey ?? string.Empty);
    ```
    
    下载语音前，需要先注入 `ISilkDecoder`：
    
    ```csharp
    public sealed class MySilkDecoder : ISilkDecoder
    {
        public Task<byte[]> DecodeAsync(byte[] silkData, int sampleRate, CancellationToken cancellationToken)
        {
            return Task.FromResult(Array.Empty<byte>());
        }
    }
    
    using var client = OpenILinkClient.Builder()
        .Token(token)
        .SilkDecoder(new MySilkDecoder())
        .Build();
    
    var wav = await client.DownloadVoiceAsync(voiceItem);
    ```
    
    ## 工具方法
    
    ```csharp
    var text = message.ExtractText();
    var isMedia = MessageUtilities.IsMediaItem(item);
    var mime = MimeUtilities.MimeFromFilename("photo.jpg");
    var extension = MimeUtilities.ExtensionFromMime("video/mp4");
    var isImage = MimeUtilities.IsImageMime("image/png");
    var isVideo = MimeUtilities.IsVideoMime("video/mp4");
    ```
    
    ## 异常处理
    
    ```csharp
    try
    {
        await client.PushTextAsync(userId, "hello");
    }
    catch (MissingContextTokenException)
    {
        Console.WriteLine("该用户还没有可用的 contextToken。");
    }
    catch (OpenILinkApiException exception) when (exception.IsSessionExpired())
    {
        Console.WriteLine("会话过期，请重新登录。");
    }
    catch (OpenILinkHttpException exception)
    {
        Console.WriteLine($"HTTP 状态码: {exception.StatusCode}");
    }
    ```
     
    我提供的代码示例：
    
    OpenILinkDemo.csproj
    
    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
    
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
      </PropertyGroup>
    
      <ItemGroup>
        <PackageReference Include="OpenILink.SDK" Version="1.0.0" />
      </ItemGroup>
      <ItemGroup>
        <PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0" />
      </ItemGroup>
    </Project>
    ```
    
    Program.cs:
    
    using Microsoft.Agents.AI;
    using Microsoft.Extensions.AI;
    
    using OpenAI;
    
    using OpenILink.SDK;
    
    using System.ClientModel;
    using System.Diagnostics;
    
    var keyFile = @"C:\lindexi\Work\Doubao.txt";
    var key = File.ReadAllText(keyFile);
    
    var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
    {
        Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
    });
    
    var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");
    var agent = chatClient.AsIChatClient()
        .AsBuilder()
        .BuildAIAgent(new ChatClientAgentOptions()
        {
            ChatOptions = new ChatOptions()
            {
                Tools =
                [
                    //AIFunctionFactory.Create(OpenApplication),
                    //AIFunctionFactory.Create(WriteFileInfo)
                ]
            }
        });
    
    var tokenFilePath = Path.Join(AppContext.BaseDirectory, "Token.txt");
    string? initialToken = null;
    // 初次扫码之前，这个 Token 肯定是空的
    if (File.Exists(tokenFilePath))
    {
        initialToken = File.ReadAllText(tokenFilePath);
    }
    
    // 存放用于读取哪条信息的 Buffer 的内容
    var bufferPath = Path.Join(AppContext.BaseDirectory, "GetUpdatesBuffer.txt");
    string? initBuffer = null;
    if (File.Exists(bufferPath))
    {
        initBuffer = File.ReadAllText(bufferPath);
    }
    
    var client = OpenILinkClient.Create(initialToken);
    
    if (string.IsNullOrWhiteSpace(client.Token))
    {
        var login = await client.LoginWithQrAsync(ShowQrCode, OnScanned);
        if (!login.Connected)
        {
            Console.Error.WriteLine($"登录失败: {login.Message}");
            return;
        }
    
        Console.WriteLine($"Token={login.BotToken}");
    }
    
    File.WriteAllText(tokenFilePath, client.Token);
    
    await client.MonitorAsync(HandleMessageAsync, new MonitorOptions
    {
        InitialBuffer = initBuffer,
        OnBufferUpdated = SaveBuffer,
        OnError = ReportError,
        OnSessionExpired = ReportSessionExpired
    });
    
    Console.WriteLine("Hello, World!");
    
    
    void ShowQrCode(string qrCodeImageDownloadUrl)
    {
        Process.Start(new ProcessStartInfo(qrCodeImageDownloadUrl)
        {
            UseShellExecute = true
        });
    }
    
    void OnScanned()
    {
        Console.WriteLine("已扫码，请在微信端确认。");
    }
    
    async Task HandleMessageAsync(WeixinMessage message)
    {
        var getConfigResponse = await client.GetConfigAsync(message.FromUserId, message.ContextToken!);
    
        var text = message.ExtractText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
    
        await client.SendTypingAsync(message.FromUserId, getConfigResponse.TypingTicket!, TypingStatus.Typing);
        await Task.Delay(TimeSpan.FromSeconds(5));
    
        Console.WriteLine($"[{message.FromUserId}] {text}");
    
        var agentResponse = await agent.RunAsync
        (
            [
                new ChatMessage(ChatRole.System,"你是一个充满积极向上情绪的聊天机器人"),
                new ChatMessage(ChatRole.User, text)
            ]
        );
    
        var reason = string.Empty; // 为什么直接用 string 类型？因为预期只有一项
    
        foreach (ChatMessage agentResponseMessage in agentResponse.Messages)
        {
            foreach (var textReasoningContent in agentResponseMessage.Contents.OfType<TextReasoningContent>())
            {
                reason += textReasoningContent.Text;
            }
        }
    
        var responseText = $"";
        if (!string.IsNullOrEmpty(reason))
        {
            responseText =
                $"""
                 思考：
                 {reason.Trim()}
                 -----------
                 {agentResponse.Text}
                 """;
        }
    
        if (agentResponse.Usage is { } usage)
        {
            var usageText = $"本次对话总Token消耗：{usage.TotalTokenCount};输入Token消耗：{usage.InputTokenCount};输出Token消耗：{usage.OutputTokenCount},其中思考占{usage.ReasoningTokenCount??0}";
            responseText += $"\r\n-----------\r\n{usageText}";
        }
    
        Console.WriteLine($"[Bot] {responseText}");
    
        await client.ReplyTextAsync(message, responseText);
    }
    
    void SaveBuffer(string buffer)
    {
        File.WriteAllText(bufferPath, buffer);
    }
    
    void ReportError(Exception exception)
    {
        Console.Error.WriteLine(exception.Message);
    }
    
    void ReportSessionExpired()
    {
        Console.Error.WriteLine("会话过期，请重新登录。");
    }
    """";

message = message.Replace("$(Input)", input);

var isThinking = false;
var thinkingStringBuilder = new StringBuilder();
var contentStringBuilder = new StringBuilder();
await foreach (var agentResponseUpdate in agent.RunStreamingAsync(message))
{
    foreach (var content in agentResponseUpdate.Contents)
    {
        if (content is TextReasoningContent textReasoningContent)
        {
            isThinking = true;
            Console.Write(textReasoningContent.Text);
            thinkingStringBuilder.Append(textReasoningContent.Text);
        }
        else if (content is TextContent textContent)
        {
            if (string.IsNullOrEmpty(textContent.Text))
            {
                continue;
            }

            if (isThinking)
            {
                Console.WriteLine();
                Console.WriteLine("---------");
            }

            isThinking = false;
            Console.Write(textContent.Text);
            contentStringBuilder.Append(textContent.Text);
        }
        else if (content is UsageContent usageContent)
        {
            Console.WriteLine();
            var usage = usageContent.Details;
            Console.WriteLine($"本次对话总Token消耗：{usage.TotalTokenCount};输入Token消耗：{usage.InputTokenCount};输出Token消耗：{usage.OutputTokenCount},其中思考占{usage.ReasoningTokenCount ?? 0}");
        }
    }
}

var thinkingText = thinkingStringBuilder.ToString();
var contentText = contentStringBuilder.ToString();

GC.KeepAlive(thinkingText);
GC.KeepAlive(contentText);

Console.WriteLine("Hello, World!");
Console.ReadLine();