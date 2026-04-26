using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using RVM.HealthGuard.API.Auth;
using RVM.HealthGuard.API.Health;
using RVM.HealthGuard.API.Hubs;
using RVM.HealthGuard.API.Middleware;
using RVM.HealthGuard.API.Services;
using RVM.HealthGuard.Infrastructure;
using RVM.HealthGuard.Infrastructure.Data;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(new RenderedCompactJsonFormatter());

        var seqUrl = context.Configuration["Seq:ServerUrl"];
        if (!string.IsNullOrEmpty(seqUrl))
            configuration.WriteTo.Seq(seqUrl);
    });

    // Controllers + OpenAPI
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // SignalR
    builder.Services.AddSignalR();

    // Infrastructure (DbContext + Repositories)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Forwarded Headers
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database");

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.AddPolicy("api-key", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Items["AppId"]?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1)
                }));
    });

    // Authentication
    builder.Services.AddAuthentication(ApiKeyAuthOptions.Scheme)
        .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(ApiKeyAuthOptions.Scheme, options =>
        {
            builder.Configuration.GetSection("ApiKeys").Bind(options);
        });

    builder.Services.AddAuthorization();

    // Application Services
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<UptimeCalculatorService>();
    builder.Services.AddSingleton<NotifyAlertService>();
    builder.Services.AddHostedService<HealthCheckWorker>();

    var app = builder.Build();

    // Auto-migrate
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<HealthGuardDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    // PathBase (behind reverse proxy)
    var pathBase = app.Configuration["App:PathBase"];
    if (!string.IsNullOrEmpty(pathBase))
        app.UsePathBase(pathBase);

    // Middleware pipeline
    app.UseForwardedHeaders();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.Use(async (context, next) =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "font-src 'self'; " +
            "img-src 'self' data:; " +
            "connect-src 'self' wss:; " +
            "frame-ancestors 'none';";
        await next();
    });

    app.UseStaticFiles();
    app.UseAntiforgery();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // Routes
    app.MapControllers();
    app.MapRazorComponents<RVM.HealthGuard.API.Components.App>()
        .AddInteractiveServerRenderMode();
    app.MapHub<HealthStatusHub>("/hubs/health-status").AllowAnonymous();
    app.MapHealthChecks("/health").AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
