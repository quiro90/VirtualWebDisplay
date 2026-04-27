using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace VirtualWebDisplay.Infrastructure;

public sealed class ScreenSecurityGate
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromSeconds(45);
    private const int AccessCodeLength = 6;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _authorizedSessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _failedAttemptsByClient = new(StringComparer.OrdinalIgnoreCase);

    public ScreenSecurityGate(bool enabled)
    {
        Enabled = enabled;
        AccessCode = enabled ? GenerateAccessCode(AccessCodeLength) : string.Empty;
    }

    public bool Enabled { get; }
    public string AccessCode { get; }

    public SecurityClientWindowState GetClientWindowState(HttpContext context)
    {
        if (!Enabled)
            return new SecurityClientWindowState(MaxAttempts, 0);

        var clientKey = BuildClientKey(context);
        var now = DateTimeOffset.UtcNow;

        if (!_failedAttemptsByClient.TryGetValue(clientKey, out var attempts))
            return new SecurityClientWindowState(MaxAttempts, 0);

        lock (attempts)
        {
            PruneOldAttempts(attempts, now);
            if (attempts.Count < MaxAttempts)
                return new SecurityClientWindowState(MaxAttempts - attempts.Count, 0);

            var oldest = attempts[0];
            var retryAfter = (int)Math.Ceiling((oldest + AttemptWindow - now).TotalSeconds);
            return new SecurityClientWindowState(0, Math.Max(1, retryAfter));
        }
    }

    public bool IsAuthorized(HttpContext context, string cookieName)
    {
        if (!Enabled)
            return true;

        if (!context.Request.Cookies.TryGetValue(cookieName, out var sessionId) || string.IsNullOrWhiteSpace(sessionId))
            return false;

        return _authorizedSessions.ContainsKey(sessionId);
    }

    public SecurityAuthorizeResult TryAuthorize(HttpContext context, string cookieName, string? submittedCode)
    {
        if (!Enabled)
            return SecurityAuthorizeResult.Success(MaxAttempts);

        var state = GetClientWindowState(context);
        if (state.RetryAfterSeconds > 0)
            return SecurityAuthorizeResult.Blocked(state.RetryAfterSeconds);

        if (string.IsNullOrWhiteSpace(submittedCode))
            return SecurityAuthorizeResult.InvalidCode(state.AttemptsRemaining);

        var normalizedSubmitted = submittedCode.Trim().ToUpperInvariant();
        if (!string.Equals(normalizedSubmitted, AccessCode, StringComparison.Ordinal))
        {
            var updatedState = RegisterFailedAttempt(context);
            return SecurityAuthorizeResult.InvalidCode(updatedState.AttemptsRemaining);
        }

        var sessionId = Guid.NewGuid().ToString("N");
        _authorizedSessions.TryAdd(sessionId, DateTimeOffset.UtcNow);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
        };

        context.Response.Cookies.Append(cookieName, sessionId, cookieOptions);

        var clientKey = BuildClientKey(context);
        _failedAttemptsByClient.TryRemove(clientKey, out _);

        return SecurityAuthorizeResult.Success(MaxAttempts);
    }

    private SecurityClientWindowState RegisterFailedAttempt(HttpContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var clientKey = BuildClientKey(context);
        var attempts = _failedAttemptsByClient.GetOrAdd(clientKey, _ => []);

        lock (attempts)
        {
            PruneOldAttempts(attempts, now);
            attempts.Add(now);

            if (attempts.Count < MaxAttempts)
                return new SecurityClientWindowState(MaxAttempts - attempts.Count, 0);

            var oldest = attempts[0];
            var retryAfter = (int)Math.Ceiling((oldest + AttemptWindow - now).TotalSeconds);
            return new SecurityClientWindowState(0, Math.Max(1, retryAfter));
        }
    }

    private static void PruneOldAttempts(List<DateTimeOffset> attempts, DateTimeOffset now)
    {
        var cutoff = now - AttemptWindow;
        attempts.RemoveAll(timestamp => timestamp < cutoff);
    }

    private static string BuildClientKey(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return ip;
    }

    private static string GenerateAccessCode(int length)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            sb.Append(alphabet[bytes[i] % alphabet.Length]);

        return sb.ToString();
    }
}

public sealed record SecurityClientWindowState(int AttemptsRemaining, int RetryAfterSeconds);

public sealed record SecurityAuthorizeResult(bool Authorized, bool TooManyAttempts, int AttemptsRemaining, int RetryAfterSeconds)
{
    public static SecurityAuthorizeResult Success(int attemptsRemaining) => new(true, false, attemptsRemaining, 0);
    public static SecurityAuthorizeResult InvalidCode(int attemptsRemaining) => new(false, false, Math.Max(0, attemptsRemaining), 0);
    public static SecurityAuthorizeResult Blocked(int retryAfterSeconds) => new(false, true, 0, Math.Max(1, retryAfterSeconds));
}
