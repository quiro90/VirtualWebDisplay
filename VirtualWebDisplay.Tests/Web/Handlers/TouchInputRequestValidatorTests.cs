using VirtualWebDisplay.Web.Api;
using VirtualWebDisplay.Web.Handlers;

namespace VirtualWebDisplay.Tests.Web.Handlers;

public sealed class TouchInputRequestValidatorTests
{
    [Fact]
    public void TryValidate_ReturnsError_WhenRequestIsNull()
    {
        var result = TouchInputRequestValidator.TryValidate(null, out var errorMessage);

        Assert.False(result);
        Assert.Equal(TouchInputRequestValidator.MissingBodyError, errorMessage);
    }

    [Fact]
    public void TryValidate_ReturnsError_WhenTypeIsNull()
    {
        var request = new TouchInputRequest { Type = null! };

        var result = TouchInputRequestValidator.TryValidate(request, out var errorMessage);

        Assert.False(result);
        Assert.Equal(TouchInputRequestValidator.MissingTypeError, errorMessage);
    }

    [Fact]
    public void TryValidate_ReturnsError_WhenTypeIsEmpty()
    {
        var request = new TouchInputRequest { Type = string.Empty };

        var result = TouchInputRequestValidator.TryValidate(request, out var errorMessage);

        Assert.False(result);
        Assert.Equal(TouchInputRequestValidator.MissingTypeError, errorMessage);
    }

    [Fact]
    public void TryValidate_ReturnsTrue_WhenTypeIsPresent()
    {
        var request = new TouchInputRequest { Type = TouchInputActions.LegacyTouchMove };

        var result = TouchInputRequestValidator.TryValidate(request, out var errorMessage);

        Assert.True(result);
        Assert.Equal(string.Empty, errorMessage);
    }
}
