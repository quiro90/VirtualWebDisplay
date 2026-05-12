using Microsoft.Extensions.DependencyInjection;
using VirtualWebDisplay.Infrastructure.Runtime;

namespace VirtualWebDisplay.Web.Services;

internal static class WebEndpointServiceCollectionExtensions
{
    public static IServiceCollection AddWebEndpointServices(
        this IServiceCollection services,
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        byte[] tlsCertDerBytes)
    {
        services.AddSingleton(runtimes);
        services.AddSingleton<IRuntimeAccessService, RuntimeAccessService>();

        services.AddSingleton<WebImagePageTemplate>();
        services.AddSingleton<RtcPageTemplate>();
        services.AddSingleton<SecurityPageTemplate>();
        services.AddSingleton<ViewerLimitPageTemplate>();

        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IIndexPageService, IndexPageService>();
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IKeepaliveService, KeepaliveService>();
        services.AddSingleton<IInputService, InputService>();
        services.AddSingleton<ICaptureService, CaptureService>();
        services.AddSingleton<IWebRtcOfferService, WebRtcOfferService>();
        services.AddSingleton<IWebEndpointOrchestrator>(provider =>
            new DefaultWebEndpointOrchestrator(
                provider.GetRequiredService<IAuthService>(),
                provider.GetRequiredService<IIndexPageService>(),
                provider.GetRequiredService<IConfigService>(),
                provider.GetRequiredService<IKeepaliveService>(),
                provider.GetRequiredService<ICaptureService>(),
                provider.GetRequiredService<IWebRtcOfferService>(),
                provider.GetRequiredService<IInputService>(),
                tlsCertDerBytes));

        return services;
    }
}
