namespace RVM.HealthGuard.API.Dtos;

public record ServiceStatusResponse(
    Guid ServiceId,
    string ServiceName,
    string CurrentStatus,
    int? LastResponseTimeMs,
    DateTime? LastCheckedAt,
    double UptimePercent24h,
    double AverageResponseTimeMs24h);

public record HealthCheckResultResponse(
    Guid Id,
    string Status,
    int ResponseTimeMs,
    int? StatusCode,
    string? ErrorMessage,
    DateTime CheckedAt);

public record ServiceStatusUpdate(
    Guid ServiceId,
    string ServiceName,
    string Status,
    int ResponseTimeMs,
    int? StatusCode,
    string? ErrorMessage,
    DateTime CheckedAt);
