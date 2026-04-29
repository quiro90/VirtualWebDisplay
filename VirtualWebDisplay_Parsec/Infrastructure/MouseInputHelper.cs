using System.Runtime.InteropServices;

namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Inyecta eventos de mouse sintéticos en Windows usando SendInput API.
/// Compatible con Parsec VDD (pantallas virtuales).
/// NO requiere permisos de Admin.
/// </summary>
internal static class MouseInputHelper
{
    // Última posición conocida del cursor para restaurar tras gestos/tap
    private static POINT? _lastCursorPosition = null;

    /// <summary>
    /// Guarda la posición actual del cursor para restaurar luego.
    /// </summary>
    public static void SaveCurrentCursorPosition()
    {
        if (!_lastCursorPosition.HasValue && GetCursorPos(out var pt))
            _lastCursorPosition = pt;
    }

    /// <summary>
    /// Restaura la última posición guardada del cursor, si existe.
    /// </summary>
    public static void RestoreLastCursorPosition()
    {
        if (_lastCursorPosition.HasValue)
        {
            MoveMouse(_lastCursorPosition.Value.X, _lastCursorPosition.Value.Y);
            _lastCursorPosition = null;
        }
    }

    // P/Invoke: SendInput desde user32.dll
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    // Constantes de flags para MOUSEINPUT
    private const uint MOUSEEVENTF_LEFTDOWN   = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP     = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN  = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP    = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP   = 0x0040;

    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const double TOUCH_SCROLL_GAIN = 1.6;

    // Constantes INPUT
    private const int INPUT_MOUSE = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// Mueve el cursor a coordenadas absolutas de pantalla.
    /// </summary>
    public static void MoveMouse(int screenX, int screenY)
    {
        try
        {
            if (!SetCursorPos(screenX, screenY))
            {
                var code = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"[MouseInputHelper] SetCursorPos failed: {code}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MouseInputHelper] MoveMouse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Click izquierdo en coordenadas absolutas.
    /// Secuencia: MOVE → LEFTDOWN → LEFTUP
    /// </summary>
    public static void LeftClick(int screenX, int screenY)
    {
        try
        {
            MoveMouse(screenX, screenY);

            var inputs = new INPUT[]
            {
                CreateMouseInput(0, 0, MOUSEEVENTF_LEFTDOWN),
                CreateMouseInput(0, 0, MOUSEEVENTF_LEFTUP)
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            System.Diagnostics.Debug.WriteLine($"[MouseInputHelper] LeftClick at ({screenX}, {screenY})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MouseInputHelper] LeftClick error: {ex.Message}");
        }
    }

    /// <summary>
    /// Click izquierdo en coordenadas objetivo y luego restaura la posición original del cursor.
    /// </summary>
    public static void LeftClickPreservingCursor(int screenX, int screenY)
    {
        SaveCurrentCursorPosition();
        LeftClick(screenX, screenY);
        RestoreLastCursorPosition();
    }

    /// <summary>
    /// Posiciona el cursor y envía LEFTDOWN (inicio de arrastre).
    /// </summary>
    public static void LeftDownAt(int screenX, int screenY)
    {
        MoveMouse(screenX, screenY);

        var inputs = new INPUT[]
        {
            CreateMouseInput(0, 0, MOUSEEVENTF_LEFTDOWN)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    /// <summary>
    /// Posiciona el cursor en las coordenadas objetivo, envia LEFTDOWN y luego restaura
    /// la posicion original del cursor. El boton queda presionado sin mover el puntero real.
    /// </summary>
    public static void LeftDownPreservingCursor(int screenX, int screenY)
    {
        SaveCurrentCursorPosition();
        LeftDownAt(screenX, screenY);
    }

    /// <summary>
    /// Envía LEFTUP (fin de arrastre).
    /// </summary>
    public static void LeftUp()
    {
        var inputs = new INPUT[]
        {
            CreateMouseInput(0, 0, MOUSEEVENTF_LEFTUP)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    /// <summary>
    /// Doble-click izquierdo (dos clicks consecutivos con pequeña pausa).
    /// </summary>
    public static void DoubleClick(int screenX, int screenY)
    {
        try
        {
            LeftClick(screenX, screenY);
            System.Threading.Thread.Sleep(50);
            LeftClick(screenX, screenY);
            System.Diagnostics.Debug.WriteLine($"[MouseInputHelper] DoubleClick at ({screenX}, {screenY})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MouseInputHelper] DoubleClick error: {ex.Message}");
        }
    }

    /// <summary>
    /// Click derecho en coordenadas absolutas.
    /// Secuencia: MOVE → RIGHTDOWN → RIGHTUP
    /// </summary>
    public static void RightClick(int screenX, int screenY)
    {
        try
        {
            MoveMouse(screenX, screenY);

            var inputs = new INPUT[]
            {
                CreateMouseInput(0, 0, MOUSEEVENTF_RIGHTDOWN),
                CreateMouseInput(0, 0, MOUSEEVENTF_RIGHTUP)
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            System.Diagnostics.Debug.WriteLine($"[MouseInputHelper] RightClick at ({screenX}, {screenY})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MouseInputHelper] RightClick error: {ex.Message}");
        }
    }

    /// <summary>
    /// Click derecho en coordenadas objetivo y luego restaura la posición original del cursor.
    /// </summary>
    public static void RightClickPreservingCursor(int screenX, int screenY)
    {
        SaveCurrentCursorPosition();
        RightClick(screenX, screenY);
        RestoreLastCursorPosition();
    }

    /// <summary>
    /// Click central en coordenadas absolutas.
    /// Secuencia: MOVE → MIDDLEDOWN → MIDDLEUP
    /// </summary>
    public static void MiddleClick(int screenX, int screenY)
    {
        try
        {
            MoveMouse(screenX, screenY);

            var inputs = new INPUT[]
            {
                CreateMouseInput(0, 0, MOUSEEVENTF_MIDDLEDOWN),
                CreateMouseInput(0, 0, MOUSEEVENTF_MIDDLEUP)
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            System.Diagnostics.Debug.WriteLine($"[MouseInputHelper] MiddleClick at ({screenX}, {screenY})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MouseInputHelper] MiddleClick error: {ex.Message}");
        }
    }

    /// <summary>
    /// Click central en coordenadas objetivo y luego restaura la posición original del cursor.
    /// </summary>
    public static void MiddleClickPreservingCursor(int screenX, int screenY)
    {
        SaveCurrentCursorPosition();
        MiddleClick(screenX, screenY);
        RestoreLastCursorPosition();
    }

    /// <summary>
    /// Envia un evento de rueda de scroll a partir de delta tactil vertical.
    /// Esta configurado en modo "natural invertido" segun preferencia: arrastre hacia abajo => scroll arriba.
    /// </summary>
    /// <summary>
    /// Envía eventos de scroll vertical y/u horizontal (ambos opcionales, pueden ser 0).
    /// </summary>
    public static void Scroll(int deltaY, int deltaX)
    {
        var inputs = new List<INPUT>(2);
        if (deltaY != 0)
        {
            var wheelDeltaY = (int)Math.Round(deltaY * TOUCH_SCROLL_GAIN);
            if (wheelDeltaY == 0)
                wheelDeltaY = deltaY > 0 ? 1 : -1;
            inputs.Add(new INPUT {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_WHEEL, mouseData = (uint)wheelDeltaY }
            });
        }
        if (deltaX != 0)
        {
            var wheelDeltaX = (int)Math.Round(deltaX * TOUCH_SCROLL_GAIN);
            if (wheelDeltaX == 0)
                wheelDeltaX = deltaX > 0 ? 1 : -1;
            // 0x1000 = MOUSEEVENTF_HWHEEL (horizontal)
            inputs.Add(new INPUT {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT { dwFlags = 0x1000, mouseData = (uint)wheelDeltaX }
            });
        }
        if (inputs.Count > 0)
            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
    }

    public static bool TryGetCursorPosition(out int x, out int y)
    {
        if (GetCursorPos(out var point))
        {
            x = point.X;
            y = point.Y;
            return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    // Helper privado para crear eventos de mouse
    private static INPUT CreateMouseInput(int dx, int dy, uint dwFlags)
    {
        return new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                dx = dx,
                dy = dy,
                dwFlags = dwFlags,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            }
        };
    }
}
