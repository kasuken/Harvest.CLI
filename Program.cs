using Harvest.CLI;
using Harvest.CLI.Configuration;
using Harvest.CLI.Services;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .Build();

var settings = configuration.GetSection("Harvest").Get<HarvestSettings>() ?? new HarvestSettings();

var accountId = !string.IsNullOrWhiteSpace(settings.AccountId)
    ? settings.AccountId
    : Environment.GetEnvironmentVariable("HARVEST_ACCOUNT_ID")
      ?? throw new InvalidOperationException("Harvest Account ID not found in configuration or environment variables.");

var accessToken = !string.IsNullOrWhiteSpace(settings.AccessToken)
    ? settings.AccessToken
    : Environment.GetEnvironmentVariable("HARVEST_ACCESS_TOKEN")
      ?? throw new InvalidOperationException("Harvest Access Token not found in configuration or environment variables.");

using var apiClient = new HarvestApiClient(accountId, accessToken);
var csvService = new CsvImportService();
var app = new Application(apiClient, csvService, settings.Import);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await app.RunAsync(cts.Token);
