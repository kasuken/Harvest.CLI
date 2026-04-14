namespace Harvest.CLI.Helpers;

/// <summary>
/// Utility methods for formatted console output.
/// </summary>
internal static class ConsoleHelper
{
    /// <summary>
    /// Displays a message in red.
    /// </summary>
    public static void DisplayError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Displays a message in yellow.
    /// </summary>
    public static void DisplayWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Prompts the user for a yes/no confirmation.
    /// </summary>
    public static bool Confirm(string prompt)
    {
        Console.Write(prompt);
        string? response = Console.ReadLine()?.Trim().ToLowerInvariant();
        return response is "y" or "yes";
    }
}
