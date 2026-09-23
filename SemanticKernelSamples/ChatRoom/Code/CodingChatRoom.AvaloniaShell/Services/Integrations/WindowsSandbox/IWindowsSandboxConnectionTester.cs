using System.Threading;
using System.Threading.Tasks;

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface IWindowsSandboxConnectionTester
{
    Task<WindowsSandboxConnectionTestResult> TestAsync
    (
        string toolPath,
        string serverAddress,
        CancellationToken cancellationToken = default
    );
}