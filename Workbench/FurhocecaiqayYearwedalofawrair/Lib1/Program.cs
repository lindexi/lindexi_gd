using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lib1;

public static class Program
{
    [UnmanagedCallersOnly(EntryPoint = "Start", CallConvs = [typeof(CallConvCdecl)])]
    public static int Start()
    {
        Console.WriteLine($"Start run");

        Task.Run(StartInner);

        return 2;
    }

    private static void StartInner()
    {
        var builder = WebApplication.CreateSlimBuilder([]);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        var app = builder.Build();

        var sampleTodos = new Todo[]
        {
            new(1, "Walk the dog"),
            new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
            new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
            new(4, "Clean the bathroom"),
            new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
        };

        var todosApi = app.MapGroup("/todos");
        todosApi.MapGet("/", () => sampleTodos);
        todosApi.MapGet("/{id}", (int id) =>
            sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
                ? Results.Ok(todo)
                : Results.NotFound());

        app.Run();
    }
}

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);