using PptxGenerator;

namespace CoursewarePptxGeneratorWpfDemo.Threading;

/// <summary>
/// Verifies access to the WPF Dispatcher that owns ViewModel state.
/// </summary>
public sealed class WpfViewModelThreadAccess : IViewModelThreadAccess
{
    /// <summary>
    /// Gets the shared WPF ViewModel thread-access verifier.
    /// </summary>
    public static WpfViewModelThreadAccess Instance { get; } = new();

    private WpfViewModelThreadAccess()
    {
    }

    /// <inheritdoc />
    public bool CheckAccess() => WpfDispatcher.Instance.CheckAccess();
}
