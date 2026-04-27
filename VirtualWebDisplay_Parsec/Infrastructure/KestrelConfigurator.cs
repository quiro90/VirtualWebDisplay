using System.Security.Cryptography.X509Certificates;

namespace VirtualWebDisplay.Infrastructure;

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
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            foreach (var runtime in runtimes)
            {
                kestrel.ListenAnyIP(runtime.Config.Port);
                kestrel.ListenAnyIP(runtime.Config.Port + 1, listenOptions =>
                    listenOptions.UseHttps(tlsCert));
            }
        });
    }
}
