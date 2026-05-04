using System.Text.Json.Serialization;

namespace VirtualWebDisplay.Infrastructure.Updates;

/// <summary>
/// Represents the subset of a GitHub release API response used for update checking.
/// </summary>
internal sealed record GitHubReleaseInfo(
    [property: JsonPropertyName("tag_name")]  string TagName,
    [property: JsonPropertyName("html_url")]  string HtmlUrl,
    [property: JsonPropertyName("body")]      string Body,
    [property: JsonPropertyName("prerelease")] bool Prerelease);
