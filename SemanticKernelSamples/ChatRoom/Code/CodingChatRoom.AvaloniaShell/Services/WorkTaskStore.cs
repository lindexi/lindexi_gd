using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CodingChatRoom.AvaloniaShell.Infrastructure;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed record WorkTaskRecord
(
    Guid Id,
    string DisplayName,
    string? WorkspacePath,
    string? ModelReference,
    ReasoningEffort? ReasoningEffort,
    bool IsArchived = false
);

internal sealed class WorkTaskStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly CodingChatRoomPaths _paths;

    public WorkTaskStore(CodingChatRoomPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _paths = paths;
    }

    public async Task<IReadOnlyList<WorkTaskRecord>> LoadAsync(CancellationToken cancellationToken = default)
    {
        _paths.WorkTasksFile.Refresh();
        if (!_paths.WorkTasksFile.Exists)
        {
            return [];
        }

        await using FileStream stream = _paths.WorkTasksFile.OpenRead();
        return await JsonSerializer.DeserializeAsync<List<WorkTaskRecord>>(stream, s_jsonOptions, cancellationToken)
            .ConfigureAwait(false) ?? [];
    }

    public async Task SaveAsync
    (
        IReadOnlyList<WorkTaskRecord> tasks,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(tasks);
        _paths.EnsureDirectories();
        await using FileStream stream = new
        (
            _paths.WorkTasksFile.FullName,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.Asynchronous
        );
        await JsonSerializer.SerializeAsync(stream, tasks, s_jsonOptions, cancellationToken).ConfigureAwait(false);
    }
}