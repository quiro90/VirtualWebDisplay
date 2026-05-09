using Microsoft.AspNetCore.Http;
using VirtualWebDisplay.Web.Security;

namespace VirtualWebDisplay.Tests.Web.Security;

public sealed class ScreenSecurityGateTests
{
    private const string CookieName = "vwd_session";

    // ── Disabled gate ───────────────────────────────────────────────────────

    [Fact]
    public void Disabled_IsAuthorized_ReturnsTrue()
    {
        var gate = new ScreenSecurityGate(enabled: false);
        var ctx = CreateContext();

        Assert.True(gate.IsAuthorized(ctx, CookieName));
    }

    [Fact]
    public void Disabled_TryAuthorize_ReturnsSuccess()
    {
        var gate = new ScreenSecurityGate(enabled: false);
        var ctx = CreateContext();

        var result = gate.TryAuthorize(ctx, CookieName, submittedCode: null);

        Assert.True(result.Authorized);
        Assert.False(result.TooManyAttempts);
    }

    [Fact]
    public void Disabled_GetClientWindowState_FullAttemptsRemaining()
    {
        var gate = new ScreenSecurityGate(enabled: false);
        var ctx = CreateContext();

        var state = gate.GetClientWindowState(ctx);

        Assert.True(state.AttemptsRemaining > 0);
        Assert.Equal(0, state.RetryAfterSeconds);
    }

    // ── Enabled – access code ───────────────────────────────────────────────

    [Fact]
    public void Enabled_AccessCode_HasExpectedLength()
    {
        var gate = new ScreenSecurityGate(enabled: true);

        Assert.Equal(6, gate.AccessCode.Length);
        Assert.NotEmpty(gate.AccessCode);
    }

    [Fact]
    public void Enabled_TwoInstances_ProduceDifferentCodes()
    {
        var g1 = new ScreenSecurityGate(enabled: true);
        var g2 = new ScreenSecurityGate(enabled: true);

        // Probabilistically true (alphabet 32^6 ≈ 10^9 combinations)
        Assert.NotEqual(g1.AccessCode, g2.AccessCode);
    }

    [Fact]
    public void Enabled_DisabledGate_HasEmptyAccessCode()
    {
        var gate = new ScreenSecurityGate(enabled: false);

        Assert.Equal(string.Empty, gate.AccessCode);
    }

    // ── Enabled – authorization flow ────────────────────────────────────────

    [Fact]
    public void Enabled_IsAuthorized_ReturnsFalse_WhenNoCookie()
    {
        var gate = new ScreenSecurityGate(enabled: true);
        var ctx = CreateContext();

        Assert.False(gate.IsAuthorized(ctx, CookieName));
    }

    [Fact]
    public void Enabled_TryAuthorize_WithCorrectCode_SetsSessionCookie()
    {
        var gate = new ScreenSecurityGate(enabled: true);
        var ctx = CreateContext();

        var result = gate.TryAuthorize(ctx, CookieName, gate.AccessCode);

        Assert.True(result.Authorized);
        Assert.False(result.TooManyAttempts);
        Assert.True(ctx.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public void Enabled_TryAuthorize_WithCorrectCode_IsCaseInsensitive()
    {
        var gate = new ScreenSecurityGate(enabled: true);
        var ctx = CreateContext();

        var result = gate.TryAuthorize(ctx, CookieName, gate.AccessCode.ToLowerInvariant());

        Assert.True(result.Authorized);
    }

    [Fact]
    public void Enabled_TryAuthorize_WithWrongCode_ReturnsInvalidCode()
    {
        var gate = new ScreenSecurityGate(enabled: true);
        var ctx = CreateContext();

        var result = gate.TryAuthorize(ctx, CookieName, "ZZZZZZ");

        Assert.False(result.Authorized);
        Assert.False(result.TooManyAttempts);
        Assert.True(result.AttemptsRemaining >= 0);
    }

    [Fact]
    public void Enabled_TryAuthorize_WithNullCode_ReturnsInvalidCode()
    {
        var gate = new ScreenSecurityGate(enabled: true);
        var ctx = CreateContext();

        var result = gate.TryAuthorize(ctx, CookieName, submittedCode: null);

        Assert.False(result.Authorized);
        Assert.False(result.TooManyAttempts);
    }

    // ── Failed attempts & rate limiting ────────────────────────────────────

    [Fact]
    public void Enabled_RepeatedWrongCodes_DecrementAttemptsRemaining()
    {
        var gate = new ScreenSecurityGate(enabled: true);

        int? firstRemaining = null;
        int? lastRemaining = null;

        for (var i = 0; i < 4; i++)
        {
            var ctx = CreateContext();
            var result = gate.TryAuthorize(ctx, CookieName, "WRONG1");
            lastRemaining = result.AttemptsRemaining;
            firstRemaining ??= result.AttemptsRemaining + i;
        }

        Assert.True(lastRemaining < firstRemaining);
    }

    [Fact]
    public void Enabled_MaxFailedAttempts_BlocksClient()
    {
        var gate = new ScreenSecurityGate(enabled: true);

        SecurityAuthorizeResult last = default!;
        for (var i = 0; i < 6; i++)
        {
            var ctx = CreateContext();
            last = gate.TryAuthorize(ctx, CookieName, "BADCODE");
        }

        Assert.False(last.Authorized);
        Assert.True(last.TooManyAttempts || last.AttemptsRemaining == 0);
    }

    [Fact]
    public void Enabled_AfterMaxFailures_GetClientWindowState_HasRetryAfter()
    {
        var gate = new ScreenSecurityGate(enabled: true);

        for (var i = 0; i < 5; i++)
            gate.TryAuthorize(CreateContext(), CookieName, "BAD");

        var state = gate.GetClientWindowState(CreateContext());

        Assert.Equal(0, state.AttemptsRemaining);
        Assert.True(state.RetryAfterSeconds > 0);
    }

    [Fact]
    public void Enabled_BlockedClient_CorrectCode_StillBlocked()
    {
        var gate = new ScreenSecurityGate(enabled: true);

        for (var i = 0; i < 5; i++)
            gate.TryAuthorize(CreateContext(), CookieName, "WRONG1");

        var result = gate.TryAuthorize(CreateContext(), CookieName, gate.AccessCode);

        Assert.False(result.Authorized);
        Assert.True(result.TooManyAttempts);
    }

    // ── SecurityAuthorizeResult factory methods ─────────────────────────────

    [Fact]
    public void SecurityAuthorizeResult_Success_IsAuthorized()
    {
        var result = SecurityAuthorizeResult.Success(5);

        Assert.True(result.Authorized);
        Assert.False(result.TooManyAttempts);
        Assert.Equal(5, result.AttemptsRemaining);
        Assert.Equal(0, result.RetryAfterSeconds);
    }

    [Fact]
    public void SecurityAuthorizeResult_InvalidCode_IsNotAuthorized()
    {
        var result = SecurityAuthorizeResult.InvalidCode(3);

        Assert.False(result.Authorized);
        Assert.False(result.TooManyAttempts);
        Assert.Equal(3, result.AttemptsRemaining);
    }

    [Fact]
    public void SecurityAuthorizeResult_InvalidCode_ClampsNegativeAttempts()
    {
        var result = SecurityAuthorizeResult.InvalidCode(-2);

        Assert.Equal(0, result.AttemptsRemaining);
    }

    [Fact]
    public void SecurityAuthorizeResult_Blocked_IsTooManyAttempts()
    {
        var result = SecurityAuthorizeResult.Blocked(30);

        Assert.False(result.Authorized);
        Assert.True(result.TooManyAttempts);
        Assert.Equal(30, result.RetryAfterSeconds);
        Assert.Equal(0, result.AttemptsRemaining);
    }

    [Fact]
    public void SecurityAuthorizeResult_Blocked_ClampsZeroSeconds()
    {
        var result = SecurityAuthorizeResult.Blocked(0);

        Assert.True(result.RetryAfterSeconds >= 1);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static DefaultHttpContext CreateContext(string remoteIp = "192.168.1.1")
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }
}
