using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.Forms;

/// <summary>
/// Diálogo para mostrar información de instalación de dependencias (ej: Parsec VDD).
/// </summary>
public static class InstallDialog
{
    public static void Show(string title, string message, string installUrl)
    {
        using var done = new ManualResetEventSlim(false);
        Exception? error = null;

        var staThread = new Thread(() =>
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using var form = new Form
                {
                    Text = title,
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ShowInTaskbar = true,
                    ClientSize = new Size(620, 252),
                };

                var messageLabel = new Label
                {
                    AutoSize = false,
                    Left = 20,
                    Top = 18,
                    Width = 580,
                    Height = 126,
                    Text = message,
                };

                var urlLabel = new Label
                {
                    AutoSize = true,
                    Left = 20,
                    Top = 148,
                    Text = AppText.Get("InstallDialog_OfficialInstaller"),
                };

                var urlBox = new TextBox
                {
                    Left = 20,
                    Top = 170,
                    Width = 460,
                    ReadOnly = true,
                    Text = installUrl,
                };

                var openButton = new Button
                {
                    Left = 490,
                    Top = 168,
                    Width = 110,
                    Height = 28,
                    Text = AppText.Get("InstallDialog_OpenDownload"),
                };

                var copyButton = new Button
                {
                    Left = 374,
                    Top = 208,
                    Width = 110,
                    Height = 28,
                    Text = AppText.Get("InstallDialog_CopyUrl"),
                };

                var okButton = new Button
                {
                    Left = 490,
                    Top = 208,
                    Width = 110,
                    Height = 28,
                    Text = AppText.Get("InstallDialog_Close"),
                    DialogResult = DialogResult.OK,
                };

                openButton.Click += (_, _) => Process.Start(new ProcessStartInfo(installUrl) { UseShellExecute = true });
                copyButton.Click += (_, _) =>
                {
                    Clipboard.SetText(installUrl);
                    urlBox.Focus();
                    urlBox.SelectAll();
                    copyButton.Text = AppText.Get("InstallDialog_Copied");
                };

                form.Controls.AddRange([messageLabel, urlLabel, urlBox, openButton, copyButton, okButton]);
                form.AcceptButton = okButton;
                form.CancelButton = okButton;
                form.Shown += (_, _) =>
                {
                    urlBox.Focus();
                    urlBox.SelectAll();
                };

                form.ShowDialog();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                done.Set();
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Start();
        done.Wait();

        if (error is not null)
        {
            MessageBox.Show(
                AppText.Format("InstallDialog_FallbackMessageWithUrl", message, installUrl),
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
