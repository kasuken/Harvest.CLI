using System.Text.Json.Serialization;

namespace Harvest.CLI.Models;

public sealed class HarvestProject
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class HarvestClient
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// A task within a Harvest project. Named HarvestTask to avoid conflict with System.Threading.Tasks.Task.
/// </summary>
public sealed class HarvestTask
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class TaskAssignment
{
    public int Id { get; init; }
    public HarvestTask Task { get; init; } = new();
}

public sealed class ProjectAssignment
{
    public HarvestProject Project { get; init; } = new();
    public HarvestClient? Client { get; init; }

    [JsonPropertyName("task_assignments")]
    public List<TaskAssignment> TaskAssignments { get; init; } = [];
}

public sealed class PaginationLinks
{
    public string? First { get; init; }
    public string? Next { get; init; }
    public string? Previous { get; init; }
    public string? Last { get; init; }
}

public sealed class ProjectAssignmentsResponse
{
    [JsonPropertyName("project_assignments")]
    public List<ProjectAssignment> ProjectAssignments { get; init; } = [];
    public PaginationLinks? Links { get; init; }
}

/// <summary>
/// Request payload for creating a Harvest time entry with timestamps.
/// </summary>
public sealed class TimeEntryRequest
{
    [JsonPropertyName("project_id")]
    public required int ProjectId { get; init; }

    [JsonPropertyName("task_id")]
    public required int TaskId { get; init; }

    [JsonPropertyName("spent_date")]
    public required string SpentDate { get; init; }

    [JsonPropertyName("started_time")]
    public required string StartedTime { get; init; }

    [JsonPropertyName("ended_time")]
    public required string EndedTime { get; init; }

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
