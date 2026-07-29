namespace CoursewarePptxGeneratorWpfDemo.Threading;

/// <summary>
/// Dispatches ViewModel state updates to the UI thread when required.
/// </summary>
public interface IViewModelDispatcher
{
    /// <summary>
    /// Executes a synchronous action on the ViewModel update thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A task that represents the dispatched operation.</returns>
    Task InvokeAsync(Action action);
}
