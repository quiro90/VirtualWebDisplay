using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Infrastructure.Runtime;

internal interface IRuntimeAccessService
{
    string SecurityCookieName(ScreenRuntimeContext runtime);
    ScreenRuntimeContext ResolveRuntime(HttpContext context);
    bool IsAuthorized(HttpContext context, ScreenRuntimeContext runtime);
    bool TryResolveAuthorizedRuntime(HttpContext context, out ScreenRuntimeContext runtime, out IResult? unauthorizedResult);
    string ResolveViewerKey(HttpContext context, ScreenRuntimeContext runtime);
    IResult UnauthorizedResult(ScreenRuntimeContext runtime);
    IResult BadRequestError(string message);
    IResult AuthorizedResult();
    IResult NotFoundResult();
    IResult TooManyRequestsResult();
    IResult InternalServerErrorResult();
    IResult ServiceUnavailableResult();
    IResult HtmlContent(string html);
    IResult ViewerLimitExceededResult();
    Task WriteViewerLimitExceededAsync(HttpContext context);
}

internal sealed class RuntimeAccessService : IRuntimeAccessService
{
    private readonly IReadOnlyList<ScreenRuntimeContext> _runtimes;

    public RuntimeAccessService(IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        _runtimes = runtimes;
    }

    public string SecurityCookieName(ScreenRuntimeContext runtime) => RuntimeAccessHelper.SecurityCookieName(runtime);

    public ScreenRuntimeContext ResolveRuntime(HttpContext context) => RuntimeAccessHelper.ResolveRuntime(context, _runtimes);

    public bool IsAuthorized(HttpContext context, ScreenRuntimeContext runtime) => RuntimeAccessHelper.IsAuthorized(context, runtime);

    public bool TryResolveAuthorizedRuntime(HttpContext context, out ScreenRuntimeContext runtime, out IResult? unauthorizedResult)
        => RuntimeAccessHelper.TryResolveAuthorizedRuntime(context, _runtimes, out runtime, out unauthorizedResult);

    public string ResolveViewerKey(HttpContext context, ScreenRuntimeContext runtime)
        => RuntimeAccessHelper.ResolveViewerKey(context, runtime);

    public IResult UnauthorizedResult(ScreenRuntimeContext runtime) => RuntimeAccessHelper.UnauthorizedResult(runtime);
    public IResult BadRequestError(string message) => RuntimeAccessHelper.BadRequestError(message);
    public IResult AuthorizedResult() => RuntimeAccessHelper.AuthorizedResult();
    public IResult NotFoundResult() => RuntimeAccessHelper.NotFoundResult();
    public IResult TooManyRequestsResult() => RuntimeAccessHelper.TooManyRequestsResult();
    public IResult InternalServerErrorResult() => RuntimeAccessHelper.InternalServerErrorResult();
    public IResult ServiceUnavailableResult() => RuntimeAccessHelper.ServiceUnavailableResult();
    public IResult HtmlContent(string html) => RuntimeAccessHelper.HtmlContent(html);
    public IResult ViewerLimitExceededResult() => RuntimeAccessHelper.ViewerLimitExceededResult();
    public Task WriteViewerLimitExceededAsync(HttpContext context) => RuntimeAccessHelper.WriteViewerLimitExceededAsync(context);
}
