using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.UI.Theme;

namespace VirtualWebDisplay.UI.Forms;

/// <summary>
/// Ventana de resoluciones personalizadas de Parsec VDD.
/// Permite definir hasta 5 slots (ancho × alto @ Hz) que se persisten en
/// HKLM\SOFTWARE\Parsec\vdd via <see cref="VddCustomModesStore"/>.
/// Si la app no corre como Admin, relanza el proceso con UAC solo para escribir.
/// </summary>
public sealed class CustomModesDialog : Form
{
    private const int WmNclButtonDown = 0xA1;
    private const int HtCaption       = 0x2;
    private const int Slots           = VddCustomModesStore.MaxSlots;

    // Límites según especificación
    private const int MinResolution = 300;
    private const int MaxResolution = 4200;
    private const int MinHz         = 24;
    private const int MaxHz         = 244;

    private readonly Panel  _titleBarPanel;
    private readonly Label  _titleLabel;
    private readonly Button _closeButton;
    private readonly Label  _infoLabel;

    private readonly (TextBox Width, TextBox Height, TextBox Hz)[] _slots;

    private readonly Button _resetButton;
    private readonly Button _saveButton;

    private readonly string _currentWindowTheme;

    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern int  SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    public CustomModesDialog(string windowTheme)
    {
        _currentWindowTheme = windowTheme;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition   = FormStartPosition.CenterParent;
        ClientSize      = new Size(460, 356);
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;

        var uiFont = TryCreateUiFont();
        if (uiFont is not null) Font = uiFont;

        // ── Barra de título ─────────────────────────────────────────────
        _titleBarPanel = new Panel
        {
            Left   = 0, Top = 0,
            Width  = ClientSize.Width, Height = 46,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        _titleBarPanel.MouseDown += TitleBar_MouseDown;

        _titleLabel = new Label
        {
            AutoSize = true, Left = 14, Top = 14,
            Font     = new Font(Font, FontStyle.Bold),
            Text     = AppText.Get("CustomModes_Title"),
        };
        _titleLabel.MouseDown += TitleBar_MouseDown;

        _closeButton = new Button
        {
            Width = 36, Height = 30,
            Left  = ClientSize.Width - 42, Top = 8,
            Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Text      = "X",
            Font      = new Font(Font.FontFamily, 9, FontStyle.Bold),
            TabStop   = false,
        };
        _closeButton.FlatAppearance.BorderSize = 1;
        _closeButton.Click += (_, _) => Close();

        _titleBarPanel.Controls.AddRange([_titleLabel, _closeButton]);

        // ── Info ─────────────────────────────────────────────────────────
        _infoLabel = new Label
        {
            Left     = 20, Top = 52,
            Width    = ClientSize.Width - 40, Height = 32,
            Text     = AppText.Get("CustomModes_Info"),
        };

        // ── Cabecera de columnas ─────────────────────────────────────────
        var headerSlot = new Label { Text = "",      Left = 20,  Top = 88, Width = 50,  Height = 20, Font = new Font(Font, FontStyle.Bold) };
        var headerW    = new Label { Text = "W",     Left = 78,  Top = 88, Width = 72,  Height = 20, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font, FontStyle.Bold) };
        var headerH    = new Label { Text = "H",     Left = 174, Top = 88, Width = 72,  Height = 20, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font, FontStyle.Bold) };
        var headerHz   = new Label { Text = "Hz",    Left = 270, Top = 88, Width = 56,  Height = 20, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font, FontStyle.Bold) };
        Controls.AddRange([headerSlot, headerW, headerH, headerHz]);

        // ── Slots ─────────────────────────────────────────────────────────
        _slots = new (TextBox, TextBox, TextBox)[Slots];
        const int startY = 112;
        const int rowH   = 34;

        for (var i = 0; i < Slots; i++)
        {
            var y = startY + i * rowH;

            var slotLabel = new Label
            {
                Text = $"#{i + 1}", Left = 20, Top = y, Width = 50, Height = 26,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var wBox = CreateNumericTextBox(78, y);
            var xLbl = new Label { Text = "×", Left = 155, Top = y, Width = 14, Height = 26, TextAlign = ContentAlignment.MiddleCenter };
            var hBox = CreateNumericTextBox(174, y);
            var atLbl = new Label { Text = "@", Left = 251, Top = y, Width = 14, Height = 26, TextAlign = ContentAlignment.MiddleCenter };
            var hzBox = CreateNumericTextBox(270, y, width: 56);

            _slots[i] = (wBox, hBox, hzBox);
            Controls.AddRange([slotLabel, wBox, xLbl, hBox, atLbl, hzBox]);
        }

        // ── Botones ───────────────────────────────────────────────────────
        _resetButton = new Button
        {
            Left   = ClientSize.Width - 220, Top = ClientSize.Height - 40,
            Width  = 96, Height = 28,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Text   = AppText.Get("CustomModes_Reset"),
        };
        _resetButton.Click += (_, _) => ResetSlots();

        _saveButton = new Button
        {
            Left   = ClientSize.Width - 114, Top = ClientSize.Height - 40,
            Width  = 94, Height = 28,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Text   = AppText.Get("CustomModes_Save"),
        };
        _saveButton.Click += SaveButton_Click;

        // ── Advertencia driver ────────────────────────────────────────────
        var warningTop = ClientSize.Height - 78;
        var warningPanel = new Panel
        {
            Left      = 16, Top = warningTop,
            Width     = ClientSize.Width - 32, Height = 28,
            BackColor = Color.FromArgb(255, 251, 180),
            Anchor    = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };
        var warningEmoji = new Label
        {
            Text      = "⚠",
            Left      = 6, Top = 0,
            Width     = 22, Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(120, 80, 0),
            Font      = new Font(Font.FontFamily, 11, FontStyle.Regular),
        };
        var warningText = new Label
        {
            Text      = AppText.Get("CustomModes_DriverWarning"),
            Left      = 30, Top = 0,
            Width     = warningPanel.Width - 36, Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(100, 60, 0),
        };
        warningPanel.Controls.AddRange([warningEmoji, warningText]);

        Controls.AddRange([_titleBarPanel, _infoLabel, warningPanel, _resetButton, _saveButton]);

        AcceptButton = _saveButton;
        CancelButton = _closeButton;

        Load += OnLoad;
        ApplyTheme();
    }

    // ── Carga ──────────────────────────────────────────────────────────────

    private void OnLoad(object? sender, EventArgs e)
    {
        var modes = VddCustomModesStore.Read();
        for (var i = 0; i < Slots; i++)
        {
            if (i < modes.Count)
            {
                _slots[i].Width.Text  = $"{modes[i].Width}";
                _slots[i].Height.Text = $"{modes[i].Height}";
                _slots[i].Hz.Text     = $"{modes[i].Hz}";
            }
        }
    }

    // ── Resetear ───────────────────────────────────────────────────────────

    private void ResetSlots()
    {
        foreach (var (w, h, hz) in _slots)
        {
            w.Text  = string.Empty;
            h.Text  = string.Empty;
            hz.Text = string.Empty;
        }
    }

    // ── Guardar ────────────────────────────────────────────────────────────

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        var modes = new List<VddCustomModesStore.CustomMode>();

        for (var i = 0; i < Slots; i++)
        {
            var wText  = _slots[i].Width.Text.Trim();
            var hText  = _slots[i].Height.Text.Trim();
            var hzText = _slots[i].Hz.Text.Trim();

            // Slot totalmente vacío → se omite (se borrará del registro)
            if (wText == string.Empty && hText == string.Empty && hzText == string.Empty)
                continue;

            // Slot parcialmente relleno → error
            if (!int.TryParse(wText, out var w) || !int.TryParse(hText, out var h) || !int.TryParse(hzText, out var hz))
            {
                ShowSlotError(i + 1);
                return;
            }

            if (w < MinResolution || w > MaxResolution || h < MinResolution || h > MaxResolution)
            {
                MessageBox.Show(
                    AppText.Format("CustomModes_InvalidResolution", i + 1, MinResolution, MaxResolution),
                    AppText.Get("Common_AppName"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (hz < MinHz || hz > MaxHz)
            {
                MessageBox.Show(
                    AppText.Format("CustomModes_InvalidHz", i + 1, MinHz, MaxHz),
                    AppText.Get("Common_AppName"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            modes.Add(new VddCustomModesStore.CustomMode(w, h, hz));
        }

        if (VddCustomModesStore.IsAdmin())
        {
            CommitModes(modes);
        }
        else
        {
            // Relanzar como Admin solo para escribir al registro
            var modesArg = string.Join(";", modes.Select(m => $"{m.Width}x{m.Height}@{m.Hz}"));
            try
            {
                var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName!;
                var psi = new ProcessStartInfo
                {
                    FileName        = exe,
                    Arguments       = $"--set-custom-modes \"{modesArg}\"",
                    Verb            = "runas",
                    UseShellExecute = true,
                };
                Process.Start(psi);
                MessageBox.Show(
                    AppText.Get("CustomModes_Saved"),
                    AppText.Get("Common_AppName"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    AppText.Get("CustomModes_AccessDenied"),
                    AppText.Get("Common_AppName"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void CommitModes(List<VddCustomModesStore.CustomMode> modes)
    {
        try
        {
            VddCustomModesStore.Write(modes);
            MessageBox.Show(
                AppText.Get("CustomModes_Saved"),
                AppText.Get("Common_AppName"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                AppText.Format("CustomModes_WriteError", ex.Message),
                AppText.Get("Common_AppName"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void ShowSlotError(int slot)
    {
        MessageBox.Show(
            AppText.Format("CustomModes_InvalidSlot", slot),
            AppText.Get("Common_AppName"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static TextBox CreateNumericTextBox(int left, int top, int width = 72) =>
        new()
        {
            Left      = left, Top = top,
            Width     = width, Height = 26,
            MaxLength = 4,
            TextAlign = HorizontalAlignment.Center,
        };

    private void ApplyTheme()
    {
        var dark    = FormThemeApplicator.ResolveDarkMode(_currentWindowTheme);
        var palette = dark ? ThemePalette.Dark() : ThemePalette.Light();

        BackColor = palette.Background;
        ForeColor = palette.Foreground;

        _titleBarPanel.BackColor = palette.TitleBackground;
        _titleLabel.ForeColor    = palette.TitleForeground;

        FormThemeApplicator.ApplyThemeRecursive(this, palette);
        FormThemeApplicator.StyleTitleButton(_closeButton, palette);
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, WmNclButtonDown, HtCaption, 0);
        }
    }

    private static Font? TryCreateUiFont()
    {
        try { return new Font("Segoe UI Variable Text", 9F); }
        catch { return null; }
    }
}
