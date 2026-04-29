using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Infrastructure.Drivers;

/// <summary>
/// Verificador de disponibilidad del driver Parsec Virtual Display Driver.
/// Encapsula la lógica de detección del VDD sin acoplarse a VirtualDisplayManager.
/// </summary>
public sealed class ParsecVddDriverVerifier : IDriverVerifier
{
    private const string AdapterGuid = "{00b41627-04c4-429e-a26e-0265cf50c8fa}";

    public string DriverName => "Parsec Virtual Display Driver (VDD)";
    public string InstallUrl => "https://builds.parsec.app/vdd/parsec-vdd-0.45.0.0.exe";

    public (bool isAvailable, string statusMessage) Verify()
    {
        if (!Parsec.ParsecVddDriverApi.OpenHandle(AdapterGuid, out var handle))
        {
            return (false, AppText.Get("Parsec_Driver_NotFound"));
        }

        Parsec.ParsecVddDriverApi.CloseHandle(handle);
        return (true, AppText.Get("Parsec_Driver_Detected"));
    }
}
