using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.Forms;

internal static class AboutDialog
{
    private const string LinkedinUrl = "https://www.linkedin.com/in/juan-quiroga-90/";

    public static void Show(IWin32Window owner, Color backgroundColor, Color foregroundColor, Color panelColor, Color borderColor)
    {
        using var dialog = new Form
        {
            Text = AppText.Get("About_Title"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(500, 285),
            BackColor = backgroundColor,
            ForeColor = foregroundColor,
        };

        var accentColor = Color.FromArgb(78, 156, 255);

        var titleLabel = new Label
        {
            AutoSize = true,
            Left = 18,
            Top = 16,
            Font = new Font(dialog.Font.FontFamily, 16, FontStyle.Bold),
            Text = AppText.Get("Common_AppDisplayName"),
            ForeColor = foregroundColor,
            BackColor = Color.Transparent,
        };

        var subtitleLabel = new Label
        {
            Left = 20,
            Top = 48,
            Width = 460,
            Height = 34,
            Text = AppText.Get("About_Subtitle"),
            ForeColor = Color.FromArgb(
                Math.Min(foregroundColor.R + 20, 255),
                Math.Min(foregroundColor.G + 20, 255),
                Math.Min(foregroundColor.B + 20, 255)),
            BackColor = Color.Transparent,
        };

        var topDivider = new Panel
        {
            Left = 18,
            Top = 88,
            Width = 460,
            Height = 2,
            BackColor = borderColor,
        };

        var authorLabel = new Label
        {
            AutoSize = true,
            Left = 18,
            Top = 104,
            Text = AppText.Get("About_AuthorPrefix"),
            ForeColor = foregroundColor,
            BackColor = Color.Transparent,
        };

        var linkedinLink = new LinkLabel
        {
            AutoSize = true,
            Left = 18,
            Top = 128,
            Text = AppText.Get("About_LinkedinHandle"),
            LinkColor = accentColor,
            ActiveLinkColor = Color.FromArgb(120, 185, 255),
            VisitedLinkColor = accentColor,
            BackColor = Color.Transparent,
        };
        linkedinLink.LinkClicked += (_, _) => OpenLinkedin();

        var messagePanel = new Panel
        {
            Left = 18,
            Top = 162,
            Width = 460,
            Height = 82,
            BackColor = panelColor,
            BorderStyle = BorderStyle.FixedSingle,
        };

        var messageLabel = new Label
        {
            Left = 12,
            Top = 12,
            Width = 434,
            Height = 54,
            Text = AppText.Get("About_Message"),
            ForeColor = foregroundColor,
            BackColor = Color.Transparent,
        };

        var footerLabel = new Label
        {
            Left = 18,
            Top = 252,
            Width = 300,
            Height = 20,
            Text = AppText.Get("About_Footer"),
            ForeColor = Color.FromArgb(
                Math.Min(foregroundColor.R + 10, 255),
                Math.Min(foregroundColor.G + 10, 255),
                Math.Min(foregroundColor.B + 10, 255)),
            BackColor = Color.Transparent,
        };

        var closeButton = new Button
        {
            Left = 378,
            Top = 248,
            Width = 100,
            Height = 28,
            Text = AppText.Get("InstallDialog_Close"),
            DialogResult = DialogResult.OK,
            BackColor = panelColor,
            ForeColor = foregroundColor,
            FlatStyle = FlatStyle.Flat,
        };
        closeButton.FlatAppearance.BorderColor = borderColor;
        closeButton.FlatAppearance.BorderSize = 1;

        messagePanel.Controls.Add(messageLabel);
        dialog.Controls.AddRange([titleLabel, subtitleLabel, topDivider, authorLabel, linkedinLink, messagePanel, footerLabel, closeButton]);
        dialog.AcceptButton = closeButton;
        dialog.CancelButton = closeButton;

        dialog.ShowDialog(owner);
    }

    private static void OpenLinkedin()
    {
        try
        {
            Process.Start(new ProcessStartInfo(LinkedinUrl) { UseShellExecute = true });
        }
        catch
        {
        }
    }
}