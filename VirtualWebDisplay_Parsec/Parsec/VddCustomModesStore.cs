using Microsoft.Win32;

namespace VirtualWebDisplay.Parsec;

/// <summary>
/// Lee y escribe los modos de resolución personalizados del driver Parsec VDD
/// en HKLM\SOFTWARE\Parsec\vdd\{0..4} → valores width, height, hz (DWORD).
/// Esta es la misma clave que usa la app oficial ParsecVDisplay.
/// </summary>
public static class VddCustomModesStore
{
    private const string RegistryPath = @"SOFTWARE\Parsec\vdd";
    public const int MaxSlots = 5;

    public sealed record CustomMode(int Width, int Height, int Hz);

    /// <summary>
    /// Lee los modos personalizados del registro.
    /// Devuelve lista vacía si la clave no existe o no hay modos configurados.
    /// No lanza excepciones.
    /// </summary>
    public static List<CustomMode> Read()
    {
        var result = new List<CustomMode>();
        try
        {
            using var vdd = Registry.LocalMachine.OpenSubKey(RegistryPath, writable: false);
            if (vdd is null)
                return result;

            for (var i = 0; i < MaxSlots; i++)
            {
                using var slot = vdd.OpenSubKey($"{i}", writable: false);
                if (slot is null)
                    continue;

                var width  = slot.GetValue("width");
                var height = slot.GetValue("height");
                var hz     = slot.GetValue("hz");

                if (width is not null && height is not null && hz is not null)
                {
                    result.Add(new CustomMode(
                        Convert.ToInt32(width),
                        Convert.ToInt32(height),
                        Convert.ToInt32(hz)));
                }
            }
        }
        catch { }

        return result;
    }

    /// <summary>
    /// Escribe los modos en el registro, reemplazando completamente lo que hubiera.
    /// Slots sobrantes (vacíos) se eliminan.
    /// Requiere privilegios de Administrador; lanza <see cref="UnauthorizedAccessException"/> si no los tiene.
    /// </summary>
    public static void Write(IReadOnlyList<CustomMode> modes)
    {
        using var vdd = Registry.LocalMachine.CreateSubKey(RegistryPath, writable: true)
            ?? throw new InvalidOperationException($"No se pudo abrir la clave {RegistryPath}");

        for (var i = 0; i < MaxSlots; i++)
        {
            if (i >= modes.Count)
            {
                // Eliminar slot si existe
                vdd.DeleteSubKey($"{i}", throwOnMissingSubKey: false);
                continue;
            }

            using var slot = vdd.CreateSubKey($"{i}", writable: true);
            if (slot is null) continue;

            slot.SetValue("width",  modes[i].Width,  RegistryValueKind.DWord);
            slot.SetValue("height", modes[i].Height, RegistryValueKind.DWord);
            slot.SetValue("hz",     modes[i].Hz,     RegistryValueKind.DWord);
        }
    }

    /// <summary>
    /// Indica si el proceso actual corre con privilegios de Administrador.
    /// </summary>
    public static bool IsAdmin()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
