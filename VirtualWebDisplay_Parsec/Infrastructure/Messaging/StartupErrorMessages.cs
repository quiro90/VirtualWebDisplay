using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Infrastructure.Messaging;

/// <summary>
/// Centraliza la construcción de mensajes de error durante el inicio de la aplicación.
/// Elimina duplicación del patrón "mensaje + \n\n + sufijo".
/// </summary>
public static class StartupErrorMessages
{
    /// <summary>
    /// Construye mensaje de error cuando el driver no está disponible.
    /// </summary>
    public static string ForDriverUnavailable(string driverStatus)
    {
        return $"{driverStatus}\n\n{AppText.Get("Program_DriverMissing_MessageSuffix")}";
    }

    /// <summary>
    /// Construye mensaje de error cuando no se puede crear un display virtual.
    /// </summary>
    public static string ForDisplayCreationFailure(string displayStatus)
    {
        return $"{displayStatus}\n\n{AppText.Get("Program_DriverMissing_MessageSuffix")}";
    }

    /// <summary>
    /// Construye mensaje de error cuando el monitor no se detecta después de crearlo.
    /// </summary>
    public static string ForMonitorNotDetected(string displayStatus, string screenName)
    {
        return $"{displayStatus}\n\n{AppText.Format("Program_MonitorNotDetected_Message", screenName)}";
    }

    /// <summary>
    /// Construye título de error para problemas de display específico.
    /// </summary>
    public static string TitleForDisplayError(string displayName)
    {
        return AppText.Format("Program_DisplayError_Title", displayName);
    }

    /// <summary>
    /// Construye título genérico para problemas de driver.
    /// </summary>
    public static string TitleForDriverMissing()
    {
        return AppText.Get("Program_DriverMissing_Title");
    }
}
