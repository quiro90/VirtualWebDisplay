using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

public sealed class VirtualDisplayTrayController : IDisposable
{
    private readonly VirtualWebDisplaySettings _settings;
    private readonly VirtualScreenSettingsStore _settingsStore;
    private readonly Thread _uiThread;
    private readonly ManualResetEventSlim _ready = new(false);

    private ApplicationContext? _context;
    private Control? _invoker;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private Action? _exitRequested;
    private IReadOnlyList<ScreenRuntimeContext> _screenRuntimes = [];
    private bool _disposed;

    public VirtualDisplayTrayController(VirtualWebDisplaySettings settings, VirtualScreenSettingsStore settingsStore)
    {
        _settings = settings;
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
            using var form = new ResolutionConfigurationForm(_settings, isInitialStartup: true);
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

    public void ConfigureRuntimeActions(Action exitRequested, IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
    {
        _exitRequested = exitRequested;
        _screenRuntimes = screenRuntimes;

        PostToUi(() =>
        {
            if (_notifyIcon is null)
                return;

            _contextMenu?.Dispose();
            _contextMenu = BuildContextMenu();
            _notifyIcon.ContextMenuStrip = _contextMenu;
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

        _contextMenu = BuildContextMenu();
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
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

        menu.Items.Add("Salir", null, (_, _) => ExitApplication());
        return menu;
    }

    private void ShowConfigurationDialog()
    {
        using var form = new ResolutionConfigurationForm(_settings, isInitialStartup: false);
        if (form.ShowDialog() != DialogResult.OK)
            return;

        ApplySelection(form.Selection);
        _notifyIcon?.ShowBalloonTip(4000, "VirtualWebDisplay", "Configuración guardada. Reiniciá la app para recrear pantallas y puertos con los nuevos valores.", ToolTipIcon.Info);
    }

    private void ApplySelection(VirtualWebDisplaySettings selection)
    {
        selection.EnsureValid();

        CopyConfig(selection.Screen1, _settings.Screen1);
        CopyConfig(selection.Screen2, _settings.Screen2);
        _settings.EnsureValid();
        _settingsStore.Save(_settings);
    }

    private static void CopyConfig(VirtualScreenConfig source, VirtualScreenConfig target)
    {
        target.Enabled = source.Enabled;
        target.Width = source.Width;
        target.Height = source.Height;
        target.Profile = source.Profile;
        target.Landscape = source.Landscape;
        target.CustomWidth = source.CustomWidth;
        target.CustomHeight = source.CustomHeight;
        target.TransmissionMethod = source.TransmissionMethod;
        target.CaptureIntervalSeconds = source.CaptureIntervalSeconds;
        target.JpegQuality = source.JpegQuality;
        target.Port = source.Port;
        target.RotateForPortrait = source.RotateForPortrait;
        target.MonitorIndex = source.MonitorIndex;
        target.VirtualDisplayPlacement = source.VirtualDisplayPlacement;
        target.BrowserImageFit = source.BrowserImageFit;
    }

    private static VirtualScreenConfig CloneConfig(VirtualScreenConfig source) => new()
    {
        Enabled = source.Enabled,
        Width = source.Width,
        Height = source.Height,
        Profile = source.Profile,
        Landscape = source.Landscape,
        CustomWidth = source.CustomWidth,
        CustomHeight = source.CustomHeight,
        TransmissionMethod = source.TransmissionMethod,
        CaptureIntervalSeconds = source.CaptureIntervalSeconds,
        JpegQuality = source.JpegQuality,
        Port = source.Port,
        RotateForPortrait = source.RotateForPortrait,
        MonitorIndex = source.MonitorIndex,
        VirtualDisplayPlacement = source.VirtualDisplayPlacement,
        BrowserImageFit = source.BrowserImageFit,
    };

    private static VirtualWebDisplaySettings CloneSettings(VirtualWebDisplaySettings settings) => new()
    {
        Screen1 = CloneConfig(settings.Screen1),
        Screen2 = CloneConfig(settings.Screen2),
    };

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
        private readonly ScreenTabControls _screen1Controls;
        private readonly ScreenTabControls _screen2Controls;

        public VirtualWebDisplaySettings Selection { get; private set; } = new();

        public ResolutionConfigurationForm(VirtualWebDisplaySettings settings, bool isInitialStartup)
        {
            Text = isInitialStartup ? "VirtualWebDisplay & Configuración de pantallas" : "VirtualWebDisplay & Configuración";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 390);

            var workingCopy = CloneSettings(settings);
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
                Height = 270,
            };

            _screen1Controls = new ScreenTabControls("Pantalla 1", allowDisable: false, isInitialStartup, workingCopy.Screen1);
            _screen2Controls = new ScreenTabControls("Pantalla 2", allowDisable: true, isInitialStartup, workingCopy.Screen2);

            tabs.TabPages.Add(_screen1Controls.TabPage);
            tabs.TabPages.Add(_screen2Controls.TabPage);

            var acceptButton = new Button
            {
                Left = 326,
                Top = 344,
                Width = 84,
                Height = 28,
                Text = isInitialStartup ? "Iniciar" : "Guardar",
                DialogResult = DialogResult.OK,
            };

            var cancelButton = new Button
            {
                Left = 418,
                Top = 344,
                Width = 84,
                Height = 28,
                Text = isInitialStartup ? "Salir" : "Cerrar",
                DialogResult = DialogResult.Cancel,
            };

            Controls.AddRange([descriptionLabel, tabs, acceptButton, cancelButton]);
            AcceptButton = acceptButton;
            CancelButton = cancelButton;

            FormClosing += (_, args) =>
            {
                if (DialogResult != DialogResult.OK)
                    return;

                var selection = new VirtualWebDisplaySettings
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
                    args.Cancel = true;
                    return;
                }

                Selection = selection;
            };
        }

        private sealed class ScreenTabControls
        {
            private readonly VirtualScreenConfig _baseConfig;
            private readonly bool _allowDisable;
            private readonly CheckBox? _enabledCheckBox;
            private readonly ComboBox _profileCombo;
            private readonly NumericUpDown _widthInput;
            private readonly NumericUpDown _heightInput;
            private readonly Button _rotarButton;
            private readonly ComboBox _placementCombo;
            private readonly NumericUpDown _portInput;
            private readonly ComboBox _transmissionMethodCombo;
            private readonly NumericUpDown _captureIntervalInput;
            private readonly TrackBar _jpegQualitySlider;
            private readonly Label _jpegQualityValueLabel;
            private readonly CheckBox _rotateStreamCheckBox;
            private readonly Control[] _managedControls;
            private bool _suppressEvents;

            public ScreenTabControls(string title, bool allowDisable, bool isInitialStartup, VirtualScreenConfig config)
            {
                _baseConfig = CloneConfig(config);
                _allowDisable = allowDisable;
                TabPage = new TabPage(title);

                var currentTop = 14;
                if (allowDisable)
                {
                    _enabledCheckBox = new CheckBox
                    {
                        Left = 14,
                        Top = currentTop,
                        Width = 120,
                        Text = "Habilitada",
                    };
                    TabPage.Controls.Add(_enabledCheckBox);
                    currentTop += 28;
                }

                var profileLabel = CreateLabel("Perfil:", 14, currentTop);
                _profileCombo = new ComboBox
                {
                    Left = 14,
                    Top = currentTop + 18,
                    Width = 260,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                };
                _profileCombo.Items.AddRange(VirtualDisplayProfiles.All.Select(profile => new ProfileItem(profile.Id, profile.DisplayName)).ToArray());

                var placementLabel = CreateLabel("Posición:", 286, currentTop);
                _placementCombo = new ComboBox
                {
                    Left = 286,
                    Top = currentTop + 18,
                    Width = 174,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                };
                _placementCombo.Items.AddRange(
                [
                    new PlacementItem("right", "Derecha"),
                    new PlacementItem("left", "Izquierda"),
                    new PlacementItem("top", "Arriba"),
                    new PlacementItem("bottom", "Abajo"),
                ]);

                currentTop += 54;

                var widthLabel = CreateLabel("Ancho:", 14, currentTop);
                _widthInput = new NumericUpDown
                {
                    Left = 14,
                    Top = currentTop + 18,
                    Width = 84,
                    Minimum = 100,
                    Maximum = 5000,
                };

                var heightLabel = CreateLabel("Alto:", 106, currentTop);
                _heightInput = new NumericUpDown
                {
                    Left = 106,
                    Top = currentTop + 18,
                    Width = 84,
                    Minimum = 100,
                    Maximum = 5000,
                };

                _rotarButton = new Button
                {
                    Left = 196,
                    Top = currentTop + 18,
                    Width = 44,
                    Height = 24,
                    Text = "\u21d5",
                };

                var portLabel = CreateLabel("Puerto:", 248, currentTop);
                _portInput = new NumericUpDown
                {
                    Left = 248,
                    Top = currentTop + 18,
                    Width = 84,
                    Minimum = 1,
                    Maximum = 65535,
                    Enabled = isInitialStartup,
                };

                var methodLabel = CreateLabel("Transmisión:", 340, currentTop);
                _transmissionMethodCombo = new ComboBox
                {
                    Left = 340,
                    Top = currentTop + 18,
                    Width = 122,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                };
                _transmissionMethodCombo.Items.AddRange(
                [
                    new TransmissionMethodItem(TransmissionModeOptions.WebImage, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.WebImage)),
                    new TransmissionMethodItem(TransmissionModeOptions.Rtc, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.Rtc)),
                ]);

                currentTop += 54;

                var captureIntervalLabel = CreateLabel("Actualizar cada (segundos):", 14, currentTop);
                _captureIntervalInput = new NumericUpDown
                {
                    Left = 14,
                    Top = currentTop + 18,
                    Width = 96,
                    Minimum = 0.01M,
                    Maximum = 60M,
                    DecimalPlaces = 3,
                    Increment = 0.01M,
                };

                var qualityLabel = CreateLabel("Calidad JPEG:", 126, currentTop);
                _jpegQualitySlider = new TrackBar
                {
                    Left = 126,
                    Top = currentTop + 10,
                    Width = 228,
                    Minimum = 10,
                    Maximum = 100,
                    TickFrequency = 10,
                    SmallChange = 5,
                    LargeChange = 10,
                };
                _jpegQualityValueLabel = new Label
                {
                    Left = 362,
                    Top = currentTop + 20,
                    Width = 64,
                };

                currentTop += 54;

                _rotateStreamCheckBox = new CheckBox
                {
                    Left = 14,
                    Top = currentTop,
                    Width = 320,
                    Text = "Rotar imagen 90° (girar retransmisión)",
                };

                _managedControls =
                [
                    profileLabel,
                    _profileCombo,
                    placementLabel,
                    _placementCombo,
                    widthLabel,
                    _widthInput,
                    heightLabel,
                    _heightInput,
                    _rotarButton,
                    portLabel,
                    _portInput,
                    methodLabel,
                    _transmissionMethodCombo,
                    captureIntervalLabel,
                    _captureIntervalInput,
                    qualityLabel,
                    _jpegQualitySlider,
                    _jpegQualityValueLabel,
                    _rotateStreamCheckBox,
                ];

                TabPage.Controls.AddRange(_managedControls);

                Initialize(config);

                if (_enabledCheckBox is not null)
                    _enabledCheckBox.CheckedChanged += (_, _) => UpdateState();
                _profileCombo.SelectedIndexChanged += (_, _) =>
                {
                    OnProfileSelected();
                    UpdateState();
                };
                _widthInput.ValueChanged += (_, _) => OnDimensionChanged();
                _heightInput.ValueChanged += (_, _) => OnDimensionChanged();
                _rotarButton.Click += (_, _) =>
                {
                    var w = _widthInput.Value;
                    var h = _heightInput.Value;
                    _suppressEvents = true;
                    _widthInput.Value = Math.Max(_widthInput.Minimum, Math.Min(_widthInput.Maximum, h));
                    _heightInput.Value = Math.Max(_heightInput.Minimum, Math.Min(_heightInput.Maximum, w));
                    _suppressEvents = false;
                    UpdateState();
                };
                _placementCombo.SelectedIndexChanged += (_, _) => UpdateState();
                _portInput.ValueChanged += (_, _) => UpdateState();
                _transmissionMethodCombo.SelectedIndexChanged += (_, _) => UpdateState();
                _captureIntervalInput.ValueChanged += (_, _) => UpdateState();
                _jpegQualitySlider.ValueChanged += (_, _) => UpdateState();
                _rotateStreamCheckBox.CheckedChanged += (_, _) => UpdateState();
            }

            public TabPage TabPage { get; }

            public VirtualScreenConfig BuildConfig(bool alwaysEnabled)
            {
                var config = CloneConfig(_baseConfig);
                config.Enabled = alwaysEnabled || _enabledCheckBox?.Checked == true;
                config.Profile = VirtualDisplayProfiles.Custom;
                config.Landscape = false;
                config.CustomWidth = (int)_widthInput.Value;
                config.CustomHeight = (int)_heightInput.Value;
                config.Port = (int)_portInput.Value;
                config.TransmissionMethod = ((TransmissionMethodItem)_transmissionMethodCombo.SelectedItem!).Method;
                config.CaptureIntervalSeconds = (double)_captureIntervalInput.Value;
                config.JpegQuality = _jpegQualitySlider.Value;
                config.RotateForPortrait = _rotateStreamCheckBox.Checked;
                config.VirtualDisplayPlacement = ((PlacementItem)_placementCombo.SelectedItem!).Placement;

                VirtualDisplayProfiles.EnsureValidSelection(config);
                TransmissionModeOptions.EnsureValidSelection(config);
                return config;
            }

            private void Initialize(VirtualScreenConfig config)
            {
                if (_enabledCheckBox is not null)
                    _enabledCheckBox.Checked = config.Enabled;

                var w = config.Width > 0 ? config.Width : config.CustomWidth;
                var h = config.Height > 0 ? config.Height : config.CustomHeight;

                var matchedProfile = VirtualDisplayProfiles.All
                    .Where(p => !VirtualDisplayProfiles.IsCustom(p.Id))
                    .FirstOrDefault(p => (p.PortraitWidth == w && p.PortraitHeight == h)
                                      || (p.PortraitHeight == w && p.PortraitWidth == h));

                _suppressEvents = true;
                _profileCombo.SelectedItem = _profileCombo.Items.Cast<ProfileItem>()
                    .First(item => item.ProfileId == (matchedProfile?.Id ?? VirtualDisplayProfiles.Custom));
                _widthInput.Value = Math.Max(_widthInput.Minimum, Math.Min(_widthInput.Maximum, w > 0 ? w : 1080));
                _heightInput.Value = Math.Max(_heightInput.Minimum, Math.Min(_heightInput.Maximum, h > 0 ? h : 1920));
                _suppressEvents = false;

                _portInput.Value = Math.Max(_portInput.Minimum, Math.Min(_portInput.Maximum, config.Port));
                _captureIntervalInput.Value = Math.Max(_captureIntervalInput.Minimum, Math.Min(_captureIntervalInput.Maximum, (decimal)config.CaptureIntervalSeconds));
                _jpegQualitySlider.Value = Math.Clamp(config.JpegQuality, _jpegQualitySlider.Minimum, _jpegQualitySlider.Maximum);
                _rotateStreamCheckBox.Checked = config.RotateForPortrait;

                _placementCombo.SelectedItem = _placementCombo.Items.Cast<PlacementItem>()
                    .FirstOrDefault(item => item.Placement == VirtualDisplayPlacementOptions.Normalize(config.VirtualDisplayPlacement))
                    ?? _placementCombo.Items.Cast<PlacementItem>().First(item => item.Placement == VirtualDisplayPlacementOptions.Right);

                _transmissionMethodCombo.SelectedItem = _transmissionMethodCombo.Items.Cast<TransmissionMethodItem>()
                    .First(item => item.Method == TransmissionModeOptions.NormalizeMethod(config.TransmissionMethod));

                UpdateState();
            }

            private void OnProfileSelected()
            {
                if (_suppressEvents)
                    return;
                var profileId = ((ProfileItem)_profileCombo.SelectedItem!).ProfileId;
                if (VirtualDisplayProfiles.IsCustom(profileId))
                    return;
                var profile = VirtualDisplayProfiles.All.First(p => p.Id == profileId);
                _suppressEvents = true;
                _widthInput.Value = profile.PortraitWidth;
                _heightInput.Value = profile.PortraitHeight;
                _suppressEvents = false;
            }

            private void OnDimensionChanged()
            {
                if (_suppressEvents)
                    return;
                var profileId = ((ProfileItem)_profileCombo.SelectedItem!).ProfileId;
                if (!VirtualDisplayProfiles.IsCustom(profileId))
                {
                    var profile = VirtualDisplayProfiles.All.First(p => p.Id == profileId);
                    var w = (int)_widthInput.Value;
                    var h = (int)_heightInput.Value;
                    if (w != profile.PortraitWidth || h != profile.PortraitHeight)
                        _profileCombo.SelectedItem = _profileCombo.Items.Cast<ProfileItem>()
                            .First(item => item.ProfileId == VirtualDisplayProfiles.Custom);
                }
                UpdateState();
            }

            private void UpdateState()
            {
                var enabled = !_allowDisable || _enabledCheckBox?.Checked == true;
                foreach (var control in _managedControls)
                    control.Enabled = enabled && (control != _portInput || _portInput.Enabled);

                var isCustom = VirtualDisplayProfiles.IsCustom(((ProfileItem)_profileCombo.SelectedItem!).ProfileId);
                _widthInput.Enabled = enabled && isCustom;
                _heightInput.Enabled = enabled && isCustom;
                _rotarButton.Enabled = enabled && isCustom;

                _jpegQualityValueLabel.Text = $"{_jpegQualitySlider.Value}%";
            }

            private static Label CreateLabel(string text, int left, int top) => new()
            {
                AutoSize = true,
                Left = left,
                Top = top,
                Text = text,
            };

            private sealed record ProfileItem(string ProfileId, string DisplayName)
            {
                public override string ToString() => DisplayName;
            }

            private sealed record TransmissionMethodItem(string Method, string DisplayName)
            {
                public override string ToString() => DisplayName;
            }

            private sealed record PlacementItem(string Placement, string DisplayName)
            {
                public override string ToString() => DisplayName;
            }
        }
    }
}