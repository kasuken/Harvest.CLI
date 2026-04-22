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

        foreach (var entry in dayEntries)
        {
            var (projectId, taskId) = ResolveProjectAndTask(entry, settings);
            decimal remainingHours = entry.Hours;

            while (remainingHours > 0)
            {
                // Skip the lunch hour — no entry created, just advance past it
                if (currentTime >= lunchStart && currentTime < lunchEnd)
                {
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

    /// <summary>
    /// Pads weekly hours to the target (default 42) by adding 30-minute non-billable blocks per day until the gap is filled.
    /// </summary>
    public void PadWeeklyHours(
        List<(DateTime Date, List<ScheduledEntry> Entries)> allScheduled,
        ImportSettings settings,
        decimal targetWeeklyHours = 42m)
    {
        ArgumentNullException.ThrowIfNull(allScheduled);
        ArgumentNullException.ThrowIfNull(settings);

        // Group scheduled days by ISO week
        var weeks = allScheduled
            .GroupBy(s => ISOWeek.GetWeekOfYear(s.Date))
            .ToList();

        foreach (var week in weeks)
        {
            decimal weekTotal = week.Sum(d => d.Entries.Sum(e => (decimal)(e.EndTime - e.StartTime).TotalHours));
            decimal gap = targetWeeklyHours - weekTotal;

            if (gap <= 0) continue;

            var weekDays = week.OrderBy(d => d.Date).ToList();
            int dayIndex = 0;
            var maxEndOfDay = new TimeOnly(18, 0); // 9-hour workday: 08:00-18:00 minus 1h lunch
            int stuckCount = 0;

            while (gap > 0 && stuckCount < weekDays.Count)
            {
                var (date, entries) = weekDays[dayIndex];

                // Find the last end time for this day
                var lastEnd = entries.Count > 0
                    ? entries.Max(e => e.EndTime)
                    : new TimeOnly(8, 0);

                // Skip lunch if we land in it
                if (lastEnd >= new TimeOnly(12, 0) && lastEnd < new TimeOnly(13, 0))
                    lastEnd = new TimeOnly(13, 0);

                // Skip this day if it's already full (at or past max end of day)
                if (lastEnd >= maxEndOfDay)
                {
                    dayIndex = (dayIndex + 1) % weekDays.Count;
                    stuckCount++;
                    continue;
                }

                stuckCount = 0;

                // Cap block so it doesn't extend past end of day
                decimal availableMinutes = (decimal)(maxEndOfDay - lastEnd).TotalMinutes;
                decimal blockMinutes = Math.Min(30, Math.Min(gap * 60, availableMinutes));

                if (blockMinutes <= 0)
                {
                    dayIndex = (dayIndex + 1) % weekDays.Count;
                    continue;
                }

                var blockEnd = lastEnd.AddMinutes((double)blockMinutes);

                entries.Add(new ScheduledEntry
                {
                    StartTime = lastEnd,
                    EndTime = blockEnd,
                    ProjectId = settings.NonBillableProjectId,
                    TaskId = settings.NonBillableTaskId,
                    Notes = string.Empty,
                    IsBillable = false
                });

                gap -= blockMinutes / 60m;
                dayIndex = (dayIndex + 1) % weekDays.Count;
            }
        }
    }
}
