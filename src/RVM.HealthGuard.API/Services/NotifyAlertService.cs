using System.Text;
using System.Text.Json;

namespace RVM.HealthGuard.API.Services;

public class NotifyAlertService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<NotifyAlertService> logger)
{
    public async Task SendIncidentAlertAsync(string serviceName, string incidentType, string? errorMessage, CancellationToken ct)
    {
        var baseUrl = configuration["RvmNotify:BaseUrl"];
        var apiKey = configuration["RvmNotify:ApiKey"];

        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
        {
            logger.LogDebug("RvmNotify nao configurado, alerta ignorado para {Service}", serviceName);
            return;
        }

        var message = $"[HealthGuard] {serviceName} — {incidentType}";
        if (!string.IsNullOrWhiteSpace(errorMessage))
            message += $": {errorMessage}";

        var payload = new
        {
            service = "HealthGuard",
            level = incidentType == "Degraded" ? "warning" : "critical",
            message,
            timestamp = DateTime.UtcNow,
        };

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/alerts")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("X-Api-Key", apiKey);

            var response = await client.SendAsync(request, ct);

            logger.LogInformation("Alerta enviado ao RVM.Notify para {Service}: {Status}", serviceName, response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar alerta ao RVM.Notify para {Service}", serviceName);
        }
    }

    public async Task SendResolutionAlertAsync(string serviceName, TimeSpan? duration, CancellationToken ct)
    {
        var baseUrl = configuration["RvmNotify:BaseUrl"];
        var apiKey = configuration["RvmNotify:ApiKey"];

        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
            return;

        var durationText = duration.HasValue ? $" (durou {duration.Value.TotalMinutes:F0}min)" : "";
        var payload = new
        {
            service = "HealthGuard",
            level = "info",
            message = $"[HealthGuard] {serviceName} — Recuperado{durationText}",
            timestamp = DateTime.UtcNow,
        };

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/alerts")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("X-Api-Key", apiKey);

            await client.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar alerta de resolucao ao RVM.Notify para {Service}", serviceName);
        }
    }
}
