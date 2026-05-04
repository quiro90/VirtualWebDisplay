using System.Drawing;
using System.Windows.Forms;
using VirtualWebDisplay.Infrastructure.Updates;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.UI.Helpers;

namespace VirtualWebDisplay.UI.Forms;

/// <summary>
/// Displays a themed dialog informing the user that a new release is available.
/// Shows the current/remote version, the release changelog, and offers a link to download.
/// </summary>
internal static class UpdateAvailableDialog
{
    public static void Show(
        IWin32Window? owner,
        GitHubReleaseInfo release,
        Color backgroundColor,
        Color foregroundColor,
        Color panelColor,
        Color borderColor,
        Color linkColor,
        Color linkActiveColor)
    {
        using var dialog = new Form
        {
            Text                = AppText.Get("Update_Title"),
            StartPosition       = owner is not null ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen,
            FormBorderStyle     = FormBorderStyle.FixedDialog,
            MaximizeBox         = false,
            MinimizeBox         = false,
            ShowInTaskbar       = true,
            ClientSize          = new Size(520, 370),
            BackColor           = backgroundColor,
            ForeColor           = foregroundColor,
        };

        var accentColor = Color.FromArgb(78, 156, 255);

        // ── Title ────────────────────────────────────────────────────────────
        var titleLabel = new Label
        {
            AutoSize  = true,
            Left      = 18,
            Top       = 16,
            Font      = new Font(dialog.Font.FontFamily, 13, FontStyle.Bold),
            Text      = AppText.Get("Update_Title"),
            ForeColor = accentColor,
            BackColor = Color.Transparent,
        };

        // ── Version info ─────────────────────────────────────────────────────
        var versionLabel = new Label
        {
            AutoSize  = false,
            Left      = 18,
            Top       = 52,
            Width     = 484,
            Height    = 20,
            Text      = AppText.Format(
                            "Update_VersionInfo",
                            Web.HtmlTemplates.TemplateVersionHelper.AppVersion,
                            release.TagName.TrimStart('v')),
            ForeColor = foregroundColor,
            BackColor = Color.Transparent,
        };

        // ── Divider ──────────────────────────────────────────────────────────
        var topDivider = new Panel
        {
            Left      = 18,
            Top       = 80,
            Width     = 484,
            Height    = 1,
            BackColor = borderColor,
        };

        // ── Changelog label ──────────────────────────────────────────────────
        var changelogLabel = new Label
        {
            AutoSize  = true,
            Left      = 18,
            Top       = 90,
            Text      = AppText.Get("Update_ChangelogLabel"),
            ForeColor = foregroundColor,
            BackColor = Color.Transparent,
        };

        // ── Changelog text box ───────────────────────────────────────────────
        var changelogBox = new RichTextBox
        {
            Left        = 18,
            Top         = 112,
            Width       = 484,
            Height      = 180,
            ReadOnly    = true,
            BackColor   = panelColor,
            ForeColor   = foregroundColor,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            Text        = string.IsNullOrWhiteSpace(release.Body)
                              ? AppText.Get("Update_NoChangelog")
                              : release.Body.Replace("\r\n", "\n"),
            Font        = new Font(dialog.Font.FontFamily, 9),
        };

        // ── Bottom divider ───────────────────────────────────────────────────
        var bottomDivider = new Panel
        {
            Left      = 18,
            Top       = 302,
            Width     = 484,
            Height    = 1,
            BackColor = borderColor,
        };

        // ── Download button ──────────────────────────────────────────────────
        var downloadButton = new Button
        {
            Left      = 18,
            Top       = 316,
            Width     = 160,
            Height    = 30,
            Text      = AppText.Get("Update_DownloadButton"),
            BackColor = accentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font(dialog.Font.FontFamily, 9, FontStyle.Bold),
        };
        downloadButton.FlatAppearance.BorderSize  = 0;
        downloadButton.Click += (_, _) => ShellHelper.OpenUrl(release.HtmlUrl);

        // ── Close button ─────────────────────────────────────────────────────
        var closeButton = new Button
        {
            Left         = 402,
            Top          = 316,
            Width        = 100,
            Height       = 30,
            Text         = AppText.Get("Update_CloseButton"),
            DialogResult = DialogResult.Cancel,
            BackColor    = panelColor,
            ForeColor    = foregroundColor,
            FlatStyle    = FlatStyle.Flat,
        };
        closeButton.FlatAppearance.BorderColor = borderColor;
        closeButton.FlatAppearance.BorderSize  = 1;

        dialog.Controls.AddRange([
            titleLabel, versionLabel,
            topDivider,
            changelogLabel, changelogBox,
            bottomDivider,
            downloadButton, closeButton,
        ]);

        dialog.AcceptButton = closeButton;
        dialog.CancelButton = closeButton;

        dialog.ShowDialog(owner);
    }
}
