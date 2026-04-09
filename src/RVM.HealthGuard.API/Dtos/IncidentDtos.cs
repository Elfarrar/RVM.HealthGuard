namespace RVM.HealthGuard.API.Dtos;

public record IncidentResponse(
    Guid Id,
    Guid ServiceId,
    string ServiceName,
    string Type,
    DateTime StartedAt,
    DateTime? ResolvedAt,
    string? Duration);
