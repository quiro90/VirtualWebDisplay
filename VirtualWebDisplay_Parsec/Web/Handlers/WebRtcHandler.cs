using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.Streaming.Models;

namespace VirtualWebDisplay.Web.Handlers;

/// <summary>
/// Maneja la negociación WebRTC (POST /webrtc/offer).
/// </summary>
internal static class WebRtcHandler
{
    internal static async Task<IResult> HandleOffer(
        HttpContext ctx,
        WebRtcSessionOffer offer,
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        CancellationToken cancellationToken)
    {
        if (!RuntimeAccessHelper.TryResolveAuthorizedRuntime(ctx, runtimes, out var runtime, out var runtimeError))
            return runtimeError!;

        if (!TransmissionModeOptions.IsRtc(runtime.Config.TransmissionMethod))
            return RuntimeAccessHelper.BadRequestError(AppText.Get("Program_WebRtcDisabled_Error"));

        if (!runtime.ViewerLimiter.CanAcceptWebRtc())
            return RuntimeAccessHelper.ViewerLimitExceededResult();

        if (!string.Equals(offer.Type, "offer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(offer.Sdp))
            return RuntimeAccessHelper.BadRequestError(AppText.Get("Program_WebRtcInvalidOffer_Error"));

        var answer = await runtime.WebRtcStreamService.CreateAnswerAsync(offer, cancellationToken);
        return Results.Json(answer);
    }
}
