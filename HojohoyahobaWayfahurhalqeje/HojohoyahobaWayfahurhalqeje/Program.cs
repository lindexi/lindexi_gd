namespace HojohoyahobaWayfahurhalqeje;

internal class Program
{
    static async Task Main(string[] args)
    {
        var task = Task.Run(Foo).ContinueWith(t =>
        {

        }, TaskContinuationOptions.OnlyOnFaulted);

        try
        {
            await task;
        }
        catch (TaskCanceledException e)
        {

        }

        var task1 = Task.Run(FooWithException).ContinueWith(t =>
        {

        }, TaskContinuationOptions.OnlyOnFaulted);

        await task1;
    }

    static void Foo()
    {

    }

    static void FooWithException()
    {
        throw new Exception("lindexi is doubi");
    }
}
