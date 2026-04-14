using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Harvest.CLI.Configuration;
using Harvest.CLI.Models;

namespace Harvest.CLI.Services;

/// <summary>
/// Handles CSV file parsing and day-entry scheduling for Ruddr exports.
/// </summary>
public sealed class CsvImportService
{
    /// <summary>
    /// Parses a Ruddr CSV export file into time entries.
    /// </summary>
    public List<CsvTimeEntry> ParseFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("File path is required.", nameof(path));

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Trim(),
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<CsvTimeEntry>().ToList();
    }

    /// <summary>
    /// Schedules a day's CSV entries into time blocks starting at 08:00, inserting a lunch break at 12:00-13:00.
    /// </summary>
    public List<ScheduledEntry> ScheduleDayEntries(List<CsvTimeEntry> dayEntries, ImportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(dayEntries);
        ArgumentNullException.ThrowIfNull(settings);

        var scheduled = new List<ScheduledEntry>();
        var currentTime = new TimeOnly(8, 0);
        var lunchStart = new TimeOnly(12, 0);
        var lunchEnd = new TimeOnly(13, 0);
        bool lunchAdded = false;

        foreach (var entry in dayEntries)
        {
            var (projectId, taskId) = ResolveProjectAndTask(entry, settings);
            decimal remainingHours = entry.Hours;

            while (remainingHours > 0)
            {
                if (currentTime >= lunchStart && currentTime < lunchEnd)
                {
                    if (!lunchAdded)
                    {
                        scheduled.Add(new ScheduledEntry
                        {
                            StartTime = lunchStart,
                            EndTime = lunchEnd,
                            ProjectId = settings.LunchBreakProjectId,
                            TaskId = settings.LunchBreakTaskId,
                            Notes = "Lunch break",
                            IsLunchBreak = true
                        });
                        lunchAdded = true;
                    }
                    currentTime = lunchEnd;
                }

                decimal blockHours = currentTime < lunchStart
                    ? Math.Min(remainingHours, (decimal)(lunchStart - currentTime).TotalHours)
                    : remainingHours;

                if (blockHours <= 0) break;

                var endTime = currentTime.AddHours((double)blockHours);

                scheduled.Add(new ScheduledEntry
                {
                    StartTime = currentTime,
                    EndTime = endTime,
                    ProjectId = projectId,
                    TaskId = taskId,
                    Notes = entry.Notes,
                    IsBillable = entry.IsBillable
                });

                currentTime = endTime;
                remainingHours -= blockHours;
            }
        }

        return scheduled;
    }

    private static (int ProjectId, int TaskId) ResolveProjectAndTask(CsvTimeEntry entry, ImportSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(entry.TimeOffType))
            return (settings.HolidayProjectId, settings.HolidayTaskId);

        return entry.IsBillable
            ? (settings.BillableProjectId, settings.BillableTaskId)
            : (settings.NonBillableProjectId, settings.NonBillableTaskId);
    }
}
