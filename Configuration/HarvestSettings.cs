namespace Harvest.CLI.Configuration;

/// <summary>
/// Strongly-typed Harvest API configuration bound from appsettings.json.
/// </summary>
public sealed record HarvestSettings
{
    public string AccountId { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public ImportSettings Import { get; init; } = new();
}

/// <summary>
/// Project and task ID mappings used during CSV import.
/// </summary>
public sealed record ImportSettings
{
    public int BillableProjectId { get; init; }
    public int BillableTaskId { get; init; }
    public int NonBillableProjectId { get; init; }
    public int NonBillableTaskId { get; init; }
    public int LunchBreakProjectId { get; init; }
    public int LunchBreakTaskId { get; init; }
    public int HolidayProjectId { get; init; }
    public int HolidayTaskId { get; init; }
}
