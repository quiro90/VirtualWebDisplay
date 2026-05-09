using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Drivers;
using VirtualWebDisplay.Infrastructure.Runtime;

namespace VirtualWebDisplay.Tests.Web.Handlers;

internal static class WebHandlerTestHelper
{
    internal static ScreenRuntimeContext CreateRuntime(
        Action<VirtualScreenConfig>? configure = null,
        string id = "screen1",
        string displayName = "Screen 1")
    {
        var config = new VirtualScreenConfig
        {
            Port = 8000,
            TouchInputEnabled = true,
            ScreenSecurityEnabled = false,
            TouchHoldEnabled = true,
            TouchScrollEnabled = true,
            TouchPreserveCursor = false,
        };
        configure?.Invoke(config);

        return new ScreenRuntimeContext(
            id: id,
            displayName: displayName,
            config: config,
            hostName: "localhost",
            localIp: "127.0.0.1",
            driverVerifier: new FakeDriverVerifier());
    }

    internal static DefaultHttpContext CreateHttpContext(int localPort, bool isHttps = false)
    {
        var context = new DefaultHttpContext();
        context.Connection.LocalPort = localPort;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        context.Request.IsHttps = isHttps;
        context.Response.Body = new MemoryStream();
        context.RequestServices = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        return context;
    }

    internal static async Task<(int StatusCode, string Body, IHeaderDictionary Headers)> ExecuteResultAsync(IResult result, DefaultHttpContext context)
    {
        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        return (context.Response.StatusCode, body, context.Response.Headers);
    }

    private sealed class FakeDriverVerifier : IDriverVerifier
    {
        public (bool isAvailable, string statusMessage) Verify() => (true, "ok");
        public string InstallUrl => "https://example.test/driver";
        public string DriverName => "Fake Driver";
    }
}
