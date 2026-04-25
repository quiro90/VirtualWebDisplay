using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
    private string? _alternateStreamUrl;
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

    public void ConfigureRuntimeActions(Func<VirtualScreenConfig, (bool ok, string message)> applyConfiguration, Action exitRequested, string streamUrl, string? alternateStreamUrl)
    {
        _applyConfiguration = applyConfiguration;
        _exitRequested = exitRequested;
        _streamUrl = streamUrl;
        _alternateStreamUrl = alternateStreamUrl;
        UpdateStatus($"Listo en {streamUrl}");

        PostToUi(() =>
        {
            if (_notifyIcon is null)
                return;

            _notifyIcon.BalloonTipTitle = "VirtualWebDisplay";
            _notifyIcon.BalloonTipText = string.IsNullOrWhiteSpace(_alternateStreamUrl)
                ? $"Disponible en {_streamUrl}"
                : $"Disponible en {_streamUrl}\nTambién en {_alternateStreamUrl}";
            _notifyIcon.ShowBalloonTip(4000);
        });
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
            ? $"{VirtualDisplayProfiles.GetDisplayName(_config.Profile)} {_config.Width}×{_config.Height} · {TransmissionModeOptions.GetDisplayName(_config.TransmissionMethod)}"
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
        _config.Port = selection.Port;
        _config.TransmissionMethod = selection.TransmissionMethod;
        _config.CaptureIntervalSeconds = selection.CaptureIntervalSeconds;
        _config.JpegQuality = selection.JpegQuality;

        VirtualDisplayProfiles.EnsureValidSelection(_config);
        TransmissionModeOptions.EnsureValidSelection(_config);
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

    private static string DetectLocalIp() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                     && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString())
            .FirstOrDefault() ?? "127.0.0.1";

    private static string BuildAccessUrl(string host, int port) =>
        port == 80 ? $"http://{host}/" : $"http://{host}:{port}/";

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
        private readonly ComboBox _transmissionMethodCombo;
        private readonly NumericUpDown _captureIntervalInput;
        private readonly TrackBar _jpegQualitySlider;
        private readonly Label _jpegQualityValueLabel;
        private readonly Label _streamingPreviewLabel;
        private readonly NumericUpDown _portInput;
        private readonly Label _accessUrlLabel;
        private readonly Label _alternateAccessUrlLabel;

        public ResolutionSelection Selection { get; private set; }

        public ResolutionConfigurationForm(VirtualScreenConfig config, bool isInitialStartup)
        {
            Text = isInitialStartup ? "VirtualWebDisplay — Resolución virtual" : "VirtualWebDisplay — Configuración";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(456, 450);

            var descriptionLabel = new Label
            {
                AutoSize = false,
                Left = 18,
                Top = 16,
                Width = 420,
                Height = 46,
                Text = isInitialStartup
                    ? "Seleccioná el perfil para la pantalla virtual antes de crear el monitor extendido. La aplicación seguirá disponible en la bandeja del sistema."
                    : "Ajustá la resolución del monitor virtual y el modo de retransmisión. Los cambios se guardan y se aplican sobre la segunda pantalla creada por la app.",
            };

            var tabs = new TabControl
            {
                Left = 18,
                Top = 68,
                Width = 420,
                Height = 264,
            };

            var screenTab = new TabPage("Pantalla virtual");
            var streamingTab = new TabPage("Retransmisión");
            tabs.TabPages.Add(screenTab);
            tabs.TabPages.Add(streamingTab);

            var portLabel = new Label
            {
                AutoSize = true,
                Left = 18,
                Top = 340,
                Text = "Puerto web local:",
            };

            _portInput = new NumericUpDown
            {
                Left = 118,
                Top = 336,
                Width = 96,
                Minimum = 1,
                Maximum = 65535,
                Enabled = isInitialStartup,
            };

            _accessUrlLabel = new Label
            {
                AutoSize = false,
                Left = 18,
                Top = 368,
                Width = 420,
                Height = 18,
            };

            _alternateAccessUrlLabel = new Label
            {
                AutoSize = false,
                Left = 18,
                Top = 388,
                Width = 420,
                Height = 18,
            };

            var portHintLabel = new Label
            {
                AutoSize = false,
                Left = 224,
                Top = 338,
                Width = 214,
                Height = 30,
                Text = isInitialStartup
                    ? "Podés usar 80 para una URL corta si el puerto está libre."
                    : "El puerto se configura al iniciar la aplicación.",
            };

            var profileLabel = new Label
            {
                AutoSize = true,
                Left = 14,
                Top = 18,
                Text = "Perfil:",
            };

            _profileCombo = new ComboBox
            {
                Left = 14,
                Top = 38,
                Width = 370,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            _profileCombo.Items.AddRange(VirtualDisplayProfiles.All.Select(profile => new ProfileItem(profile.Id, profile.DisplayName)).ToArray());

            _landscapeCheckBox = new CheckBox
            {
                Left = 14,
                Top = 74,
                AutoSize = true,
                Text = "Landscape",
            };

            var widthLabel = new Label
            {
                AutoSize = true,
                Left = 14,
                Top = 106,
                Text = "Ancho:",
            };

            _widthInput = new NumericUpDown
            {
                Left = 14,
                Top = 126,
                Width = 120,
                Minimum = 100,
                Maximum = 5000,
            };

            var heightLabel = new Label
            {
                AutoSize = true,
                Left = 150,
                Top = 106,
                Text = "Alto:",
            };

            _heightInput = new NumericUpDown
            {
                Left = 150,
                Top = 126,
                Width = 120,
                Minimum = 100,
                Maximum = 5000,
            };

            _previewLabel = new Label
            {
                AutoSize = false,
                Left = 14,
                Top = 166,
                Width = 370,
                Height = 24,
            };

            screenTab.Controls.AddRange(
            [
                profileLabel,
                _profileCombo,
                _landscapeCheckBox,
                widthLabel,
                _widthInput,
                heightLabel,
                _heightInput,
                _previewLabel,
            ]);

            var transmissionMethodLabel = new Label
            {
                AutoSize = true,
                Left = 14,
                Top = 18,
                Text = "Método:",
            };

            _transmissionMethodCombo = new ComboBox
            {
                Left = 14,
                Top = 38,
                Width = 370,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            _transmissionMethodCombo.Items.AddRange(
            [
                new TransmissionMethodItem(TransmissionModeOptions.WebImage, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.WebImage)),
                new TransmissionMethodItem(TransmissionModeOptions.Rtc, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.Rtc)),
            ]);

            var captureIntervalLabel = new Label
            {
                AutoSize = true,
                Left = 14,
                Top = 76,
                Text = "Actualizar cada (segundos):",
            };

            _captureIntervalInput = new NumericUpDown
            {
                Left = 14,
                Top = 96,
                Width = 120,
                Minimum = 0.01M,
                Maximum = 60M,
                DecimalPlaces = 3,
                Increment = 0.01M,
            };

            var jpegQualityLabel = new Label
            {
                AutoSize = true,
                Left = 14,
                Top = 132,
                Text = "Calidad de imagen:",
            };

            _jpegQualitySlider = new TrackBar
            {
                Left = 14,
                Top = 152,
                Width = 280,
                Minimum = 10,
                Maximum = 100,
                TickFrequency = 10,
                SmallChange = 5,
                LargeChange = 10,
            };

            _jpegQualityValueLabel = new Label
            {
                AutoSize = true,
                Left = 304,
                Top = 158,
            };

            _streamingPreviewLabel = new Label
            {
                AutoSize = false,
                Left = 14,
                Top = 208,
                Width = 370,
                Height = 28,
            };

            streamingTab.Controls.AddRange(
            [
                transmissionMethodLabel,
                _transmissionMethodCombo,
                captureIntervalLabel,
                _captureIntervalInput,
                jpegQualityLabel,
                _jpegQualitySlider,
                _jpegQualityValueLabel,
                _streamingPreviewLabel,
            ]);

            var acceptButton = new Button
            {
                Left = 262,
                Top = 410,
                Width = 84,
                Height = 28,
                Text = isInitialStartup ? "Iniciar" : "Aplicar",
                DialogResult = DialogResult.OK,
            };

            var cancelButton = new Button
            {
                Left = 354,
                Top = 410,
                Width = 84,
                Height = 28,
                Text = isInitialStartup ? "Salir" : "Cerrar",
                DialogResult = DialogResult.Cancel,
            };

            Controls.AddRange(
            [
                descriptionLabel,
                tabs,
                portLabel,
                _portInput,
                portHintLabel,
                _accessUrlLabel,
                _alternateAccessUrlLabel,
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

            var normalizedMethod = TransmissionModeOptions.NormalizeMethod(config.TransmissionMethod);
            _transmissionMethodCombo.SelectedItem = _transmissionMethodCombo.Items
                .Cast<TransmissionMethodItem>()
                .First(item => item.Method == normalizedMethod);

            var captureInterval = (decimal)Math.Clamp(config.CaptureIntervalSeconds, (double)_captureIntervalInput.Minimum, (double)_captureIntervalInput.Maximum);
            _captureIntervalInput.Value = captureInterval;
            _jpegQualitySlider.Value = Math.Clamp(config.JpegQuality, _jpegQualitySlider.Minimum, _jpegQualitySlider.Maximum);
            _portInput.Value = Math.Max(_portInput.Minimum, Math.Min(_portInput.Maximum, config.Port));

            _profileCombo.SelectedIndexChanged += (_, _) => UpdatePreview();
            _landscapeCheckBox.CheckedChanged += (_, _) => UpdatePreview();
            _widthInput.ValueChanged += (_, _) => UpdatePreview();
            _heightInput.ValueChanged += (_, _) => UpdatePreview();
            _transmissionMethodCombo.SelectedIndexChanged += (_, _) => UpdateStreamingPreview();
            _captureIntervalInput.ValueChanged += (_, _) => UpdateStreamingPreview();
            _jpegQualitySlider.ValueChanged += (_, _) => UpdateStreamingPreview();
            _portInput.ValueChanged += (_, _) => UpdateAccessUrls();

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
                    (int)_heightInput.Value,
                    (int)_portInput.Value,
                    ((TransmissionMethodItem)_transmissionMethodCombo.SelectedItem!).Method,
                    (double)_captureIntervalInput.Value,
                    _jpegQualitySlider.Value);
            };

            UpdatePreview();
            UpdateStreamingPreview();
            UpdateAccessUrls();
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

        private void UpdateStreamingPreview()
        {
            var method = ((TransmissionMethodItem)_transmissionMethodCombo.SelectedItem!).Method;
            var isWebImage = TransmissionModeOptions.IsWebImage(method);

            _jpegQualityValueLabel.Text = $"{_jpegQualitySlider.Value}%";
            _streamingPreviewLabel.Text = isWebImage
                ? $"Web image: refresco cada {(double)_captureIntervalInput.Value:0.###} s, JPEG {_jpegQualitySlider.Value}%."
                : $"WebRTC: refresco objetivo cada {(double)_captureIntervalInput.Value:0.###} s, JPEG {_jpegQualitySlider.Value}% para tablets.";
        }

        private void UpdateAccessUrls()
        {
            var port = (int)_portInput.Value;
            var hostUrl = BuildAccessUrl(Dns.GetHostName(), port);
            var ipUrl = BuildAccessUrl(DetectLocalIp(), port);

            _accessUrlLabel.Text = $"URL local: {hostUrl}";
            _alternateAccessUrlLabel.Text = string.Equals(hostUrl, ipUrl, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : $"Alternativa por IP: {ipUrl}";
        }

        private sealed record ProfileItem(string ProfileId, string DisplayName)
        {
            public override string ToString() => DisplayName;
        }

        private sealed record TransmissionMethodItem(string Method, string DisplayName)
        {
            public override string ToString() => DisplayName;
        }
    }

    private sealed record ResolutionSelection(string ProfileId, bool Landscape, int CustomWidth, int CustomHeight, int Port, string TransmissionMethod, double CaptureIntervalSeconds, int JpegQuality);
}
