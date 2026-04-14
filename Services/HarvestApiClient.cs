using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Harvest.CLI.Models;

namespace Harvest.CLI.Services;

/// <summary>
/// Async HTTP client for the Harvest V2 API.
/// </summary>
public sealed class HarvestApiClient : IDisposable
{
    private readonly HttpClient _httpClient = new();

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HarvestApiClient(string accountId, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Account ID is required.", nameof(accountId));
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is required.", nameof(accessToken));

        _httpClient.BaseAddress = new Uri("https://api.harvestapp.com/v2/");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Harvest-Account-Id", accountId);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Harvest.CLI");
    }

    /// <summary>
    /// Retrieves all active project assignments for the authenticated user.
    /// </summary>
    public async Task<List<ProjectAssignment>> GetProjectAssignmentsAsync(CancellationToken ct = default)
    {
        var assignments = new List<ProjectAssignment>();
        string? nextPage = "users/me/project_assignments?is_active=true";

        while (!string.IsNullOrEmpty(nextPage))
        {
            using var response = await _httpClient.GetAsync(nextPage, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<ProjectAssignmentsResponse>(content, s_jsonOptions);

            if (result?.ProjectAssignments is not null)
                assignments.AddRange(result.ProjectAssignments);

            nextPage = result?.Links?.Next;
        }

        return assignments.OrderBy(p => p.Project.Name).ToList();
    }

    /// <summary>
    /// Submits a time entry to Harvest.
    /// </summary>
    public async Task SubmitTimeEntryAsync(TimeEntryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("time_entries", content, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Failed to submit time entry. Status: {response.StatusCode}, Response: {error}");
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
