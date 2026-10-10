using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

internal sealed record WorkTaskRecoveryInfo
(
    int AssignedMissingIdCount,
    int ReassignedDuplicateIdCount,
    string BackupFilePath
);

internal sealed class WorkTaskStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly CodingChatRoomPaths _paths;

    public WorkTaskRecoveryInfo? LastRecoveryInfo { get; private set; }

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

        List<WorkTaskRecord> tasks;
        await using (FileStream stream = _paths.WorkTasksFile.OpenRead())
        {
            tasks = await JsonSerializer.DeserializeAsync<List<WorkTaskRecord>>(stream, s_jsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? [];
        }

        LastRecoveryInfo = null;
        var assignedIds = tasks
            .Where(task => task.Id != Guid.Empty)
            .Select(task => task.Id)
            .ToHashSet();
        var duplicateIndexes = tasks
            .Select((task, index) => (task, index))
            .Where(item => item.task.Id != Guid.Empty)
            .GroupBy(item => item.task.Id)
            .Where(group => group.Count() > 1)
            .SelectMany(group =>
            {
                int retainedIndex = group.FirstOrDefault(item => !item.task.IsArchived, group.First()).index;
                return group.Where(item => item.index != retainedIndex).Select(item => item.index);
            })
            .ToHashSet();
        int assignedMissingIdCount = 0;
        int reassignedDuplicateIdCount = 0;

        for (int index = 0; index < tasks.Count; index++)
        {
            WorkTaskRecord task = tasks[index];
            if (task.Id != Guid.Empty && !duplicateIndexes.Contains(index))
            {
                continue;
            }

            Guid id;
            do
            {
                id = Guid.NewGuid();
            }
            while (!assignedIds.Add(id));

            tasks[index] = task with { Id = id };
            if (task.Id == Guid.Empty)
            {
                assignedMissingIdCount++;
            }
            else
            {
                reassignedDuplicateIdCount++;
            }
        }

        if (assignedMissingIdCount > 0 || reassignedDuplicateIdCount > 0)
        {
            string backupFilePath = Path.Join
            (
                _paths.RootDirectory,
                $"WorkTasks.backup-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json"
            );
            File.Copy(_paths.WorkTasksFile.FullName, backupFilePath, overwrite: false);
            await SaveAsync(tasks, cancellationToken).ConfigureAwait(false);
            LastRecoveryInfo = new WorkTaskRecoveryInfo
            (
                assignedMissingIdCount,
                reassignedDuplicateIdCount,
                backupFilePath
            );
        }

        return tasks;
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