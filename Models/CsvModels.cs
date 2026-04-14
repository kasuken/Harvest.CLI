using CsvHelper.Configuration.Attributes;

namespace Harvest.CLI.Models;

/// <summary>
/// Represents a single row in a Ruddr CSV time export.
/// </summary>
public sealed class CsvTimeEntry
{
    [Name("Date")]
    public DateTime Date { get; init; }

    [Name("Billable")]
    [BooleanTrueValues("Yes")]
    [BooleanFalseValues("No")]
    public bool IsBillable { get; init; }

    [Name("Hours")]
    public decimal Hours { get; init; }

    [Name("Notes")]
    public string Notes { get; init; } = string.Empty;

    [Name("Time Off Type")]
    public string TimeOffType { get; init; } = string.Empty;

    [Name("Project Time Type")]
    public string ProjectTimeType { get; init; } = string.Empty;
}

/// <summary>
/// A time block scheduled for submission to Harvest, produced by the day scheduler.
/// </summary>
public sealed class ScheduledEntry
{
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
    public required int ProjectId { get; init; }
    public required int TaskId { get; init; }
    public string Notes { get; init; } = string.Empty;
    public bool IsLunchBreak { get; init; }
    public bool IsBillable { get; init; }
}
