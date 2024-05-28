var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
IConfigurationSection section = builder.Configuration.GetSection("MyOptions");
MyOptions options1 = new();
section.Bind(options1);

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();

public class MyOptions
{
    public int A { get; set; }
    public string S { get; set; }
    public byte[] Data { get; set; }
    public Dictionary<string, string> Values { get; set; }
    public List<MyClass> Values2 { get; set; }
}

public class MyClass
{
    public int SomethingElse { get; set; }
}