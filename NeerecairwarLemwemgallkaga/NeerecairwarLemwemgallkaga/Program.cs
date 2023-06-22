using NeerecairwarLemwemgallkaga;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddProvider(new LoggerProvider());
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Foo");
logger.LogInformation($"LogInfo");
logger.LogWarning($"LogWarning");

app.Run();
