using System.Security.Cryptography.X509Certificates;

namespace VirtualWebDisplay.Web.Hosting;

/// <summary>
/// Configura los puertos HTTP y HTTPS en Kestrel para cada runtime activo.
/// </summary>
internal static class KestrelConfigurator
{
    /// <summary>
    /// Registra un par de puertos (HTTP + HTTPS) por cada <see cref="ScreenRuntimeContext"/>.
    /// El puerto HTTPS es siempre <c>Config.Port + 1</c>.
    /// </summary>
    internal static void Configure(
        WebApplicationBuilder builder,
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        X509Certificate2 tlsCert)
    {
        Configure(builder, runtimes.Select(r => r.Config.Port).ToArray(), tlsCert);
    }

    internal static void Configure(
        WebApplicationBuilder builder,
        IReadOnlyList<int> ports,
        X509Certificate2 tlsCert)
    {
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            foreach (var port in ports)
            {
                kestrel.ListenAnyIP(port);
                kestrel.ListenAnyIP(port + 1, listenOptions =>
                    listenOptions.UseHttps(tlsCert));
            }
        });
    }
}
