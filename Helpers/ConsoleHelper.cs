using Spectre.Console;

namespace Harvest.CLI.Helpers;

/// <summary>
/// Utility methods for formatted console output using Spectre.Console.
/// </summary>
internal static class ConsoleHelper
{
    /// <summary>
    /// Displays an error message in a red panel.
    /// </summary>
    public static void DisplayError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red]:cross_mark: {message.EscapeMarkup()}[/]");
    }

    /// <summary>
    /// Displays a warning message in yellow.
    /// </summary>
    public static void DisplayWarning(string message)
    {
        AnsiConsole.MarkupLine($"[bold yellow]:warning: {message.EscapeMarkup()}[/]");
    }

    /// <summary>
    /// Displays a success message in green.
    /// </summary>
    public static void DisplaySuccess(string message)
    {
        AnsiConsole.MarkupLine($"[bold green]:check_mark_button: {message.EscapeMarkup()}[/]");
    }

    /// <summary>
    /// Prompts the user for a yes/no confirmation.
    /// </summary>
    public static bool Confirm(string prompt)
    {
        return AnsiConsole.Confirm(prompt);
    }
}
