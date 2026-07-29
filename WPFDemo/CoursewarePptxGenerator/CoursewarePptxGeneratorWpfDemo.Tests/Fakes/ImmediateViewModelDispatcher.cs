using CoursewarePptxGeneratorWpfDemo.Threading;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class ImmediateViewModelDispatcher : IViewModelDispatcher
{
    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action();
        return Task.CompletedTask;
    }
}
