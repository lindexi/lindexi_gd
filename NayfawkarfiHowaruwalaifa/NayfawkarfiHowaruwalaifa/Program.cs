namespace NayfawkarfiHowaruwalaifa;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var app = builder.Build();

        app.MapGet("{*x}", async () =>
        {
            var httpContext = app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext;
            await Task.Delay(TimeSpan.FromSeconds(1000));
            return "Hello World!";
        });

        app.Run();
    }
}
