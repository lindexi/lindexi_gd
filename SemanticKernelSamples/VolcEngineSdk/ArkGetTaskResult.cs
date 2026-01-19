namespace VolcEngineSdk;

public record ArkGetTaskResult()
{
    public required string TaskId { get; init; }

    public required string Model { get; init; }

    public required ArkTaskStatus Status { get; init; }

    public required ArkGeneratedVideoContent? Content { get; init; }
}