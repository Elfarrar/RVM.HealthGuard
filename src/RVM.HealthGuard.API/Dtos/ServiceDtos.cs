namespace RVM.HealthGuard.API.Dtos;

public record CreateServiceRequest(
    string Name,
    string Url,
    int CheckIntervalSeconds = 30,
    int TimeoutSeconds = 10,
    int ExpectedStatusCode = 200,
    bool IsEnabled = true);

public record UpdateServiceRequest(
    string? Name,
    string? Url,
    int? CheckIntervalSeconds,
    int? TimeoutSeconds,
    int? ExpectedStatusCode,
    bool? IsEnabled);

public record ServiceResponse(
    Guid Id,
    string Name,
    string Url,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    int ExpectedStatusCode,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
