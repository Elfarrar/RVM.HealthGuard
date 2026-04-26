using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RVM.HealthGuard.API.Services;
using System.Net;

namespace RVM.HealthGuard.Test.Services;

public class NotifyAlertServiceTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static IConfiguration BuildConfig(string? baseUrl, string? apiKey)
    {
        var dict = new Dictionary<string, string?>();
        if (baseUrl is not null) dict["RvmNotify:BaseUrl"] = baseUrl;
        if (apiKey is not null) dict["RvmNotify:ApiKey"] = apiKey;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static NotifyAlertService CreateService(
        IConfiguration config,
        HttpMessageHandler? handler = null)
    {
        var factoryMock = new Mock<IHttpClientFactory>();
        var loggerMock = new Mock<ILogger<NotifyAlertService>>();

        if (handler is not null)
        {
            var client = new HttpClient(handler);
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        }
        else
        {
            // Por padrao retorna cliente que nao precisa de rede (nao configurado = skip)
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient());
        }

        return new NotifyAlertService(factoryMock.Object, config, loggerMock.Object);
    }

    // -------------------------------------------------------------------------
    // SendIncidentAlertAsync — config ausente
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendIncidentAlert_NoBaseUrl_SkipsSilently()
    {
        var config = BuildConfig(null, "key");
        var svc = CreateService(config);

        // Nao deve lancar excecao
        var ex = await Record.ExceptionAsync(
            () => svc.SendIncidentAlertAsync("MyAPI", "Down", null, CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SendIncidentAlert_NoApiKey_SkipsSilently()
    {
        var config = BuildConfig("https://notify.example.com", null);
        var svc = CreateService(config);

        var ex = await Record.ExceptionAsync(
            () => svc.SendIncidentAlertAsync("MyAPI", "Down", null, CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SendIncidentAlert_EmptyBaseUrl_SkipsSilently()
    {
        var config = BuildConfig("", "key");
        var svc = CreateService(config);

        var ex = await Record.ExceptionAsync(
            () => svc.SendIncidentAlertAsync("MyAPI", "Down", null, CancellationToken.None));

        Assert.Null(ex);
    }

    // -------------------------------------------------------------------------
    // SendIncidentAlertAsync — envia HTTP com config valida
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendIncidentAlert_ValidConfig_SendsHttpPost()
    {
        var requestReceived = false;
        var handler = new MockHttpHandler(req =>
        {
            requestReceived = true;
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Contains("/api/alerts", req.RequestUri!.PathAndQuery);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var config = BuildConfig("https://notify.example.com", "secret-key");
        var svc = CreateService(config, handler);

        await svc.SendIncidentAlertAsync("MyAPI", "Down", "Connection refused", CancellationToken.None);

        Assert.True(requestReceived);
    }

    [Fact]
    public async Task SendIncidentAlert_DegradedType_UsesWarningLevel()
    {
        string? bodyContent = null;
        var handler = new MockHttpHandler(async req =>
        {
            bodyContent = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var config = BuildConfig("https://notify.example.com", "key");
        var svc = CreateService(config, handler);

        await svc.SendIncidentAlertAsync("MyAPI", "Degraded", null, CancellationToken.None);

        Assert.NotNull(bodyContent);
        Assert.Contains("\"warning\"", bodyContent);
    }

    [Fact]
    public async Task SendIncidentAlert_DownType_UsesCriticalLevel()
    {
        string? bodyContent = null;
        var handler = new MockHttpHandler(async req =>
        {
            bodyContent = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var config = BuildConfig("https://notify.example.com", "key");
        var svc = CreateService(config, handler);

        await svc.SendIncidentAlertAsync("MyAPI", "Down", null, CancellationToken.None);

        Assert.Contains("\"critical\"", bodyContent!);
    }

    [Fact]
    public async Task SendIncidentAlert_WithErrorMessage_IncludesInMessage()
    {
        string? bodyContent = null;
        var handler = new MockHttpHandler(async req =>
        {
            bodyContent = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var config = BuildConfig("https://notify.example.com", "key");
        var svc = CreateService(config, handler);

        await svc.SendIncidentAlertAsync("MyAPI", "Down", "Connection timeout", CancellationToken.None);

        Assert.Contains("Connection timeout", bodyContent!);
    }

    [Fact]
    public async Task SendIncidentAlert_HttpException_DoesNotThrow()
    {
        Func<HttpRequestMessage, Task<HttpResponseMessage>> throwingFunc =
            _ => throw new HttpRequestException("Network error");
        var handler = new MockHttpHandler(throwingFunc);
        var config = BuildConfig("https://notify.example.com", "key");
        var svc = CreateService(config, handler);

        // Deve absorver excecao e apenas logar warning
        var ex = await Record.ExceptionAsync(
            () => svc.SendIncidentAlertAsync("MyAPI", "Down", null, CancellationToken.None));

        Assert.Null(ex);
    }

    // -------------------------------------------------------------------------
    // SendResolutionAlertAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendResolutionAlert_NoConfig_SkipsSilently()
    {
        var config = BuildConfig(null, null);
        var svc = CreateService(config);

        var ex = await Record.ExceptionAsync(
            () => svc.SendResolutionAlertAsync("MyAPI", TimeSpan.FromMinutes(10), CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SendResolutionAlert_ValidConfig_SendsHttpPost()
    {
        var called = false;
        var handler = new MockHttpHandler(req =>
        {
            called = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var config = BuildConfig("https://notify.example.com", "key");
        var svc = CreateService(config, handler);

        await svc.SendResolutionAlertAsync("MyAPI", TimeSpan.FromMinutes(10), CancellationToken.None);

        Assert.True(called);
    }

    [Fact]
    public async Task SendResolutionAlert_WithDuration_IncludesDurationInMessage()
    {
        string? bodyContent = null;
        var handler = new MockHttpHandler(async req =>
        {
            bodyContent = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var config = BuildConfig("https://notify.example.com", "key");
        var svc = CreateService(config, handler);

        await svc.SendResolutionAlertAsync("MyAPI", TimeSpan.FromMinutes(15), CancellationToken.None);

        Assert.Contains("15", bodyContent!); // "durou 15min"
    }

    [Fact]
    public async Task SendResolutionAlert_NullDuration_NoCrash()
    {
        var handler = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var config = BuildConfig("https://notify.example.com", "key");
        var svc = CreateService(config, handler);

        var ex = await Record.ExceptionAsync(
            () => svc.SendResolutionAlertAsync("MyAPI", null, CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SendResolutionAlert_UsesInfoLevel()
    {
        string? bodyContent = null;
        var handler = new MockHttpHandler(async req =>
        {
            bodyContent = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var config = BuildConfig("https://notify.example.com", "key");
        var svc = CreateService(config, handler);

        await svc.SendResolutionAlertAsync("MyAPI", null, CancellationToken.None);

        Assert.Contains("\"info\"", bodyContent!);
    }
}

// -------------------------------------------------------------------------
// Helper — delegating HttpMessageHandler
// -------------------------------------------------------------------------

file sealed class MockHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : this(req => Task.FromResult(handler(req))) { }

    public MockHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => _handler(request);
}
