using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Infrastructure.Runtime;
using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Web.Services;

internal sealed class InputService : IInputService
{
    private readonly TouchInputHandler _handler;

    public InputService(IRuntimeAccessService runtimeAccess)
    {
        _handler = new TouchInputHandler(runtimeAccess);
    }

    public IResult HandleTouchInput(HttpContext ctx, TouchInputRequest request)
    {
        return _handler.HandleTouchInput(ctx, request);
    }

    public IResult HandleTouchStats(HttpContext ctx)
    {
        return _handler.HandleTouchStats(ctx);
    }
}
