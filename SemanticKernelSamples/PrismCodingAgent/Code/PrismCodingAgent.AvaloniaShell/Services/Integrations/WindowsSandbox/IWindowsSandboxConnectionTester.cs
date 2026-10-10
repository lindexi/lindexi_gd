using System.Threading;
using System.Threading.Tasks;

namespace CodingAgent.AvaloniaShell.Services.Integrations.WindowsSandbox;

internal interface IWindowsSandboxConnectionTester
{
    Task<WindowsSandboxConnectionTestResult> TestAsync
    (
        string toolPath,
        string serverAddress,
        CancellationToken cancellationToken = default
    );
}