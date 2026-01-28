namespace QurhurchairduryuCaqolojearqelkall;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSimpleConsole());

        var app = builder.Build();

        app.MapPost("/", async (HttpContext context) =>
        {
            var file = Path.Join(AppContext.BaseDirectory, Path.GetRandomFileName());

            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"收到文件: {context.Request.ContentLength} bytes. 存放到 {file}");

            using var fileStream = File.Create(file);
            await context.Request.Body.CopyToAsync(fileStream);
        });

        app.Urls.Add("http://0.0.0.0:12779");

        app.Run();
    }
}
