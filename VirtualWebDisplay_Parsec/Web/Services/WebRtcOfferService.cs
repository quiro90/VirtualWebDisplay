using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Infrastructure.Runtime;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.Streaming.Models;

namespace VirtualWebDisplay.Web.Services;

internal sealed class WebRtcOfferService : IWebRtcOfferService
{
    private readonly IRuntimeAccessService _runtimeAccess;

    public WebRtcOfferService(IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
    }

    public async Task<IResult> HandleOffer(HttpContext ctx, WebRtcSessionOffer offer, CancellationToken cancellationToken)
    {
        if (!_runtimeAccess.TryResolveAuthorizedRuntime(ctx, out var runtime, out var runtimeError))
            return runtimeError!;

        if (!TransmissionModeOptions.IsRtc(runtime.Config.TransmissionMethod))
            return _runtimeAccess.BadRequestError(AppText.Get("Program_WebRtcDisabled_Error"));

        if (!runtime.ViewerLimiter.CanAcceptWebRtc())
            return _runtimeAccess.ViewerLimitExceededResult();

        if (!string.Equals(offer.Type, "offer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(offer.Sdp))
            return _runtimeAccess.BadRequestError(AppText.Get("Program_WebRtcInvalidOffer_Error"));

        var answer = await runtime.WebRtcStreamService.CreateAnswerAsync(offer, cancellationToken);
        return Results.Json(answer);
    }
}
