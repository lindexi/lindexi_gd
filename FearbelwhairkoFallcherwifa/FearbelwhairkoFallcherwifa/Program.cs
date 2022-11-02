using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

var readonlyCoinConfiguration = new ReadonlyCoinConfiguration();
builder.Configuration.AddConfiguration(new ConfigurationManager());

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

var keyValuePairs = app.Configuration.AsEnumerable().ToList();

app.Run();

class ReadonlyCoinConfiguration : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return null;
    }
}