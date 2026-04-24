using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

public sealed class VirtualDisplayTrayController : IDisposable
{
    private readonly VirtualScreenConfig _config;
    private readonly VirtualScreenSettingsStore _settingsStore;
    private readonly Thread _uiThread;
    private readonly ManualResetEventSlim _ready = new(false);

    private ApplicationContext? _context;
    private Control? _invoker;
    private NotifyIcon? _notifyIcon;
    private Func<VirtualScreenConfig, (bool ok, string message)>? _applyConfiguration;
    private Action? _exitRequested;
    private string? _streamUrl;
    private bool _disposed;

    public VirtualDisplayTrayController(VirtualScreenConfig config, VirtualScreenSettingsStore settingsStore)
    {
        _config = config;
        _settingsStore = settingsStore;
        _uiThread = new Thread(RunUiThread)
        {
            IsBackground = true,
            Name = "VirtualWebDisplayTray",
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
        _ready.Wait();
    }

    public bool ShowStartupConfiguration()
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        PostToUi(() =>
        {
            using var form = new ResolutionConfigurationForm(_config, isInitialStartup: true);
            var result = form.ShowDialog();
            if (result == DialogResult.OK)
            {
                ApplySelection(form.Selection);
                completion.TrySetResult(true);
                return;
            }

            completion.TrySetResult(false);
            _context?.ExitThread();
        });

        return completion.Task.GetAwaiter().GetResult();
    }

    public void ConfigureRuntimeActions(Func<VirtualScreenConfig, (bool ok, string message)> applyConfiguration, Action exitRequested, string streamUrl)
    {
        _applyConfiguration = applyConfiguration;
        _exitRequested = exitRequested;
        _streamUrl = streamUrl;
        UpdateStatus($"Listo en {streamUrl}");
    }

    public void UpdateStatus(string status)
    {
        PostToUi(() =>
        {
            if (_notifyIcon is null)
                return;

            _notifyIcon.Text = TrimTrayText($"VirtualWebDisplay - {status}");
        });
    }

    private void RunUiThread()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        _context = new ApplicationContext();
        _invoker = new Control();
        _invoker.CreateControl();

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = TrimTrayText("VirtualWebDisplay"),
            Visible = true,
            ContextMenuStrip = BuildContextMenu(),
        };

        _notifyIcon.DoubleClick += (_, _) => ShowConfigurationDialog();
        _ready.Set();

        Application.Run(_context);

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _invoker.Dispose();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Configuración...", null, (_, _) => ShowConfigurationDialog());
        menu.Items.Add("Abrir transmisión", null, (_, _) => OpenStreamUrl());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => ExitApplication());
        return menu;
    }

    private void ShowConfigurationDialog()
    {
        using var form = new ResolutionConfigurationForm(_config, isInitialStartup: false);
        if (form.ShowDialog() != DialogResult.OK)
            return;

        ApplySelection(form.Selection);

        var result = _applyConfiguration is null
            ? (ok: true, message: $"Configuración guardada: {_config.Width}×{_config.Height}.")
            : _applyConfiguration(_config);

        UpdateStatus(result.ok
            ? $"{VirtualDisplayProfiles.GetDisplayName(_config.Profile)} {_config.Width}×{_config.Height}"
            : "Error al aplicar resolución");

        if (_notifyIcon is not null)
        {
            _notifyIcon.BalloonTipTitle = result.ok ? "VirtualWebDisplay" : "VirtualWebDisplay — Error";
            _notifyIcon.BalloonTipText = result.message;
            _notifyIcon.ShowBalloonTip(3000);
        }
    }

    private void ApplySelection(ResolutionSelection selection)
    {
        _config.Profile = selection.ProfileId;
        _config.Landscape = selection.Landscape;
        _config.CustomWidth = selection.CustomWidth;
        _config.CustomHeight = selection.CustomHeight;
        VirtualDisplayProfiles.EnsureValidSelection(_config);
        _settingsStore.Save(_config);
    }

    private void OpenStreamUrl()
    {
        if (string.IsNullOrWhiteSpace(_streamUrl))
            return;

        Process.Start(new ProcessStartInfo(_streamUrl) { UseShellExecute = true });
    }

    private void ExitApplication()
    {
        _exitRequested?.Invoke();
        _context?.ExitThread();
    }

    private void PostToUi(Action action)
    {
        if (_invoker is null || _invoker.IsDisposed)
            return;

        _invoker.BeginInvoke(action);
    }

    private static string TrimTrayText(string text) =>
        text.Length <= 63 ? text : text[..63];

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        PostToUi(() => _context?.ExitThread());
        if (!_uiThread.Join(1500))
            _uiThread.Interrupt();
        _ready.Dispose();
    }

    private sealed class ResolutionConfigurationForm : Form
    {
        private readonly ComboBox _profileCombo;
        private readonly NumericUpDown _widthInput;
        private readonly NumericUpDown _heightInput;
        private readonly CheckBox _landscapeCheckBox;
        private readonly Label _previewLabel;

        public ResolutionSelection Selection { get; private set; }

        public ResolutionConfigurationForm(VirtualScreenConfig config, bool isInitialStartup)
        {
            Text = isInitialStartup ? "VirtualWebDisplay — Resolución virtual" : "VirtualWebDisplay — Configuración";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 255);

            var descriptionLabel = new Label
            {
                AutoSize = false,
                Left = 18,
                Top = 16,
                Width = 384,
                Height = 42,
                Text = isInitialStartup
                    ? "Seleccioná el perfil para la pantalla virtual antes de crear el monitor extendido. La aplicación seguirá disponible en la bandeja del sistema."
                    : "Ajustá la resolución del monitor virtual. Los cambios se guardan y se aplican sobre la segunda pantalla creada por la app.",
            };

            var profileLabel = new Label
            {
                AutoSize = true,
                Left = 18,
                Top = 72,
                Text = "Perfil:",
            };

            _profileCombo = new ComboBox
            {
                Left = 18,
                Top = 92,
                Width = 384,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            _profileCombo.Items.AddRange(VirtualDisplayProfiles.All.Select(profile => new ProfileItem(profile.Id, profile.DisplayName)).ToArray());

            _landscapeCheckBox = new CheckBox
            {
                Left = 18,
                Top = 128,
                AutoSize = true,
                Text = "Landscape",
            };

            var widthLabel = new Label
            {
                AutoSize = true,
                Left = 18,
                Top = 160,
                Text = "Ancho:",
            };

            _widthInput = new NumericUpDown
            {
                Left = 18,
                Top = 180,
                Width = 120,
                Minimum = 100,
                Maximum = 5000,
            };

            var heightLabel = new Label
            {
                AutoSize = true,
                Left = 154,
                Top = 160,
                Text = "Alto:",
            };

            _heightInput = new NumericUpDown
            {
                Left = 154,
                Top = 180,
                Width = 120,
                Minimum = 100,
                Maximum = 5000,
            };

            _previewLabel = new Label
            {
                AutoSize = false,
                Left = 18,
                Top = 214,
                Width = 260,
                Height = 24,
            };

            var acceptButton = new Button
            {
                Left = 226,
                Top = 214,
                Width = 84,
                Height = 28,
                Text = isInitialStartup ? "Iniciar" : "Aplicar",
                DialogResult = DialogResult.OK,
            };

            var cancelButton = new Button
            {
                Left = 318,
                Top = 214,
                Width = 84,
                Height = 28,
                Text = isInitialStartup ? "Salir" : "Cerrar",
                DialogResult = DialogResult.Cancel,
            };

            Controls.AddRange(
            [
                descriptionLabel,
                profileLabel,
                _profileCombo,
                _landscapeCheckBox,
                widthLabel,
                _widthInput,
                heightLabel,
                _heightInput,
                _previewLabel,
                acceptButton,
                cancelButton,
            ]);

            AcceptButton = acceptButton;
            CancelButton = cancelButton;

            var normalizedProfileId = VirtualDisplayProfiles.NormalizeProfileId(config.Profile);
            var selectedItem = _profileCombo.Items.Cast<ProfileItem>().FirstOrDefault(item => item.ProfileId == normalizedProfileId)
                ?? _profileCombo.Items.Cast<ProfileItem>().First(item => item.ProfileId == VirtualDisplayProfiles.Custom);
            _profileCombo.SelectedItem = selectedItem;

            _landscapeCheckBox.Checked = config.Landscape;
            _widthInput.Value = Math.Max(_widthInput.Minimum, Math.Min(_widthInput.Maximum, config.CustomWidth));
            _heightInput.Value = Math.Max(_heightInput.Minimum, Math.Min(_heightInput.Maximum, config.CustomHeight));

            _profileCombo.SelectedIndexChanged += (_, _) => UpdatePreview();
            _landscapeCheckBox.CheckedChanged += (_, _) => UpdatePreview();
            _widthInput.ValueChanged += (_, _) => UpdatePreview();
            _heightInput.ValueChanged += (_, _) => UpdatePreview();

            FormClosing += (_, args) =>
            {
                if (DialogResult != DialogResult.OK)
                    return;

                var profileId = ((ProfileItem)_profileCombo.SelectedItem!).ProfileId;
                if (VirtualDisplayProfiles.IsCustom(profileId) && (_widthInput.Value < 100 || _heightInput.Value < 100))
                {
                    args.Cancel = true;
                    return;
                }

                Selection = new ResolutionSelection(
                    profileId,
                    _landscapeCheckBox.Checked,
                    (int)_widthInput.Value,
                    (int)_heightInput.Value);
            };

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var profileId = ((ProfileItem)_profileCombo.SelectedItem!).ProfileId;
            var isCustom = VirtualDisplayProfiles.IsCustom(profileId);
            _widthInput.Enabled = isCustom;
            _heightInput.Enabled = isCustom;

            var size = VirtualDisplayProfiles.GetEffectiveSize(
                profileId,
                _landscapeCheckBox.Checked,
                (int)_widthInput.Value,
                (int)_heightInput.Value);

            _previewLabel.Text = $"Resolución final: {size.Width}×{size.Height}";
        }

        private sealed record ProfileItem(string ProfileId, string DisplayName)
        {
            public override string ToString() => DisplayName;
        }
    }

    private sealed record ResolutionSelection(string ProfileId, bool Landscape, int CustomWidth, int CustomHeight);
}
