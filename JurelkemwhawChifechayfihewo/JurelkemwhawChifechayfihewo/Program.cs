var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddDebug());

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

var logLevelConfiguration = app.Configuration.GetSection("Logging").GetSection("LogLevel");
var configuration1 = logLevelConfiguration["Microsoft.AspNetCore"];
var configuration2 = logLevelConfiguration["Microsoft"];

// 配置文件 1：
// configuration1 = Warning
// configuration2 = null

// 配置文件 2：
// configuration1 = Debug
// configuration2 = Warning

var serviceProvider = app.Services;

var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var logger1 = loggerFactory.CreateLogger("Microsoft.AspNetCore.Foo");
logger1.LogInformation($"Logger1 LogInfo");
logger1.LogWarning($"Logger1 LogWarning");

var logger2 = loggerFactory.CreateLogger("Microsoft.Foo");
logger2.LogInformation($"Logger2 LogInfo");
logger2.LogWarning($"Logger2 LogWarning");

//app.Run();

// 控制台输出:

// 配置文件 1：
// Microsoft.AspNetCore.Foo: Warning: Logger1 LogWarning
// Microsoft.Foo: Information: Logger2 LogInfo
// Microsoft.Foo: Warning: Logger2 LogWarning

// 配置文件 2：
// Microsoft.AspNetCore.Foo: Information: Logger1 LogInfo
// Microsoft.AspNetCore.Foo: Warning: Logger1 LogWarning
// Microsoft.Foo: Warning: Logger2 LogWarning

Console.Read();
