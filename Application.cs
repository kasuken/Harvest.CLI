using System.Globalization;
using Harvest.CLI.Configuration;
using Harvest.CLI.Helpers;
using Harvest.CLI.Models;
using Harvest.CLI.Services;
using Spectre.Console;

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
        AnsiConsole.Write(new FigletText("Harvest CLI").Color(Color.Orange1));
        AnsiConsole.Write(new Rule("[grey]Time Tracking Made Easy[/]").RuleStyle("orange1"));
        AnsiConsole.WriteLine();

        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]How would you like to proceed?[/]")
                .HighlightStyle(new Style(Color.Orange1, decoration: Decoration.Bold))
                .AddChoices("Browse projects & tasks", "Import from CSV file (Ruddr export)", "Manual entry"));

        if (mode.StartsWith("Browse", StringComparison.Ordinal))
        {
            await DisplayProjectsAndTasksAsync(ct);
        }
        else if (mode.StartsWith("Import", StringComparison.Ordinal))
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

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[grey]Session complete[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine("[dim]Thank you for using [bold orange1]Harvest CLI[/]. Goodbye![/]");
    }

    /// <summary>
    /// Fetches and displays all assigned projects and their tasks with IDs.
    /// </summary>
    private async Task DisplayProjectsAndTasksAsync(CancellationToken ct)
    {
        List<ProjectAssignment> assignments = [];
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("orange1"))
            .StartAsync("Loading project assignments...", async _ =>
            {
                assignments = await apiClient.GetProjectAssignmentsAsync(ct);
            });

        if (assignments.Count == 0)
        {
            ConsoleHelper.DisplayWarning("No active project assignments found.");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]Found [orange1]{assignments.Count}[/] project(s).[/]");
        AnsiConsole.WriteLine();

        foreach (var assignment in assignments)
        {
            string clientInfo = assignment.Client is not null
                ? $" [dim]({assignment.Client.Name.EscapeMarkup()})[/]"
                : string.Empty;

            AnsiConsole.MarkupLine(
                $"[bold orange1]:file_folder: {assignment.Project.Name.EscapeMarkup()}[/]{clientInfo}  [dim]ID: {assignment.Project.Id}[/]");

            if (assignment.TaskAssignments.Count == 0)
            {
                AnsiConsole.MarkupLine("   [dim]No tasks assigned.[/]");
            }
            else
            {
                var table = new Table()
                    .Border(TableBorder.Simple)
                    .BorderColor(Color.Grey)
                    .AddColumn(new TableColumn("[bold]Task ID[/]").RightAligned())
                    .AddColumn(new TableColumn("[bold]Task Name[/]"));

                foreach (var task in assignment.TaskAssignments.OrderBy(t => t.Task.Name))
                {
                    table.AddRow(
                        $"[dim]{task.Task.Id}[/]",
                        task.Task.Name.EscapeMarkup());
                }

                AnsiConsole.Write(table);
            }

            AnsiConsole.WriteLine();
        }
    }

    private async Task ProcessManualEntryLoopAsync(CancellationToken ct)
    {
        bool continueTracking = true;
        while (continueTracking)
        {
            try
            {
                await ProcessSingleTimeEntryAsync(ct);

                continueTracking = ConsoleHelper.Confirm("Add another time entry?");

                if (continueTracking)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(new Rule("[orange1]New Time Entry[/]").RuleStyle("grey"));
                    AnsiConsole.WriteLine();
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.DisplayError($"Error: {ex.Message}");
                continueTracking = ConsoleHelper.Confirm("Try again?");
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
            AnsiConsole.MarkupLine("[dim]Time tracking cancelled.[/]");
            return;
        }

        string notes = PromptForNotes();

        string timeInfo = $"{startTime:HH\\:mm} - {endTime:HH\\:mm}";
        string fullNotes = string.IsNullOrEmpty(notes) ? timeInfo : $"{timeInfo} | {notes}";

        if (ConfirmTimeEntry(date, startTime, endTime, hours, projectName, taskName, fullNotes))
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots2)
                .SpinnerStyle(Style.Parse("orange1"))
                .StartAsync("Submitting time entry...", async _ =>
                    await SubmitTimeEntryAsync(projectId, taskId, date, startTime, endTime, notes, ct));

            ConsoleHelper.DisplaySuccess($"Time entry recorded for {date:yyyy-MM-dd} from {startTime:HH\\:mm} to {endTime:HH\\:mm}.");

            if (ConsoleHelper.Confirm("Fill up hours with the same values until Friday of the current week?"))
            {
                await FillUpHoursUntilFridayAsync(date, projectId, taskId, startTime, endTime, notes, ct);
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]Time tracking cancelled.[/]");
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

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots2)
                .SpinnerStyle(Style.Parse("orange1"))
                .StartAsync($"Submitting {current:yyyy-MM-dd}...", async _ =>
                    await SubmitTimeEntryAsync(projectId, taskId, current, startTime, endTime, notes, ct));

            ConsoleHelper.DisplaySuccess($"Time entry recorded for {current:yyyy-MM-dd} from {startTime:HH\\:mm} to {endTime:HH\\:mm}.");
        }
    }

    private async Task ProcessCsvImportAsync(CancellationToken ct)
    {
        if (importSettings.BillableProjectId == 0 || importSettings.BillableTaskId == 0 ||
            importSettings.NonBillableProjectId == 0 || importSettings.NonBillableTaskId == 0)
        {
            ConsoleHelper.DisplayError(
                "Import settings are incomplete. Configure all project/task IDs in appsettings.json under Harvest:Import.");
            return;
        }

        var csvPath = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the [orange1]path to the CSV file[/]:")
                .ValidationErrorMessage("[red]File not found[/]")
                .Validate(path =>
                {
                    var trimmed = path.Trim().Trim('"');
                    return !string.IsNullOrEmpty(trimmed) && File.Exists(trimmed)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]File not found. Please enter a valid path.[/]");
                })).Trim().Trim('"');

        var entries = csvService.ParseFile(csvPath);
        AnsiConsole.MarkupLine($"\n[bold]Parsed [orange1]{entries.Count}[/] time entries from CSV.[/]");

        var groupedByDate = entries.GroupBy(e => e.Date).OrderBy(g => g.Key);

        // Build the full schedule for all dates before submitting anything
        var allScheduled = new List<(DateTime Date, List<ScheduledEntry> Entries)>();

        foreach (var dateGroup in groupedByDate)
        {
            var dayEntries = dateGroup.ToList();
            var scheduledEntries = csvService.ScheduleDayEntries(dayEntries, importSettings);
            allScheduled.Add((dateGroup.Key, scheduledEntries.OrderBy(e => e.StartTime).ToList()));
        }

        // Pad to 42 hours per week with 30-min non-billable blocks
        csvService.PadWeeklyHours(allScheduled, importSettings);

        // Display preview of all scheduled entries (including padding)
        foreach (var (date, dayEntries) in allScheduled)
        {
            decimal dayHours = dayEntries.Sum(e => (decimal)(e.EndTime - e.StartTime).TotalHours);

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[bold]{date:yyyy-MM-dd}[/]  [dim]{dayEntries.Count} entries | {dayHours:0.##}h[/]").RuleStyle("orange1").LeftJustified());

            if (dayHours > 9)
            {
                ConsoleHelper.DisplayWarning(
                    $"{dayHours:0.##} hours exceeds 9-hour daily limit (8:00-18:00 minus 1h lunch).");
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[bold]Type[/]").Centered())
                .AddColumn(new TableColumn("[bold]Time[/]").Centered())
                .AddColumn(new TableColumn("[bold]Notes[/]"));

            foreach (var scheduled in dayEntries)
            {
                string label = scheduled.IsBillable
                    ? "[green]:dollar_banknote: Billable[/]"
                    : "[blue]:blue_circle: Non-Billable[/]";

                table.AddRow(
                    label,
                    $"{scheduled.StartTime:HH\\:mm}-{scheduled.EndTime:HH\\:mm}",
                    scheduled.Notes.EscapeMarkup());
            }

            AnsiConsole.Write(table);
        }

        int totalEntries = allScheduled.Sum(s => s.Entries.Count);
        decimal grandTotalHours = allScheduled.Sum(d => d.Entries.Sum(e => (decimal)(e.EndTime - e.StartTime).TotalHours));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Total: [orange1]{totalEntries}[/] time entries across [orange1]{allScheduled.Count}[/] days | [orange1]{grandTotalHours:0.##}h[/][/]");

        if (!ConsoleHelper.Confirm("Submit all entries to Harvest?"))
        {
            AnsiConsole.MarkupLine("[dim]Import cancelled.[/]");
            return;
        }

        // Submit all entries after confirmation
        foreach (var (date, orderedEntries) in allScheduled)
        {
            foreach (var scheduled in orderedEntries)
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots2)
                    .SpinnerStyle(Style.Parse("orange1"))
                    .StartAsync($"Submitting {date:yyyy-MM-dd} {scheduled.StartTime:HH\\:mm}-{scheduled.EndTime:HH\\:mm}...", async _ =>
                    {
                        await SubmitTimeEntryAsync(
                            scheduled.ProjectId, scheduled.TaskId, date,
                            scheduled.StartTime, scheduled.EndTime, scheduled.Notes, ct);
                    });

                ConsoleHelper.DisplaySuccess($"{date:yyyy-MM-dd} {scheduled.StartTime:HH\\:mm}-{scheduled.EndTime:HH\\:mm} submitted.");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel("[bold green]CSV import completed successfully![/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0));
    }

    private async Task<(int ProjectId, int TaskId, string ProjectName, string TaskName)> SelectProjectAndTaskAsync(
        CancellationToken ct)
    {
        List<ProjectAssignment> assignments = [];
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("orange1"))
            .StartAsync("Loading projects...", async _ =>
            {
                assignments = await apiClient.GetProjectAssignmentsAsync(ct);
            });

        if (assignments.Count == 0)
            throw new InvalidOperationException("No active project assignments found for your user.");

        var projectChoices = assignments
            .Select(a => $"{a.Project.Name} ({a.Client?.Name ?? "No client"})")
            .Prepend("Cancel")
            .ToList();

        var projectSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select a project:[/]")
                .PageSize(15)
                .HighlightStyle(new Style(Color.Orange1, decoration: Decoration.Bold))
                .AddChoices(projectChoices));

        if (projectSelection == "Cancel")
            return (0, 0, string.Empty, string.Empty);

        int projectIndex = projectChoices.IndexOf(projectSelection) - 1; // -1 for Cancel
        var selected = assignments[projectIndex];
        int projectId = selected.Project.Id;
        string projectName = selected.Project.Name;

        var tasks = selected.TaskAssignments
            .OrderBy(t => t.Task.Name)
            .ToList();

        if (tasks.Count == 0)
            throw new InvalidOperationException($"No active tasks found for project '{projectName}'.");

        var taskChoices = tasks
            .Select(t => t.Task.Name)
            .Prepend("Cancel")
            .ToList();

        var taskSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]Select a task for [orange1]{projectName.EscapeMarkup()}[/]:[/]")
                .PageSize(15)
                .HighlightStyle(new Style(Color.Orange1, decoration: Decoration.Bold))
                .AddChoices(taskChoices));

        if (taskSelection == "Cancel")
            return (0, 0, string.Empty, string.Empty);

        int taskIndex = taskChoices.IndexOf(taskSelection) - 1; // -1 for Cancel
        return (projectId, tasks[taskIndex].Task.Id, projectName, tasks[taskIndex].Task.Name);
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

        var input = AnsiConsole.Prompt(
            new TextPrompt<string>($"Date for time entry [dim](yyyy-MM-dd, Enter = {today:yyyy-MM-dd})[/]:")
                .AllowEmpty()
                .Validate(value =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return ValidationResult.Success();

                    return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Invalid format. Use yyyy-MM-dd.[/]");
                }));

        if (string.IsNullOrWhiteSpace(input))
            return today;

        return DateTime.ParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static (TimeOnly StartTime, TimeOnly EndTime, decimal Hours) PromptForTimeRange()
    {
        var startTime = PromptForTime("Start time [dim](HH:mm)[/]:");
        var endTime = PromptForTime("End time [dim](HH:mm)[/]:");

        decimal hours = CalculateHours(startTime, endTime);

        if (hours <= 0)
        {
            ConsoleHelper.DisplayError("End time must be after start time. Please try again.");
            return PromptForTimeRange();
        }

        AnsiConsole.MarkupLine($"[dim]Time range:[/] [bold]{startTime:HH\\:mm}[/] - [bold]{endTime:HH\\:mm}[/] [dim]({hours:0.##} hours)[/]");
        return (startTime, endTime, hours);
    }

    private static TimeOnly PromptForTime(string prompt)
    {
        var value = AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
                .Validate(v =>
                    TimeOnly.TryParseExact(v, "H:mm", out _) || TimeOnly.TryParseExact(v, "HH:mm", out _)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Invalid format. Use HH:mm (e.g. 09:00).[/]")));

        return TimeOnly.TryParseExact(value, "H:mm", out var t1) ? t1 : TimeOnly.ParseExact(value, "HH:mm");
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
        return AnsiConsole.Prompt(
            new TextPrompt<string>("Notes [dim](optional, Enter to skip)[/]:")
                .AllowEmpty());
    }

    private static bool ConfirmTimeEntry(
        DateTime date, TimeOnly startTime, TimeOnly endTime, decimal hours,
        string projectName, string taskName, string notes)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Orange1)
            .Title("[bold orange1]Time Entry Summary[/]")
            .AddColumn(new TableColumn("[bold]Field[/]"))
            .AddColumn(new TableColumn("[bold]Value[/]"));

        table.AddRow("[dim]Date[/]", $"[bold]{date:yyyy-MM-dd}[/]");
        table.AddRow("[dim]Time[/]", $"[bold]{startTime:HH\\:mm}[/] - [bold]{endTime:HH\\:mm}[/] ({hours:0.##}h)");
        table.AddRow("[dim]Project[/]", $"[bold orange1]{projectName.EscapeMarkup()}[/]");
        table.AddRow("[dim]Task[/]", taskName.EscapeMarkup());
        table.AddRow("[dim]Notes[/]", notes.EscapeMarkup());

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);

        return ConsoleHelper.Confirm("Submit this time entry?");
    }

    #endregion
}
