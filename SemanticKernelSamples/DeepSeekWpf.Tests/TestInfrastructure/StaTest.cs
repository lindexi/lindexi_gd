using System.Runtime.ExceptionServices;

namespace DeepSeekWpf.Tests.TestInfrastructure;

internal static class StaTest
{
    public static Task RunAsync(Func<Task> action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return Task.CompletedTask;
    }
}