using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Configuration.Models;

public sealed class VirtualWebDisplaySettings
{
    public string UiLanguage { get; set; } = "es";
    public VirtualScreenConfig Screen1 { get; set; } = CreateScreen1Defaults();
    public VirtualScreenConfig Screen2 { get; set; } = CreateScreen2Defaults();

    public void EnsureValid()
    {
        UiLanguage = AppText.NormalizeLanguage(UiLanguage);

        Screen1 ??= CreateScreen1Defaults();
        Screen2 ??= CreateScreen2Defaults();

        Screen1.Enabled = true;

        MigrateRotation(Screen1);
        MigrateRotation(Screen2);

        VirtualDisplayProfiles.EnsureValidSelection(Screen1);
        VirtualDisplayProfiles.EnsureValidSelection(Screen2);
        TransmissionModeOptions.EnsureValidSelection(Screen1);
        TransmissionModeOptions.EnsureValidSelection(Screen2);

        // Asignar puerto por defecto solo si no está configurado.
        // Los puertos configurados por el usuario se respetan.
        if (Screen1.Port <= 0)
            Screen1.Port = 8000;

        if (Screen2.Port <= 0)
            Screen2.Port = Screen1.Port == 8000 ? 8002 : Screen1.Port + 2;

        // Cada pantalla necesita 2 puertos consecutivos (HTTP en Port, HTTPS en Port+1).
        // Validar que Screen2 no se superponga con los puertos de Screen1.
        // Esto solo ajusta si hay conflicto, respetando la configuración del usuario en otros casos.
        if (Screen2.Port == Screen1.Port || Screen2.Port == Screen1.Port + 1)
            Screen2.Port = Screen1.Port >= 65534 ? 65532 : Screen1.Port + 2;
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
        Port = 8002,
        VirtualDisplayPlacement = "left",
        TransmissionMethod = TransmissionModeOptions.Rtc,
    };

    private static void MigrateRotation(VirtualScreenConfig config)
    {
        if (config.StreamRotationDegrees == 0 && config.RotateForPortrait)
            config.StreamRotationDegrees = 90;
        config.RotateForPortrait = false;
    }
}
