using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/", (FooRequest request) =>
{
    return "OK";
});

app.Run();

public class FooRequest
{
    public int Id { get; init; } 
    public required string Title { get; init; } 
}

[JsonSerializable(typeof(FooRequest[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
