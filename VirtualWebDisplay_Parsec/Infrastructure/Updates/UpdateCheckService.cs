using System.Net.Http;
using System.Text.Json;
using VirtualWebDisplay.Web.HtmlTemplates;

namespace VirtualWebDisplay.Infrastructure.Updates;

/// <summary>
/// Checks the GitHub releases API for a newer version of the application.
/// All errors are swallowed — the update check never blocks or crashes the app.
/// </summary>
internal static class UpdateCheckService
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/quiro90/VirtualWebDisplay/releases/latest";

    private static readonly HttpClient _httpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
        client.DefaultRequestHeaders.Add("User-Agent", "VirtualWebDisplay");
        return client;
    }

    /// <summary>
    /// Fetches the latest GitHub release and returns it if a newer version is available.
    /// Returns <see langword="null"/> if the app is up-to-date, or if any error occurs.
    /// </summary>
    public static async Task<GitHubReleaseInfo?> CheckForUpdateAsync()
    {
        try
        {
            var json = await _httpClient.GetStringAsync(LatestReleaseUrl);

            var release = JsonSerializer.Deserialize<GitHubReleaseInfo>(json);
            if (release is null || release.Prerelease)
                return null;

            // tag_name is expected as "v1.2.3"; strip the leading 'v' before parsing.
            var remoteTag = release.TagName.TrimStart('v');
            if (!System.Version.TryParse(remoteTag, out var remoteVersion))
                return null;

            if (!System.Version.TryParse(TemplateVersionHelper.AppVersion, out var localVersion))
                return null;

            // Only notify when the remote version is strictly greater.
            return remoteVersion > localVersion ? release : null;
        }
        catch
        {
            // Fail silently: no internet, timeout, unexpected API shape, etc.
            return null;
        }
    }
}
