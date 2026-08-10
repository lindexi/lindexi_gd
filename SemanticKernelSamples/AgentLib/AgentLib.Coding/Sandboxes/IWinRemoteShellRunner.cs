namespace AgentLib.Coding.Sandboxes;

internal interface IWinRemoteShellRunner
{
    Task PushAsync(string sourcePath, string remoteTargetPath, CancellationToken cancellationToken);

    Task<string> ExecuteAsync(string command, int timeoutSeconds, CancellationToken cancellationToken);

    Task PullAsync(string remoteSourcePath, string localOutputPath, CancellationToken cancellationToken);
}