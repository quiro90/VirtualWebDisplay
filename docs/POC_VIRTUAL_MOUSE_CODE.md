# 🧪 Prueba de Concepto (PoC): Implementación de Virtual Mouse Input

Este documento muestra **código listo para copiar-pegar** si decides implementar la funcionalidad.

---

## 📋 PASO 1: Crear `MouseInputHelper.cs`

**Ubicación:** `Infrastructure/MouseInputHelper.cs`

```csharp
using System.Runtime.InteropServices;

namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Inyecta eventos de mouse sintéticos en Windows usando SendInput API.
/// NO requiere permisos de Admin.
/// </summary>
internal static class MouseInputHelper
{
    // P/Invoke: SendInput
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    // Constantes de flags para MOUSEINPUT
    private const uint MOUSEEVENTF_MOVE       = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN   = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP     = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN  = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP    = 0x0010;
    private const uint MOUSEEVENTF_ABSOLUTE   = 0x8000;

    // Constantes INPUT
    private const int INPUT_MOUSE = 0;

    // Structs Win32
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

    /// <summary>
    /// Mueve el mouse a coordenadas absolutas (pantalla).
    /// </summary>
    public static void MoveMouse(int screenX, int screenY)
    {
        try
        {
            var mi = new MOUSEINPUT
            {
                dx = screenX,
                dy = screenY,
                dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            };

            var input = new INPUT { type = INPUT_MOUSE, mi = mi };
            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        }
        catch (Exception ex)
        {
            // Log error pero no falles
            System.Diagnostics.Debug.WriteLine($"MouseInputHelper.MoveMouse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Click izquierdo en coordenadas absolutas.
    /// </summary>
    public static void LeftClick(int screenX, int screenY)
    {
        try
        {
            var inputs = new INPUT[]
            {
                CreateMouseInput(screenX, screenY, MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE),
                CreateMouseInput(0, 0, MOUSEEVENTF_LEFTDOWN),
                CreateMouseInput(0, 0, MOUSEEVENTF_LEFTUP)
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MouseInputHelper.LeftClick error: {ex.Message}");
        }
    }

    /// <summary>
    /// Click derecho en coordenadas absolutas.
    /// </summary>
    public static void RightClick(int screenX, int screenY)
    {
        try
        {
            var inputs = new INPUT[]
            {
                CreateMouseInput(screenX, screenY, MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE),
                CreateMouseInput(0, 0, MOUSEEVENTF_RIGHTDOWN),
                CreateMouseInput(0, 0, MOUSEEVENTF_RIGHTUP)
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MouseInputHelper.RightClick error: {ex.Message}");
        }
    }

    /// <summary>
    /// Doble-click izquierdo.
    /// </summary>
    public static void DoubleClick(int screenX, int screenY)
    {
        LeftClick(screenX, screenY);
        System.Threading.Thread.Sleep(50);
        LeftClick(screenX, screenY);
    }

    // Helper privado
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
```

---

## 📋 PASO 2: Crear modelo `TouchInputRequest.cs`

**Ubicación:** `Controllers/TouchInputRequest.cs`

```csharp
namespace VirtualWebDisplay.Controllers;

/// <summary>
/// Representa un evento táctil enviado desde el cliente.
/// </summary>
public sealed class TouchInputRequest
{
    /// <summary>
    /// Tipo de evento: "touchstart", "touchmove", "touchend"
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Coordenada X relativa al viewport del navegador.
    /// </summary>
    public required double X { get; set; }

    /// <summary>
    /// Coordenada Y relativa al viewport del navegador.
    /// </summary>
    public required double Y { get; set; }

    /// <summary>
    /// Ancho del viewport en píxeles (para mapeo de coordenadas).
    /// </summary>
    public double ViewportWidth { get; set; } = 1.0;

    /// <summary>
    /// Alto del viewport en píxeles (para mapeo de coordenadas).
    /// </summary>
    public double ViewportHeight { get; set; } = 1.0;

    /// <summary>
    /// Número de dedos tocando la pantalla.
    /// 1 = click izquierdo, 2+ = click derecho
    /// </summary>
    public int Fingers { get; set; } = 1;

    /// <summary>
    /// Timestamp del evento (ms desde epoch).
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Identificador de sesión táctil (para tracking de toques).
    /// </summary>
    public string? SessionId { get; set; }
}
```

---

## 📋 PASO 3: Crear `InputHandler.cs`

**Ubicación:** `Controllers/Handlers/InputHandler.cs`

```csharp
using VirtualWebDisplay.Infrastructure;

namespace VirtualWebDisplay.Controllers.Handlers;

/// <summary>
/// Maneja entrada de usuario desde cliente (toques táctiles de tablet, etc).
/// Traduce eventos táctiles a clics de mouse en la pantalla virtual.
/// </summary>
internal static class InputHandler
{
    /// <summary>
    /// POST /input/touch - Recibe eventos táctiles y los convierte en clics de mouse.
    /// </summary>
    internal static IResult HandleTouchInput(
        HttpContext ctx,
        TouchInputRequest request,
        IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        // Validación básica
        if (request == null)
            return Results.BadRequest(new { error = "Request body required" });

        if (string.IsNullOrEmpty(request.Type))
            return Results.BadRequest(new { error = "Type field required" });

        // Resolver runtime y verificar autorización
        var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);

        if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
            return RuntimeAccessHelper.UnauthorizedResult(runtime);

        // Solo procesar si el modo de transmisión está habilitado
        // (Nota: funciona en ambos modos, pero podemos agregar validación si queremos)

        try
        {
            // Mapear coordenadas viewport → pantalla virtual
            var (screenX, screenY) = MapCoordinates(
                request.X,
                request.Y,
                request.ViewportWidth,
                request.ViewportHeight,
                runtime.Config.Width,
                runtime.Config.Height);

            // Procesar según tipo de evento táctil
            switch (request.Type.ToLowerInvariant())
            {
                case "touchstart":
                    ProcessTouchStart(screenX, screenY, request.Fingers);
                    break;

                case "touchmove":
                    ProcessTouchMove(screenX, screenY);
                    break;

                case "touchend":
                    // Generalmente no es necesario hacer nada en touchend
                    // pero aquí van lógicas de limpieza si es necesario
                    ProcessTouchEnd();
                    break;

                default:
                    return Results.BadRequest(new { error = $"Unknown event type: {request.Type}" });
            }

            return Results.Ok();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InputHandler error: {ex.Message}");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Mapea coordenadas del viewport del navegador a coordenadas de pantalla absoluta.
    /// Necesario porque la imagen mostrada puede estar escalada/rotada.
    /// </summary>
    private static (int screenX, int screenY) MapCoordinates(
        double viewportX,
        double viewportY,
        double viewportWidth,
        double viewportHeight,
        int screenWidth,
        int screenHeight)
    {
        // Normalizar a [0, 1]
        double normX = viewportWidth > 0 ? viewportX / viewportWidth : 0;
        double normY = viewportHeight > 0 ? viewportY / viewportHeight : 0;

        // Clamp a [0, 1]
        normX = Math.Clamp(normX, 0, 1);
        normY = Math.Clamp(normY, 0, 1);

        // Mapear a resolución de pantalla
        int screenX = (int)Math.Round(normX * screenWidth);
        int screenY = (int)Math.Round(normY * screenHeight);

        return (screenX, screenY);
    }

    /// <summary>
    /// Procesa evento touchstart: determina left-click (1 dedo) o right-click (2+ dedos).
    /// </summary>
    private static void ProcessTouchStart(int screenX, int screenY, int fingers)
    {
        if (fingers == 1)
        {
            // Un dedo = click izquierdo
            MouseInputHelper.LeftClick(screenX, screenY);
        }
        else if (fingers >= 2)
        {
            // Dos o más dedos = click derecho
            MouseInputHelper.RightClick(screenX, screenY);
        }

        System.Diagnostics.Debug.WriteLine(
            $"[InputHandler] Touch start: ({screenX}, {screenY}), fingers={fingers}");
    }

    /// <summary>
    /// Procesa evento touchmove: mueve el mouse sin hacer click.
    /// Útil para aplicaciones que responden a movimiento de mouse.
    /// </summary>
    private static void ProcessTouchMove(int screenX, int screenY)
    {
        MouseInputHelper.MoveMouse(screenX, screenY);

        // Log solo ocasionalmente para no llenar debug
        // System.Diagnostics.Debug.WriteLine($"[InputHandler] Touch move: ({screenX}, {screenY})");
    }

    /// <summary>
    /// Procesa evento touchend: limpieza si es necesaria.
    /// </summary>
    private static void ProcessTouchEnd()
    {
        // Por ahora no hacemos nada, pero aquí podría ir:
        // - Cancelar arrastres en progreso
        // - Resetear estado de gestos
        // - Logging de duración de toque, etc.
    }
}
```

---

## 📋 PASO 4: Registrar endpoint en `WebApiEndpoints.cs`

**Modificación de:** `Controllers/WebApiEndpoints.cs`

Agregar esta línea en el método `Map()`, después de los otros endpoints:

```csharp
app.MapPost("/input/touch", (HttpContext ctx, TouchInputRequest request) =>
    InputHandler.HandleTouchInput(ctx, request, runtimes));
```

Ejemplo completo de cómo se vería:

```csharp
public static void Map(
    WebApplication app,
    IReadOnlyList<ScreenRuntimeContext> runtimes,
    byte[] tlsCertDerBytes)
{
    app.MapPost("/auth/login", (HttpContext ctx, SecurityLoginRequest request) =>
        AuthHandler.HandleLogin(ctx, request, runtimes));

    app.MapGet("/", (HttpContext ctx) =>
        IndexHandler.HandleIndex(ctx, runtimes, _webImageTemplate, _rtcTemplate, _securityPageTemplate, _viewerLimitPageTemplate));

    app.MapGet("/cap", (HttpContext ctx) =>
        CaptureHandler.HandleCapture(ctx, runtimes));

    app.MapGet("/mjpeg", (HttpContext ctx) =>
        CaptureHandler.HandleMjpeg(ctx, runtimes));

    app.MapGet("/keepalive", (HttpContext ctx) =>
    {
        var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
        if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
            return RuntimeAccessHelper.UnauthorizedResult(runtime);

        return Results.NoContent();
    });

    app.MapPost("/webrtc/offer", (HttpContext ctx, WebRtcSessionOffer offer, CancellationToken ct) =>
        WebRtcHandler.HandleOffer(ctx, offer, runtimes, ct));

    // ← AGREGAR ESTA LÍNEA NUEVA:
    app.MapPost("/input/touch", (HttpContext ctx, TouchInputRequest request) =>
        InputHandler.HandleTouchInput(ctx, request, runtimes));

    app.MapGet("/cert", () =>
        Results.Bytes(tlsCertDerBytes, "application/x-x509-ca-cert", LocalCertificateProvider.CrtDownloadFileName));

    app.MapGet("/config", (HttpContext ctx) =>
    {
        var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);
        if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
            return RuntimeAccessHelper.UnauthorizedResult(runtime);

        return Results.Json(new
        {
            runtime.DisplayName,
            runtime.Config,
            runtime.HostUrl,
            runtime.IpUrl,
        });
    });
}
```

---

## 📋 PASO 5: Actualizar templates HTML

### Opción A: `WebImagePageTemplate.cs` (JPEG polling)

Agregar este JavaScript dentro del `<script>` existente, después de la función `next()`:

```javascript
// ────────────────────────────────────────────────────────────────
// TOUCH INPUT HANDLING (Virtual Mouse from Tablet)
// ────────────────────────────────────────────────────────────────

var touchInputEnabled = true; // Toggle para habilitar/deshabilitar
var lastTouchTime = 0;
var touchThrottle = 50; // ms mínimo entre eventos

document.addEventListener('touchstart', function(e) {
    if (!touchInputEnabled) return;
    
    var now = Date.now();
    if (now - lastTouchTime < touchThrottle) return;
    lastTouchTime = now;
    
    e.preventDefault(); // Prevenir scroll/zoom por toque
    
    var touch = e.touches[0];
    var screenRect = img.getBoundingClientRect(); // img es el elemento de imagen existente
    
    sendTouchInput({
        type: 'touchstart',
        x: touch.clientX - screenRect.left,
        y: touch.clientY - screenRect.top,
        viewportWidth: screenRect.width,
        viewportHeight: screenRect.height,
        fingers: e.touches.length,
        timestamp: now
    });
}, false);

document.addEventListener('touchmove', function(e) {
    if (!touchInputEnabled) return;
    
    var now = Date.now();
    if (now - lastTouchTime < touchThrottle) return;
    lastTouchTime = now;
    
    e.preventDefault();
    
    var touch = e.touches[0];
    var screenRect = img.getBoundingClientRect();
    
    sendTouchInput({
        type: 'touchmove',
        x: touch.clientX - screenRect.left,
        y: touch.clientY - screenRect.top,
        viewportWidth: screenRect.width,
        viewportHeight: screenRect.height,
        fingers: e.touches.length,
        timestamp: now
    });
}, false);

document.addEventListener('touchend', function(e) {
    if (!touchInputEnabled) return;
    
    sendTouchInput({
        type: 'touchend',
        timestamp: Date.now()
    });
}, false);

function sendTouchInput(data) {
    fetch('/input/touch', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
        keepalive: true
    }).catch(function(err) {
        console.error('Touch input error:', err);
    });
}
```

### Opción B: `RtcPageTemplate.cs` (WebRTC)

Agregar el mismo código JavaScript, pero reemplazar `img.getBoundingClientRect()` con `canvas.getBoundingClientRect()`:

```javascript
// ────────────────────────────────────────────────────────────────
// TOUCH INPUT HANDLING (Virtual Mouse from Tablet)
// ────────────────────────────────────────────────────────────────

var touchInputEnabled = true;
var lastTouchTime = 0;
var touchThrottle = 50; // ms

document.addEventListener('touchstart', function(e) {
    if (!touchInputEnabled) return;
    
    var now = Date.now();
    if (now - lastTouchTime < touchThrottle) return;
    lastTouchTime = now;
    
    e.preventDefault();
    
    var touch = e.touches[0];
    var screenRect = canvas.getBoundingClientRect(); // ← canvas en lugar de img
    
    sendTouchInput({
        type: 'touchstart',
        x: touch.clientX - screenRect.left,
        y: touch.clientY - screenRect.top,
        viewportWidth: screenRect.width,
        viewportHeight: screenRect.height,
        fingers: e.touches.length,
        timestamp: now
    });
}, false);

document.addEventListener('touchmove', function(e) {
    if (!touchInputEnabled) return;
    
    var now = Date.now();
    if (now - lastTouchTime < touchThrottle) return;
    lastTouchTime = now;
    
    e.preventDefault();
    
    var touch = e.touches[0];
    var screenRect = canvas.getBoundingClientRect();
    
    sendTouchInput({
        type: 'touchmove',
        x: touch.clientX - screenRect.left,
        y: touch.clientY - screenRect.top,
        viewportWidth: screenRect.width,
        viewportHeight: screenRect.height,
        fingers: e.touches.length,
        timestamp: now
    });
}, false);

document.addEventListener('touchend', function(e) {
    if (!touchInputEnabled) return;
    
    sendTouchInput({
        type: 'touchend',
        timestamp: Date.now()
    });
}, false);

function sendTouchInput(data) {
    fetch('/input/touch', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
        keepalive: true
    }).catch(function(err) {
        console.error('Touch input error:', err);
    });
}
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

- [ ] Crear `Infrastructure/MouseInputHelper.cs` con P/Invoke SendInput
- [ ] Crear `Controllers/TouchInputRequest.cs`
- [ ] Crear `Controllers/Handlers/InputHandler.cs`
- [ ] Agregar endpoint `/input/touch` en `WebApiEndpoints.cs`
- [ ] Actualizar `WebImagePageTemplate.cs` con event listeners
- [ ] Actualizar `RtcPageTemplate.cs` con event listeners
- [ ] Compilar y verificar no hay errores
- [ ] Probar con tablet en WebRTC (mejor experiencia)
- [ ] Probar con tablet en Web Image
- [ ] Verificar que mouse principal no se ve afectado
- [ ] Documentar en `/refactoring/PLAN.md`

---

## 🧪 TESTING MANUAL

1. **Compilar y ejecutar** VirtualWebDisplay
2. **Abrir navegador** en tablet a `https://[IP]:puerto/`
3. **Iniciar en WebRTC mode**
4. **Hacer toque de 1 dedo** → debe hacer click izquierdo
5. **Hacer toque de 2 dedos** → debe hacer click derecho
6. **Ver puntero del mouse del PC** → debe estar en la posición correcta
7. **Verificar aplicación en monitor virtual** → responde a los clicks

---

## 🐛 TROUBLESHOOTING

| Problema | Causa | Solución |
|----------|-------|----------|
| Coordenadas invertidas o corridas | Mapeo incorrecto | Verificar `MapCoordinates()` |
| Click no sucede | Puntero en posición incorrecta | Verificar resolución config vs real |
| Eventos táctiles no se envían | JS error | Check dev console (F12) de navegador |
| Acceso denegado | No autorizado | Verificar que tablet está autenticada |
| Lag notable | Latencia de red + polling | Usar WebRTC en lugar de Web Image |

---

## 📝 NOTAS IMPORTANTES

1. **SendInput NO requiere Admin** - Funciona en user mode normal
2. **Puntero sigue siendo UNO SOLO** - Esto es lo que quieres (no interferencia)
3. **Funciona en ambos modos** - WebRTC e Image Web idéntico
4. **Sin dependencias externas** - Solo Win32 APIs nativas
5. **Autorización requerida** - Misma que para ver la pantalla
6. **Throttling recomendado** - 50ms entre eventos evita saturación

---

## 🔒 CONSIDERACIONES DE SEGURIDAD

- ✅ Se valida autorización (mismo que captura)
- ✅ Se valida el runtime
- ✅ Se sanitizan coordenadas (clamp a rango válido)
- ✅ SendInput no requiere permisos especiales
- ⚠️ Si PC es Admin, apps elevated pueden bloquear entrada
- ⚠️ Solo se puede hacer desde máquina con pantalla virtual activa

---

## 🚀 PRÓXIMAS MEJORAS (OPCIONAL)

- Agregar toggle en UI para habilitar/deshabilitar
- Estadísticas de latencia de entrada
- Gestos multi-toque (3 dedos = alt-tab)
- Teclado virtual HTML5
- Soporte para stylus/pen
- Aceleración de movimiento (velocidad táctil)
