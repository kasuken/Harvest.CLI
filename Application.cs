using System.Globalization;
using Harvest.CLI.Configuration;
using Harvest.CLI.Helpers;
using Harvest.CLI.Models;
using Harvest.CLI.Services;

namespace Harvest.CLI;

/// <summary>
/// Main application workflow for the Harvest CLI.
/// </summary>
public sealed class Application(
    HarvestApiClient apiClient,
    CsvImportService csvService,
    ImportSettings importSettings)
{
    /// <summary>
    /// Runs the main application loop.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine("Welcome to Harvest CLI!");
        Console.WriteLine("============================");

        Console.WriteLine("\nHow would you like to proceed?");
        Console.WriteLine("1. Import from CSV file (Ruddr export)");
        Console.WriteLine("2. Manual entry");
        Console.Write("\nSelect an option (1 or 2): ");
        string? modeChoice = Console.ReadLine()?.Trim();

        if (modeChoice == "1")
        {
            try
            {
                await ProcessCsvImportAsync(ct);
            }
            catch (Exception ex)
            {
                ConsoleHelper.DisplayError($"Import error: {ex.Message}");
            }
        }
        else
        {
            await ProcessManualEntryLoopAsync(ct);
        }

        Console.WriteLine("\nThank you for using Harvest CLI. Goodbye!");
    }

    private async Task ProcessManualEntryLoopAsync(CancellationToken ct)
    {
        bool continueTracking = true;
        while (continueTracking)
        {
            try
            {
                await ProcessSingleTimeEntryAsync(ct);

                continueTracking = ConsoleHelper.Confirm("\nDo you want to add another time entry? (y/n): ");

                if (continueTracking)
                {
                    Console.WriteLine("\n----------------------------------------------------");
                    Console.WriteLine("Starting a new time entry...");
                    Console.WriteLine("----------------------------------------------------\n");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.DisplayError($"Error: {ex.Message}");
                continueTracking = ConsoleHelper.Confirm("\nDo you want to try again? (y/n): ");
            }
        }
    }

    private async Task ProcessSingleTimeEntryAsync(CancellationToken ct)
    {
        DateTime date = PromptForDate();
        var (startTime, endTime, hours) = PromptForTimeRange();
        var (projectId, taskId, projectName, taskName) = await SelectProjectAndTaskAsync(ct);

        if (projectId == 0 || taskId == 0)
        {
            Console.WriteLine("Time tracking cancelled.");
            return;
        }

        string notes = PromptForNotes();

        string timeInfo = $"{startTime:HH\\:mm} - {endTime:HH\\:mm}";
        string fullNotes = string.IsNullOrEmpty(notes) ? timeInfo : $"{timeInfo} | {notes}";

        if (ConfirmTimeEntry(date, startTime, endTime, hours, projectName, taskName, fullNotes))
        {
            await SubmitTimeEntryAsync(projectId, taskId, date, startTime, endTime, notes, ct);
            Console.WriteLine($"\nSuccess! Time entry recorded for {date:yyyy-MM-dd} from {startTime:HH\\:mm} to {endTime:HH\\:mm}.");

            if (ConsoleHelper.Confirm("\nDo you want to fill up the hours with the same values until Friday of the current week? (y/n): "))
            {
                await FillUpHoursUntilFridayAsync(date, projectId, taskId, startTime, endTime, notes, ct);
            }
        }
        else
        {
            Console.WriteLine("Time tracking cancelled.");
        }
    }

    private async Task FillUpHoursUntilFridayAsync(
        DateTime startDate, int projectId, int taskId,
        TimeOnly startTime, TimeOnly endTime, string notes,
        CancellationToken ct)
    {
        DateTime current = startDate;
        while (current.DayOfWeek != DayOfWeek.Friday)
        {
            current = current.AddDays(1);
            if (current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            await SubmitTimeEntryAsync(projectId, taskId, current, startTime, endTime, notes, ct);
            Console.WriteLine($"\nSuccess! Time entry recorded for {current:yyyy-MM-dd} from {startTime:HH\\:mm} to {endTime:HH\\:mm}.");
        }
    }

    private async Task ProcessCsvImportAsync(CancellationToken ct)
    {
        if (importSettings.BillableProjectId == 0 || importSettings.BillableTaskId == 0 ||
            importSettings.NonBillableProjectId == 0 || importSettings.NonBillableTaskId == 0 ||
            importSettings.LunchBreakProjectId == 0 || importSettings.LunchBreakTaskId == 0)
        {
            ConsoleHelper.DisplayError(
                "Import settings are incomplete. Configure all project/task IDs in appsettings.json under Harvest:Import.");
            return;
        }

        Console.WriteLine("\nEnter the path to the CSV file:");
        string? csvPath = Console.ReadLine()?.Trim().Trim('"');

        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            ConsoleHelper.DisplayError("File not found. Import cancelled.");
            return;
        }

        var entries = csvService.ParseFile(csvPath);
        Console.WriteLine($"\nParsed {entries.Count} time entries from CSV.");

        var groupedByDate = entries.GroupBy(e => e.Date).OrderBy(g => g.Key);

        foreach (var dateGroup in groupedByDate)
        {
            var date = dateGroup.Key;
            var dayEntries = dateGroup.ToList();
            decimal totalHours = dayEntries.Sum(e => e.Hours);

            Console.WriteLine($"\n--- {date:yyyy-MM-dd} | {dayEntries.Count} entries | {totalHours} total hours ---");

            if (totalHours > 9)
            {
                ConsoleHelper.DisplayWarning(
                    $"  Warning: {totalHours} hours exceeds 9-hour daily limit (8:00-18:00 minus 1h lunch).");
            }

            var scheduledEntries = csvService.ScheduleDayEntries(dayEntries, importSettings);

            foreach (var scheduled in scheduledEntries.OrderBy(e => e.StartTime))
            {
                string label = scheduled.IsLunchBreak
                    ? "[Lunch]"
                    : (scheduled.IsBillable ? "[Billable]" : "[Non-Billable]");

                Console.WriteLine($"  {label} {scheduled.StartTime:HH\\:mm}-{scheduled.EndTime:HH\\:mm} | {scheduled.Notes}");

                await SubmitTimeEntryAsync(
                    scheduled.ProjectId, scheduled.TaskId, date,
                    scheduled.StartTime, scheduled.EndTime, scheduled.Notes, ct);

                Console.WriteLine("    -> Submitted successfully.");
            }
        }

        Console.WriteLine("\nCSV import completed!");
    }

    private async Task<(int ProjectId, int TaskId, string ProjectName, string TaskName)> SelectProjectAndTaskAsync(
        CancellationToken ct)
    {
        var assignments = await apiClient.GetProjectAssignmentsAsync(ct);

        if (assignments.Count == 0)
            throw new InvalidOperationException("No active project assignments found for your user.");

        Console.WriteLine("\nAvailable Projects:");
        Console.WriteLine("------------------");
        for (int i = 0; i < assignments.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {assignments[i].Project.Name} ({assignments[i].Client?.Name})");
        }

        int projectId;
        string projectName;

        while (true)
        {
            Console.Write("\nEnter project number (or 0 to cancel): ");
            if (int.TryParse(Console.ReadLine(), out int selection))
            {
                if (selection == 0)
                    return (0, 0, string.Empty, string.Empty);

                if (selection > 0 && selection <= assignments.Count)
                {
                    var selected = assignments[selection - 1];
                    projectId = selected.Project.Id;
                    projectName = selected.Project.Name;
                    break;
                }
            }

            ConsoleHelper.DisplayError("Invalid selection. Please try again.");
        }

        var tasks = assignments
            .First(pa => pa.Project.Id == projectId)
            .TaskAssignments
            .OrderBy(t => t.Task.Name)
            .ToList();

        if (tasks.Count == 0)
            throw new InvalidOperationException($"No active tasks found for project '{projectName}'.");

        Console.WriteLine("\nAvailable Tasks:");
        Console.WriteLine("---------------");
        for (int i = 0; i < tasks.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {tasks[i].Task.Name}");
        }

        while (true)
        {
            Console.Write("\nEnter task number (or 0 to cancel): ");
            if (int.TryParse(Console.ReadLine(), out int taskSelection))
            {
                if (taskSelection == 0)
                    return (0, 0, string.Empty, string.Empty);

                if (taskSelection > 0 && taskSelection <= tasks.Count)
                {
                    return (projectId, tasks[taskSelection - 1].Task.Id,
                        projectName, tasks[taskSelection - 1].Task.Name);
                }
            }

            ConsoleHelper.DisplayError("Invalid selection. Please try again.");
        }
    }

    private async Task SubmitTimeEntryAsync(
        int projectId, int taskId, DateTime date,
        TimeOnly startTime, TimeOnly endTime, string notes,
        CancellationToken ct)
    {
        var request = new TimeEntryRequest
        {
            ProjectId = projectId,
            TaskId = taskId,
            SpentDate = date.ToString("yyyy-MM-dd"),
            StartedTime = startTime.ToString("HH:mm"),
            EndedTime = endTime.ToString("HH:mm"),
            Notes = notes
        };

        await apiClient.SubmitTimeEntryAsync(request, ct);
    }

    #region Console Prompts

    private static DateTime PromptForDate()
    {
        DateTime today = DateTime.Today;

        Console.WriteLine($"\nEnter date for time entry (format: yyyy-MM-dd, press Enter for today {today:yyyy-MM-dd}):");
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            return today;

        if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            return date;

        Console.WriteLine($"Invalid date format. Using today's date ({today:yyyy-MM-dd}).");
        return today;
    }

    private static (TimeOnly StartTime, TimeOnly EndTime, decimal Hours) PromptForTimeRange()
    {
        TimeOnly startTime;
        while (true)
        {
            Console.WriteLine("\nEnter start time (format: HH:mm):");
            string? startInput = Console.ReadLine();

            if (TimeOnly.TryParseExact(startInput, "H:mm", out startTime) ||
                TimeOnly.TryParseExact(startInput, "HH:mm", out startTime))
                break;

            ConsoleHelper.DisplayError("Invalid time format. Please use HH:mm format (e.g. 09:00).");
        }

        TimeOnly endTime;
        while (true)
        {
            Console.WriteLine("\nEnter end time (format: HH:mm):");
            string? endInput = Console.ReadLine();

            if (TimeOnly.TryParseExact(endInput, "H:mm", out endTime) ||
                TimeOnly.TryParseExact(endInput, "HH:mm", out endTime))
                break;

            ConsoleHelper.DisplayError("Invalid time format. Please use HH:mm format (e.g. 17:30).");
        }

        decimal hours = CalculateHours(startTime, endTime);

        if (hours <= 0)
        {
            ConsoleHelper.DisplayError("End time must be after start time. Please try again.");
            return PromptForTimeRange();
        }

        Console.WriteLine($"\nTime range: {startTime:HH\\:mm} - {endTime:HH\\:mm} ({hours:0.##} hours)");
        return (startTime, endTime, hours);
    }

    private static decimal CalculateHours(TimeOnly startTime, TimeOnly endTime)
    {
        TimeSpan duration = endTime < startTime
            ? endTime.AddHours(24) - startTime
            : endTime - startTime;

        return (decimal)duration.TotalHours;
    }

    private static string PromptForNotes()
    {
        Console.WriteLine("\nEnter notes (optional, press Enter to skip):");
        return Console.ReadLine() ?? string.Empty;
    }

    private static bool ConfirmTimeEntry(
        DateTime date, TimeOnly startTime, TimeOnly endTime, decimal hours,
        string projectName, string taskName, string notes)
    {
        Console.WriteLine("\n=== Time Entry Summary ===");
        Console.WriteLine($"Date: {date:yyyy-MM-dd}");
        Console.WriteLine($"Time: {startTime:HH\\:mm} - {endTime:HH\\:mm} ({hours:0.##} hours)");
        Console.WriteLine($"Project: {projectName}");
        Console.WriteLine($"Task: {taskName}");
        Console.WriteLine($"Notes: {notes}");
        Console.WriteLine("========================");

        return ConsoleHelper.Confirm("\nSubmit this time entry? (y/n): ");
    }

    #endregion
}
