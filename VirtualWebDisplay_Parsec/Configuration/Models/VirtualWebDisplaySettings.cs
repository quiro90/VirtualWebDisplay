using VirtualWebDisplay.Localization;
using VirtualWebDisplay.Configuration;

namespace VirtualWebDisplay.Configuration.Models;

public sealed class VirtualWebDisplaySettings
{
    public string UiLanguage { get; set; } = "en";
    public string WindowTheme { get; set; } = WindowThemeOptions.System;
    public VirtualScreenConfig Screen1 { get; set; } = CreateScreen1Defaults();
    public VirtualScreenConfig Screen2 { get; set; } = CreateScreen2Defaults();

    public void EnsureValid()
    {
        UiLanguage = AppText.NormalizeLanguage(UiLanguage);
        WindowTheme = WindowThemeOptions.Normalize(WindowTheme);

        Screen1 ??= CreateScreen1Defaults();
        Screen2 ??= CreateScreen2Defaults();

        Screen1.Enabled = true;

        TransmissionModeOptions.EnsureValidSelection(Screen1);
        TransmissionModeOptions.EnsureValidSelection(Screen2);
        Screen1.TouchGestureHoldDelayMs = TouchGestureOptions.ClampHoldDelay(Screen1.TouchGestureHoldDelayMs);
        Screen2.TouchGestureHoldDelayMs = TouchGestureOptions.ClampHoldDelay(Screen2.TouchGestureHoldDelayMs);

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

}
