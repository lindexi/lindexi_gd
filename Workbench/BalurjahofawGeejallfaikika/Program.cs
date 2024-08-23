using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

Task.Run(async () =>
{
    var foo = new Foo();
    var jsonContent = JsonContent.Create(foo);

    await jsonContent.LoadIntoBufferAsync();

    var httpClient = new HttpClient();
    await httpClient.PostAsync("http://localhost:5255", jsonContent);


});

var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

app.MapPost("/", async context =>
{
    await Task.CompletedTask;
    var headers = context.Request.Headers;
});

app.Run();

class Foo
{
    public int Value { set; get; }
}