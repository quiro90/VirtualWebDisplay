using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

public sealed class VirtualDisplayTrayController : IDisposable
{
    private readonly VirtualWebDisplaySettings _settings;
    private readonly VirtualScreenSettingsStore _settingsStore;
    private readonly string _localIp;
    private readonly Thread _uiThread;
    private readonly ManualResetEventSlim _ready = new(false);

    private ApplicationContext? _context;
    private Control? _invoker;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private Action? _exitRequested;
    private Action? _restartRequested;
    private IReadOnlyList<ScreenRuntimeContext> _screenRuntimes = [];
    private bool _disposed;
    private Icon? _appIcon;
    private ResolutionConfigurationForm? _startupForm;

    private static Icon LoadAppIcon()
    {
        var stream = typeof(VirtualDisplayTrayController).Assembly
            .GetManifestResourceStream("VirtualWebDisplay.app.ico");
        return stream is not null ? new Icon(stream) : SystemIcons.Application;
    }

    public VirtualDisplayTrayController(VirtualWebDisplaySettings settings, VirtualScreenSettingsStore settingsStore, string localIp)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        _localIp = localIp;
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
            _startupForm = CreateConfigurationForm(isInitialStartup: true, hasStarted: false);

            _startupForm.FormClosed += (_, _) =>
            {
                if (_startupForm.WasStarted)
                {
                    _startupForm = null;
                }
                else
                {
                    completion.TrySetResult(false);
                    _context?.ExitThread();
                }
            };

            _startupForm.ConfigurationApplied += _ =>
            {
                if (!_startupForm.WasStarted)
                    completion.TrySetResult(true);
            };

            _startupForm.Show();
        });

        return completion.Task.GetAwaiter().GetResult();
    }

    public void ConfigureRuntimeActions(Action exitRequested, Action restartRequested, IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
    {
        _exitRequested = exitRequested;
        _restartRequested = restartRequested;
        _screenRuntimes = screenRuntimes;

        PostToUi(() =>
        {
            if (_notifyIcon is null)
                return;

            _contextMenu?.Dispose();
            _contextMenu = BuildContextMenu();
            _notifyIcon.ContextMenuStrip = _contextMenu;

            // Notificar al formulario de inicio que la aplicación ya se inició
            _startupForm?.NotifyStartupCompleted();
        });

        var summary = string.Join(" | ", _screenRuntimes.Select(runtime => $"{runtime.DisplayName}: {runtime.HostUrl}"));
        UpdateStatus(summary);

        PostToUi(() =>
        {
            if (_notifyIcon is null)
                return;

            _notifyIcon.BalloonTipTitle = "VirtualWebDisplay";
            _notifyIcon.BalloonTipText = string.Join("\n", _screenRuntimes.Select(runtime =>
                string.Equals(runtime.HostUrl, runtime.IpUrl, StringComparison.OrdinalIgnoreCase)
                    ? $"{runtime.DisplayName}: {runtime.HostUrl}"
                    : $"{runtime.DisplayName}: {runtime.HostUrl} | {runtime.IpUrl}"));
            _notifyIcon.ShowBalloonTip(5000);
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

        _appIcon = LoadAppIcon();
        _contextMenu = BuildContextMenu();
        _notifyIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = TrimTrayText("VirtualWebDisplay"),
            Visible = true,
            ContextMenuStrip = _contextMenu,
        };

        _notifyIcon.DoubleClick += (_, _) => ShowConfigurationDialog();
        _ready.Set();

        Application.Run(_context);

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu?.Dispose();
        _appIcon?.Dispose();
        _invoker.Dispose();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Configuración...", null, (_, _) => ShowConfigurationDialog());

        if (_screenRuntimes.Count > 0)
        {
            foreach (var runtime in _screenRuntimes)
                menu.Items.Add($"Abrir {runtime.DisplayName}", null, (_, _) => OpenStreamUrl(runtime.HostUrl));

            menu.Items.Add(new ToolStripSeparator());
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Reiniciar", null, (_, _) => RestartApplication());
        menu.Items.Add("Salir", null, (_, _) => ExitApplication());
        return menu;
    }

    private void ShowConfigurationDialog()
    {
        if (_startupForm is not null && !_startupForm.IsDisposed)
        {
            _startupForm.BringToFront();
            _startupForm.Activate();
            return;
        }

        var hasStarted = _screenRuntimes.Count > 0;
        using var form = CreateConfigurationForm(isInitialStartup: false, hasStarted);

        var dialogResult = form.ShowDialog();

        if (dialogResult == DialogResult.OK && !hasStarted)
        {
            _notifyIcon?.ShowBalloonTip(4000, "VirtualWebDisplay", "Configuración guardada. Usá 'Reiniciar' desde el ícono de bandeja para aplicar los nuevos valores.", ToolTipIcon.Info);
        }
    }

    private ResolutionConfigurationForm CreateConfigurationForm(bool isInitialStartup, bool hasStarted)
    {
        var form = new ResolutionConfigurationForm(_settings, isInitialStartup, _localIp, hasStarted);

        form.ConfigurationApplied += ApplySelection;
        form.RestartRequested += RestartApplication;

        return form;
    }

    private void ApplySelection(VirtualWebDisplaySettings selection)
    {
        selection.EnsureValid();

        selection.Screen1.CopyTo(_settings.Screen1);
        selection.Screen2.CopyTo(_settings.Screen2);
        _settings.EnsureValid();
        _settingsStore.Save(_settings);
    }

    private static void OpenStreamUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void ExitApplication()
    {
        _exitRequested?.Invoke();
        _context?.ExitThread();
    }

    private void RestartApplication()
    {
        _restartRequested?.Invoke();
        _context?.ExitThread();
    }

    private void PostToUi(Action action)
    {
        if (_invoker is null || _invoker.IsDisposed || !_invoker.IsHandleCreated)
            return;

        try
        {
            _invoker.BeginInvoke(action);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // El control fue destruido entre el guard y el BeginInvoke (race condition al cerrar).
        }
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
        private readonly ScreenTabControls _screen1Controls;
        private readonly ScreenTabControls _screen2Controls;
        private readonly Button _acceptButton;
        private readonly bool _isInitialStartup;
        private bool _wasStarted;

        public VirtualWebDisplaySettings Selection { get; private set; } = new();
        public bool WasStarted => _wasStarted;

        public event Action<VirtualWebDisplaySettings>? ConfigurationApplied;
        public event Action? RestartRequested;

        private string AcceptButtonText => _wasStarted ? "Reiniciar" : (_isInitialStartup ? "Iniciar" : "Guardar");

        public ResolutionConfigurationForm(VirtualWebDisplaySettings settings, bool isInitialStartup, string localIp, bool hasStarted = false)
        {
            _isInitialStartup = isInitialStartup;
            _wasStarted = hasStarted;
            Text = isInitialStartup ? "VirtualWebDisplay & Configuración de pantallas" : "VirtualWebDisplay & Configuración";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = isInitialStartup;
            ClientSize = new Size(520, 420);

            var workingCopy = new VirtualWebDisplaySettings
            {
                Screen1 = settings.Screen1.Clone(),
                Screen2 = settings.Screen2.Clone(),
            };
            workingCopy.EnsureValid();

            var descriptionLabel = new Label
            {
                AutoSize = false,
                Left = 18,
                Top = 14,
                Width = 484,
                Height = 44,
                Text = isInitialStartup
                    ? "Configura las Pantallas."
                    : "Ajusta la configuración de cada pantalla. Los cambios se guardan para el próximo inicio de la aplicación.",
            };

            var tabs = new TabControl
            {
                Left = 18,
                Top = 62,
                Width = 484,
                Height = 300,
            };

            _screen1Controls = new ScreenTabControls("Pantalla 1", allowDisable: false, isInitialStartup, workingCopy.Screen1, localIp);
            _screen2Controls = new ScreenTabControls("Pantalla 2", allowDisable: true,  isInitialStartup, workingCopy.Screen2, localIp);

            tabs.TabPages.Add(_screen1Controls.TabPage);
            tabs.TabPages.Add(_screen2Controls.TabPage);

            _acceptButton = new Button
            {
                Left = 326,
                Top = 374,
                Width = 84,
                Height = 28,
                Text = AcceptButtonText,
            };
            _acceptButton.Click += AcceptButton_Click;

            var cancelButton = new Button
            {
                Left = 418,
                Top = 374,
                Width = 84,
                Height = 28,
                Text = isInitialStartup ? "Salir" : "Cerrar",
            };
            cancelButton.Click += (_, _) => Close();

            Controls.AddRange([descriptionLabel, tabs, _acceptButton, cancelButton]);
            AcceptButton = _acceptButton;
            CancelButton = cancelButton;
        }

        public void NotifyStartupCompleted()
        {
            if (!_isInitialStartup || _wasStarted)
                return;

            _wasStarted = true;
            _acceptButton.Text = AcceptButtonText;
        }

        private void AcceptButton_Click(object? sender, EventArgs e)
        {
            if (!ValidateAndBuildSelection(out var selection))
                return;

            Selection = selection;
            ConfigurationApplied?.Invoke(selection);

            if (_wasStarted)
            {
                RestartRequested?.Invoke();
                if (!_isInitialStartup)
                    CloseDialog();
            }
            else if (!_isInitialStartup)
            {
                CloseDialog();
            }
        }

        private bool ValidateAndBuildSelection(out VirtualWebDisplaySettings selection)
        {
            selection = new VirtualWebDisplaySettings
            {
                Screen1 = _screen1Controls.BuildConfig(alwaysEnabled: true),
                Screen2 = _screen2Controls.BuildConfig(alwaysEnabled: false),
            };

            selection.EnsureValid();

            if (selection.Screen2.Enabled && selection.Screen1.Port == selection.Screen2.Port)
            {
                MessageBox.Show(
                    "La Pantalla 2 debe usar un puerto distinto al de la Pantalla 1.",
                    "VirtualWebDisplay & Puerto duplicado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void CloseDialog()
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed class ScreenTabControls
        {
            private readonly VirtualScreenConfig _baseConfig;
            private readonly bool _allowDisable;
            private readonly bool _portEditable;
            private readonly CheckBox? _enabledCheckBox;
            private readonly ComboBox _placementCombo;
            private readonly NumericUpDown _portInput;
            private readonly ComboBox _transmissionMethodCombo;
            private readonly NumericUpDown _captureIntervalInput;
            private readonly TrackBar _jpegQualitySlider;
            private readonly Label _jpegQualityValueLabel;
            private readonly ComboBox _streamRotationCombo;
            private readonly ComboBox _browserImageFitCombo;
            private readonly Control[] _managedControls;
            private readonly string _localIp;
            private readonly LinkLabel _httpUrlLink;

            public ScreenTabControls(string title, bool allowDisable, bool isInitialStartup, VirtualScreenConfig config, string localIp)
            {
                _baseConfig = config.Clone();
                _allowDisable = allowDisable;
                _portEditable = isInitialStartup;
                _localIp = localIp;
                TabPage = new TabPage(title);

                var currentTop = 14;
                if (allowDisable)
                {
                    _enabledCheckBox = new CheckBox
                    {
                        Left = 14,
                        Top = currentTop,
                        Width = 180,
                        Text = "Habilitar (experimental)",
                    };
                    TabPage.Controls.Add(_enabledCheckBox);
                    currentTop += 28;
                }

                // Fila 1: Puerto | Transmisión | Posición
                var portLabel = CreateLabel("Puerto:", 14, currentTop);
                _portInput = new NumericUpDown
                {
                    Left = 14,
                    Top = currentTop + 18,
                    Width = 72,
                    Minimum = 1,
                    Maximum = 65535,
                };

                var methodLabel = CreateLabel("Transmisión:", 98, currentTop);
                _transmissionMethodCombo = new ComboBox
                {
                    Left = 98,
                    Top = currentTop + 18,
                    Width = 170,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                };
                _transmissionMethodCombo.Items.AddRange(
                [
                    new TransmissionMethodItem(TransmissionModeOptions.WebImage, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.WebImage)),
                    new TransmissionMethodItem(TransmissionModeOptions.Rtc, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.Rtc)),
                ]);

                var placementLabel = CreateLabel("Posición:", 280, currentTop);
                _placementCombo = new ComboBox
                {
                    Left = 280,
                    Top = currentTop + 18,
                    Width = 180,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                };
                _placementCombo.Items.AddRange(
                [
                    new PlacementItem("right", "Derecha"),
                    new PlacementItem("left", "Izquierda"),
                    new PlacementItem("top", "Arriba"),
                    new PlacementItem("bottom", "Abajo"),
                    new PlacementItem(VirtualDisplayPlacementOptions.Duplicate, "Duplicar"),
                ]);

                currentTop += 54;

                // Fila 2: Rotación stream
                var rotationLabel = CreateLabel("Rotación stream:", 14, currentTop);
                _streamRotationCombo = new ComboBox
                {
                    Left = 14,
                    Top = currentTop + 18,
                    Width = 230,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                };
                _streamRotationCombo.Items.AddRange(
                [
                    new StreamRotationItem(0,   "Sin rotación (0°)"),
                    new StreamRotationItem(90,  "Rotar 90°"),
                    new StreamRotationItem(180, "Rotar 180°"),
                    new StreamRotationItem(270, "Rotar 270°"),
                ]);

                currentTop += 54;

                // Fila 3: Actualizar cada (ms) | Calidad JPEG
                var captureIntervalLabel = CreateLabel("Actualizar cada (ms):", 14, currentTop);
                _captureIntervalInput = new NumericUpDown
                {
                    Left = 14,
                    Top = currentTop + 18,
                    Width = 72,
                    Minimum = 3M,
                    Maximum = 500M,
                    DecimalPlaces = 0,
                    Increment = 1M,
                };

                var qualityLabel = CreateLabel("Calidad JPEG:", 100, currentTop);
                _jpegQualitySlider = new TrackBar
                {
                    Left = 100,
                    Top = currentTop + 10,
                    Width = 280,
                    Minimum = 10,
                    Maximum = 100,
                    TickFrequency = 10,
                    SmallChange = 5,
                    LargeChange = 10,
                };
                _jpegQualityValueLabel = new Label
                {
                    Left = 388,
                    Top = currentTop + 20,
                    Width = 50,
                };

                currentTop += 54;

                // Fila 4: Ajuste | Botón configuración Windows
                var fitLabel = CreateLabel("Ajuste:", 14, currentTop);
                _browserImageFitCombo = new ComboBox
                {
                    Left = 14,
                    Top = currentTop + 18,
                    Width = 180,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                };
                _browserImageFitCombo.Items.AddRange(
                [
                    new ImageFitItem("fill", "Estirar (llenar)"),
                    new ImageFitItem("cover", "Recortar (cover)"),
                    new ImageFitItem("contain", "Contener (barras)"),
                ]);

                var windowsDisplayButton = new Button
                {
                    Left = 206,
                    Top = currentTop + 14,
                    Width = 254,
                    Height = 28,
                    Text = "Configurar pantallas (Windows) ↗",
                };
                windowsDisplayButton.Click += (_, _) =>
                {
                    try { Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true }); }
                    catch { }
                };

                currentTop += 54;

                _httpUrlLink = new LinkLabel
                {
                    Left = 14,
                    Top = currentTop,
                    Width = 440,
                    AutoSize = false,
                    Text = $"http://{_localIp}:{config.Port}",
                };

                _managedControls =
                [
                    placementLabel,
                    _placementCombo,
                    portLabel,
                    _portInput,
                    methodLabel,
                    _transmissionMethodCombo,
                    captureIntervalLabel,
                    _captureIntervalInput,
                    qualityLabel,
                    _jpegQualitySlider,
                    _jpegQualityValueLabel,
                    rotationLabel,
                    _streamRotationCombo,
                    fitLabel,
                    _browserImageFitCombo,
                ];

                TabPage.Controls.AddRange(_managedControls);
                TabPage.Controls.Add(_httpUrlLink);
                TabPage.Controls.Add(windowsDisplayButton);

                _httpUrlLink.LinkClicked += (_, _) => OpenUrl(_httpUrlLink.Text);

                Initialize(config);

                if (_enabledCheckBox is not null)
                    _enabledCheckBox.CheckedChanged += (_, _) => UpdateState();
                _placementCombo.SelectedIndexChanged += (_, _) => UpdateState();
                _portInput.ValueChanged += (_, _) => UpdateState();
                _transmissionMethodCombo.SelectedIndexChanged += (_, _) => UpdateState();
                _captureIntervalInput.ValueChanged += (_, _) => UpdateState();
                _jpegQualitySlider.ValueChanged += (_, _) => UpdateState();
                _streamRotationCombo.SelectedIndexChanged += (_, _) => UpdateState();
                _browserImageFitCombo.SelectedIndexChanged += (_, _) => UpdateState();
            }

            public TabPage TabPage { get; }

            public VirtualScreenConfig BuildConfig(bool alwaysEnabled)
            {
                var config = _baseConfig.Clone();
                config.Enabled = alwaysEnabled || _enabledCheckBox?.Checked == true;
                config.Port = (int)_portInput.Value;
                config.TransmissionMethod = ((TransmissionMethodItem)_transmissionMethodCombo.SelectedItem!).Method;
                config.CaptureIntervalSeconds = (double)_captureIntervalInput.Value / 1000.0;
                config.JpegQuality = _jpegQualitySlider.Value;
                config.StreamRotationDegrees = ((StreamRotationItem)_streamRotationCombo.SelectedItem!).Degrees;
                config.BrowserImageFit = ((ImageFitItem)_browserImageFitCombo.SelectedItem!).Fit;
                config.VirtualDisplayPlacement = ((PlacementItem)_placementCombo.SelectedItem!).Placement;

                TransmissionModeOptions.EnsureValidSelection(config);
                return config;
            }

            private void Initialize(VirtualScreenConfig config)
            {
                if (_enabledCheckBox is not null)
                    _enabledCheckBox.Checked = config.Enabled;

                _portInput.Value = Math.Max(_portInput.Minimum, Math.Min(_portInput.Maximum, config.Port));
                _captureIntervalInput.Value = Math.Clamp((decimal)(config.CaptureIntervalSeconds * 1000), _captureIntervalInput.Minimum, _captureIntervalInput.Maximum);
                _jpegQualitySlider.Value = Math.Clamp(config.JpegQuality, _jpegQualitySlider.Minimum, _jpegQualitySlider.Maximum);
                var validDegrees = new[] { 0, 90, 180, 270 };
                var degrees = validDegrees.Contains(config.StreamRotationDegrees) ? config.StreamRotationDegrees : 0;
                _streamRotationCombo.SelectedItem = _streamRotationCombo.Items.Cast<StreamRotationItem>()
                    .First(item => item.Degrees == degrees);

                var normalizedPlacement = VirtualDisplayPlacementOptions.Normalize(config.VirtualDisplayPlacement);
                _placementCombo.SelectedItem = _placementCombo.Items.Cast<PlacementItem>()
                    .FirstOrDefault(item => item.Placement == normalizedPlacement)
                    ?? _placementCombo.Items.Cast<PlacementItem>().First(item => item.Placement == VirtualDisplayPlacementOptions.Right);

                var normalizedFit = config.BrowserImageFit?.Trim().ToLowerInvariant() ?? "fill";
                _browserImageFitCombo.SelectedItem = _browserImageFitCombo.Items.Cast<ImageFitItem>()
                    .FirstOrDefault(item => item.Fit == normalizedFit)
                    ?? _browserImageFitCombo.Items.Cast<ImageFitItem>().First(item => item.Fit == "fill");

                _transmissionMethodCombo.SelectedItem = _transmissionMethodCombo.Items.Cast<TransmissionMethodItem>()
                    .First(item => item.Method == TransmissionModeOptions.NormalizeMethod(config.TransmissionMethod));

                UpdateState();
            }

            private void UpdateState()
            {
                var enabled = !_allowDisable || _enabledCheckBox?.Checked == true;
                foreach (var control in _managedControls)
                    control.Enabled = enabled && (control != _portInput || _portEditable);

                _jpegQualityValueLabel.Text = $"{_jpegQualitySlider.Value}%";

                var port = (int)_portInput.Value;
                _httpUrlLink.Text = $"http://{_localIp}:{port}";
            }

            private static void OpenUrl(string url)
            {
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { }
            }

            private static Label CreateLabel(string text, int left, int top) => new()
            {
                AutoSize = true,
                Left = left,
                Top = top,
                Text = text,
            };

            private sealed record TransmissionMethodItem(string Method, string DisplayName)
            {
                public override string ToString() => DisplayName;
            }

            private sealed record StreamRotationItem(int Degrees, string DisplayName)
            {
                public override string ToString() => DisplayName;
            }

            private sealed record PlacementItem(string Placement, string DisplayName)
            {
                public override string ToString() => DisplayName;
            }

            private sealed record ImageFitItem(string Fit, string DisplayName)
            {
                public override string ToString() => DisplayName;
            }
        }
    }
}