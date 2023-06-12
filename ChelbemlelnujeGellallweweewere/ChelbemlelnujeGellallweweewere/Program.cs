namespace ChelbemlelnujeGellallweweewere;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();

        var app = builder.Build();
        var appConfigurator = app.Configuration.ToAppConfigurator();

        var fooConfiguration = appConfigurator.Of<FooConfiguration>();
        Console.WriteLine(fooConfiguration.Name);

        app.MapControllers();

        app.Run();
    }
}