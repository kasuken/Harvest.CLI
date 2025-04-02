using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace Harvest.CLI
{
    public class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static IConfiguration _configuration;

        public static void Main(string[] args)
        {
            // Initialize configuration
            InitializeConfiguration();

            // Initialize HTTP client
            SetupHttpClient();

            Console.WriteLine("Welcome to Harvest CLI!");
            Console.WriteLine("============================");

            bool continueTracking = true;
            while (continueTracking)
            {
                try
                {
                    ProcessTimeEntryAsync();

                    // Ask if the user wants to add another time entry
                    Console.WriteLine("\nDo you want to add another time entry? (y/n): ");
                    string? response = Console.ReadLine()?.Trim().ToLower();
                    continueTracking = (response == "y" || response == "yes");

                    if (continueTracking)
                    {
                        Console.WriteLine("\n----------------------------------------------------");
                        Console.WriteLine("Starting a new time entry...");
                        Console.WriteLine("----------------------------------------------------\n");
                    }
                }
                catch (Exception ex)
                {
                    DisplayError($"Error: {ex.Message}");

                    // Ask if the user wants to try again after an error
                    Console.WriteLine("\nDo you want to try again? (y/n): ");
                    string? response = Console.ReadLine()?.Trim().ToLower();
                    continueTracking = (response == "y" || response == "yes");
                }
            }

            Console.WriteLine("\nThank you for using Harvest CLI. Goodbye!");
        }

        private static void ProcessTimeEntryAsync()
        {
            // Get date for time entry
            DateTime date = GetDateAsync();

            // Get time range
            (TimeOnly startTime, TimeOnly endTime, decimal hours) = GetTimeRangeAsync();

            // Select project and task
            var (projectId, taskId, projectName, taskName) = SelectProjectAndTaskAsync();

            if (projectId == 0 || taskId == 0)
            {
                Console.WriteLine("Time tracking cancelled.");
                return;
            }

            // Get notes
            string notes = GetNotesAsync();

            // Format notes to include time information
            string timeInfo = $"{startTime:HH\\:mm} - {endTime:HH\\:mm}";
            string fullNotes = string.IsNullOrEmpty(notes)
                ? timeInfo
                : $"{timeInfo} | {notes}";

            // Confirm before submission
            if (ConfirmTimeEntryAsync(date, startTime, endTime, hours, projectName, taskName, fullNotes))
            {
                // Submit time entry with timestamps
                TrackTimeWithTimestampsAsync(projectId, taskId, date, startTime, endTime, notes);
                Console.WriteLine($"\nSuccess! Time entry recorded for {date:yyyy-MM-dd} from {startTime:HH\\:mm} to {endTime:HH\\:mm}.");

                // Ask if the user wants to fill up the hours until Friday
                Console.WriteLine("\nDo you want to fill up the hours with the same values until Friday of the current week? (y/n): ");
                string? fillResponse = Console.ReadLine()?.Trim().ToLower();
                if (fillResponse == "y" || fillResponse == "yes")
                {
                    FillUpHoursUntilFriday(date, projectId, taskId, startTime, endTime, notes);
                }
            }
            else
            {
                Console.WriteLine("Time tracking cancelled.");
            }
        }

        private static void FillUpHoursUntilFriday(DateTime startDate, int projectId, int taskId, TimeOnly startTime, TimeOnly endTime, string notes)
        {
            DateTime current = startDate;
            while (current.DayOfWeek != DayOfWeek.Friday)
            {
                current = current.AddDays(1);
                if (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                TrackTimeWithTimestampsAsync(projectId, taskId, current, startTime, endTime, notes);
                Console.WriteLine($"\nSuccess! Time entry recorded for {current:yyyy-MM-dd} from {startTime:HH\\:mm} to {endTime:HH\\:mm}.");
            }
        }

        private static void InitializeConfiguration()
        {
            string environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .Build();
        }

        private static void SetupHttpClient()
        {
            var harvestAccountId = _configuration["Harvest:AccountId"]
                ?? Environment.GetEnvironmentVariable("HARVEST_ACCOUNT_ID")
                ?? throw new InvalidOperationException("Harvest Account ID not found in configuration or environment variables");

            var harvestAccessToken = _configuration["Harvest:AccessToken"]
                ?? Environment.GetEnvironmentVariable("HARVEST_ACCESS_TOKEN")
                ?? throw new InvalidOperationException("Harvest Access Token not found in configuration or environment variables");

            _httpClient.BaseAddress = new Uri("https://api.harvestapp.com/v2/");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Harvest-Account-Id", harvestAccountId);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", harvestAccessToken);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Harvest.CLI");
        }

        private static DateTime GetDateAsync()
        {
            DateTime today = DateTime.Today;

            Console.WriteLine($"\nEnter date for time entry (format: yyyy-MM-dd, press Enter for today {today:yyyy-MM-dd}):");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return today;
            }

            if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return date;
            }
            else
            {
                Console.WriteLine($"Invalid date format. Using today's date ({today:yyyy-MM-dd}).");
                return today;
            }
        }

        private static (TimeOnly startTime, TimeOnly endTime, decimal hours) GetTimeRangeAsync()
        {
            TimeOnly startTime;
            while (true)
            {
                Console.WriteLine("\nEnter start time (format: HH:mm):");
                string? startInput = Console.ReadLine();

                if (TimeOnly.TryParseExact(startInput, "H:mm", out startTime) ||
                    TimeOnly.TryParseExact(startInput, "HH:mm", out startTime))
                {
                    break;
                }

                DisplayError("Invalid time format. Please use HH:mm format (e.g. 09:00).");
            }

            TimeOnly endTime;
            while (true)
            {
                Console.WriteLine("\nEnter end time (format: HH:mm):");
                string? endInput = Console.ReadLine();

                if (TimeOnly.TryParseExact(endInput, "H:mm", out endTime) ||
                    TimeOnly.TryParseExact(endInput, "HH:mm", out endTime))
                {
                    break;
                }

                DisplayError("Invalid time format. Please use HH:mm format (e.g. 17:30).");
            }

            // Calculate hours
            decimal hours = CalculateHours(startTime, endTime);

            if (hours <= 0)
            {
                DisplayError("End time must be after start time. Please try again.");
                return GetTimeRangeAsync();
            }

            Console.WriteLine($"\nTime range: {startTime:HH\\:mm} - {endTime:HH\\:mm} ({hours:0.##} hours)");
            return (startTime, endTime, hours);
        }

        private static decimal CalculateHours(TimeOnly startTime, TimeOnly endTime)
        {
            TimeSpan duration;

            // Handle overnight shifts
            if (endTime < startTime)
            {
                // Add a day to end time if it's earlier than start time
                duration = endTime.AddHours(24) - startTime;
            }
            else
            {
                duration = endTime - startTime;
            }

            return (decimal)duration.TotalHours;
        }

        private static string GetNotesAsync()
        {
            Console.WriteLine("\nEnter notes (optional, press Enter to skip):");
            return Console.ReadLine() ?? string.Empty;
        }

        private static bool ConfirmTimeEntryAsync(DateTime date, TimeOnly startTime, TimeOnly endTime, decimal hours, string projectName, string taskName, string notes)
        {
            Console.WriteLine("\n=== Time Entry Summary ===");
            Console.WriteLine($"Date: {date:yyyy-MM-dd}");
            Console.WriteLine($"Time: {startTime:HH\\:mm} - {endTime:HH\\:mm} ({hours:0.##} hours)");
            Console.WriteLine($"Project: {projectName}");
            Console.WriteLine($"Task: {taskName}");
            Console.WriteLine($"Notes: {notes}");
            Console.WriteLine("========================");

            Console.Write("\nSubmit this time entry? (y/n): ");
            string? response = Console.ReadLine()?.Trim().ToLower();
            return response == "y" || response == "yes";
        }

        private static List<ProjectAssignment> GetProjectAssignmentsAsync()
        {
            var projectAssignments = new List<ProjectAssignment>();
            string? nextPage = "users/me/project_assignments?is_active=true";

            while (!string.IsNullOrEmpty(nextPage))
            {
                var response = _httpClient.GetAsync(nextPage).Result;
                response.EnsureSuccessStatusCode();

                var content = response.Content.ReadAsStringAsync().Result;
                var result = JsonSerializer.Deserialize<ProjectAssignmentsResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.ProjectAssignments != null)
                {
                    projectAssignments.AddRange(result.ProjectAssignments);
                }

                nextPage = result?.Links?.Next;
            }

            return projectAssignments.OrderBy(p => p.Project.Name).ToList();
        }

        private static (int projectId, int taskId, string projectName, string taskName) SelectProjectAndTaskAsync()
        {
            var projectAssignments = GetProjectAssignmentsAsync();

            if (projectAssignments.Count == 0)
            {
                throw new InvalidOperationException("No active project assignments found for your user.");
            }

            Console.WriteLine("\nAvailable Projects:");
            Console.WriteLine("------------------");
            for (int i = 0; i < projectAssignments.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {projectAssignments[i].Project.Name} ({projectAssignments[i].Client?.Name})");
            }

            int projectId = 0;
            string projectName = string.Empty;

            while (true)
            {
                Console.Write("\nEnter project number (or 0 to cancel): ");
                if (int.TryParse(Console.ReadLine(), out int selection))
                {
                    if (selection == 0)
                    {
                        return (0, 0, string.Empty, string.Empty);
                    }

                    if (selection > 0 && selection <= projectAssignments.Count)
                    {
                        var selectedProject = projectAssignments[selection - 1];
                        projectId = selectedProject.Project.Id;
                        projectName = selectedProject.Project.Name;
                        break;
                    }
                }

                DisplayError("Invalid selection. Please try again.");
            }

            // Get tasks for selected project
            var taskAssignments = projectAssignments
                .First(pa => pa.Project.Id == projectId)
                .TaskAssignments
                .OrderBy(t => t.Task.Name)
                .ToList();

            if (taskAssignments.Count == 0)
            {
                throw new InvalidOperationException($"No active tasks found for project '{projectName}'.");
            }

            Console.WriteLine("\nAvailable Tasks:");
            Console.WriteLine("---------------");
            for (int i = 0; i < taskAssignments.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {taskAssignments[i].Task.Name}");
            }

            int taskId = 0;
            string taskName = string.Empty;

            while (true)
            {
                Console.Write("\nEnter task number (or 0 to cancel): ");
                if (int.TryParse(Console.ReadLine(), out int taskSelection))
                {
                    if (taskSelection == 0)
                    {
                        return (0, 0, string.Empty, string.Empty);
                    }

                    if (taskSelection > 0 && taskSelection <= taskAssignments.Count)
                    {
                        taskId = taskAssignments[taskSelection - 1].Task.Id;
                        taskName = taskAssignments[taskSelection - 1].Task.Name;
                        break;
                    }
                }

                DisplayError("Invalid selection. Please try again.");
            }

            return (projectId, taskId, projectName, taskName);
        }

        private static void TrackTimeWithTimestampsAsync(int projectId, int taskId, DateTime date, TimeOnly startTime, TimeOnly endTime, string notes)
        {
            var timeEntryData = new TimeEntryWithTimestampsRequest
            {
                ProjectId = projectId,
                TaskId = taskId,
                SpentDate = date.ToString("yyyy-MM-dd"),
                StartedTime = startTime.ToString("HH:mm"),
                EndedTime = endTime.ToString("HH:mm"),
                Notes = notes
            };

            var content = new StringContent(
                JsonSerializer.Serialize(timeEntryData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                Encoding.UTF8,
                "application/json");

            var response = _httpClient.PostAsync("time_entries", content).Result;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = response.Content.ReadAsStringAsync().Result;
                throw new Exception($"Failed to track time. Status code: {response.StatusCode}, Response: {errorContent}");
            }
        }

        private static void DisplayError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        #region Models

        public class Project
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class Client
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class Task
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class TaskAssignment
        {
            public int Id { get; set; }

            public Task Task { get; set; } = new Task();
        }

        public class ProjectAssignment
        {
            public Project Project { get; set; } = new Project();
            public Client? Client { get; set; }

            [JsonPropertyName("task_assignments")]
            public List<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
        }

        public class Links
        {
            public string? First { get; set; }
            public string? Next { get; set; }
            public string? Previous { get; set; }
            public string? Last { get; set; }
        }

        public class ProjectAssignmentsResponse
        {
            [JsonPropertyName("project_assignments")]
            public List<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();
            public Links? Links { get; set; }
        }

        public class TimeEntryWithTimestampsRequest
        {
            [JsonPropertyName("project_id")]
            public int ProjectId { get; set; }

            [JsonPropertyName("task_id")]
            public int TaskId { get; set; }

            [JsonPropertyName("spent_date")]
            public string SpentDate { get; set; } = string.Empty;

            [JsonPropertyName("started_time")]
            public string StartedTime { get; set; } = string.Empty;

            [JsonPropertyName("ended_time")]
            public string EndedTime { get; set; } = string.Empty;

            public string Notes { get; set; } = string.Empty;
        }

        #endregion
    }
}
