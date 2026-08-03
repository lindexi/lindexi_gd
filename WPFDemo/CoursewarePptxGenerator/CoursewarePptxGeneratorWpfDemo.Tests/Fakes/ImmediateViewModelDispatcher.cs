using CoursewarePptxGeneratorWpfDemo.Threading;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class ImmediateViewModelThreadAccess : IViewModelThreadAccess
{
    public bool CheckAccess() => true;
}
