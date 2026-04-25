public sealed class VirtualWebDisplaySettings
{
    public VirtualScreenConfig Screen1 { get; set; } = CreateScreen1Defaults();
    public VirtualScreenConfig Screen2 { get; set; } = CreateScreen2Defaults();

    public void EnsureValid()
    {
        Screen1 ??= CreateScreen1Defaults();
        Screen2 ??= CreateScreen2Defaults();

        Screen1.Enabled = true;

        MigrateRotation(Screen1);
        MigrateRotation(Screen2);

        VirtualDisplayProfiles.EnsureValidSelection(Screen1);
        VirtualDisplayProfiles.EnsureValidSelection(Screen2);
        TransmissionModeOptions.EnsureValidSelection(Screen1);
        TransmissionModeOptions.EnsureValidSelection(Screen2);

        if (Screen1.Port <= 0)
            Screen1.Port = 8000;

        if (Screen2.Port <= 0)
            Screen2.Port = Screen1.Port == 8000 ? 8001 : Screen1.Port + 1;

        if (Screen2.Port == Screen1.Port)
            Screen2.Port = Screen1.Port == 65535 ? 65534 : Screen1.Port + 1;
    }

    public static VirtualScreenConfig CreateScreen1Defaults() => new()
    {
        Enabled = true,
        Port = 8000,
        VirtualDisplayPlacement = "right",
    };

    public static VirtualScreenConfig CreateScreen2Defaults() => new()
    {
        Enabled = false,
        Port = 8001,
        VirtualDisplayPlacement = "left",
        TransmissionMethod = TransmissionModeOptions.Rtc,
    };

    private static void MigrateRotation(VirtualScreenConfig config)
    {
        if (config.StreamRotationDegrees == 0 && config.RotateForPortrait)
            config.StreamRotationDegrees = 90;
    }
}
