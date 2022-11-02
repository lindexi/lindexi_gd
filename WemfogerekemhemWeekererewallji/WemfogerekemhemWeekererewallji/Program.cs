namespace WemfogerekemhemWeekererewallji;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddInMemoryCollection().Add(new ReadonlyCoinConfiguration());

        builder.Services.AddControllers();

        var app = builder.Build();

        var keyValuePairs = app.Configuration.AsEnumerable().ToList();

        app.MapControllers();

        app.Run();
    }
}