namespace DeepSeekWpf.Services;

public interface IUserInteractionService
{
    Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken = default);

    Task<string?> SelectSaveFileAsync(
        string title,
        string suggestedFileName,
        string filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> SelectFilesAsync(
        string title,
        string filter,
        bool allowMultiple,
        CancellationToken cancellationToken = default);

    Task ShowMessageAsync(string title, string message, CancellationToken cancellationToken = default);

    Task CopyTextAsync(string text, CancellationToken cancellationToken = default);

    Task OpenPathAsync(string path, CancellationToken cancellationToken = default);
}
