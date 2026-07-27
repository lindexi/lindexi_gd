using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace DeepSeekWpf.Services;

public sealed class WpfUserInteractionService : IUserInteractionService
{
    public Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task<string?> SelectSaveFileAsync(
        string title,
        string suggestedFileName,
        string filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new SaveFileDialog
        {
            Title = title,
            FileName = suggestedFileName,
            Filter = filter,
            AddExtension = true,
            DefaultExt = ".md",
            OverwritePrompt = true,
        };

        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FileName : null);
    }

    public Task ShowMessageAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    public Task CopyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Clipboard.SetText(text ?? string.Empty);
        return Task.CompletedTask;
    }

    public Task OpenPathAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            var extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            }
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
        return Task.CompletedTask;
    }
}
