namespace CoursewarePptxGeneratorWpfDemo.Threading;

/// <summary>
/// Verifies access to the thread that owns ViewModel observable state.
/// </summary>
public interface IViewModelThreadAccess
{
    /// <summary>
    /// Determines whether the caller currently owns ViewModel observable state.
    /// </summary>
    /// <returns><see langword="true" /> when the caller is on the owning thread.</returns>
    bool CheckAccess();
}
