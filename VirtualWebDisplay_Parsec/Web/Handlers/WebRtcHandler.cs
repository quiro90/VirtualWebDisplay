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
        var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);

        if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
            return RuntimeAccessHelper.UnauthorizedResult(runtime);

        if (!TransmissionModeOptions.IsRtc(runtime.Config.TransmissionMethod))
            return Results.BadRequest(new { error = AppText.Get("Program_WebRtcDisabled_Error") });

        if (!runtime.ViewerLimiter.CanAcceptWebRtc())
            return Results.Json(
                new { error = AppText.Get("Program_ViewerLimit_Full_Error") },
                statusCode: StatusCodes.Status429TooManyRequests);

        if (!string.Equals(offer.Type, "offer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(offer.Sdp))
            return Results.BadRequest(new { error = AppText.Get("Program_WebRtcInvalidOffer_Error") });

        var answer = await runtime.WebRtcStreamService.CreateAnswerAsync(offer, cancellationToken);
        return Results.Json(answer);
    }
}
