# 🛠️ Guía de Desarrollo - VirtualWebDisplay

## 📋 Tabla de Contenidos

1. [Requisitos Previos](#requisitos-previos)
2. [Configuración del Entorno](#configuración-del-entorno)
3. [Estructura del Proyecto](#estructura-del-proyecto)
4. [Compilación](#compilación)
5. [Ejecución y Depuración](#ejecución-y-depuración)
6. [Agregar Nuevas Características](#agregar-nuevas-características)
7. [Convenciones de Código](#convenciones-de-código)
8. [Testing](#testing)
9. [Debugging Tips](#debugging-tips)
10. [Problemas Comunes](#problemas-comunes)

---

## Requisitos Previos

### Software Necesario

1. **Windows 10/11** (64-bit)
   - Requerido para Parsec VDD driver y WinForms

2. **.NET 10 SDK**
   - Descargar de: https://dotnet.microsoft.com/download/dotnet/10.0
   - Verificar instalación:
     ```powershell
     dotnet --version
     # Debe mostrar: 10.x.x
     ```

3. **Visual Studio 2026** (recomendado) o **Visual Studio Code**
   - VS 2026 Enterprise/Professional/Community
   - Workload: ".NET desktop development"
   - O VS Code con extensión C# Dev Kit

4. **Parsec Virtual Display Driver**
   - Descargar de: https://github.com/nomi-san/parsec-vdd/releases
   - Instalar ejecutando `installdriver.bat` como administrador
   - Verificar instalación:
     ```powershell
     # En Device Manager debería aparecer:
     # Display adapters > Parsec Virtual Display Adapter
     ```

### Dependencias del Proyecto

Gestionadas automáticamente por NuGet:
- `SIPSorcery` (WebRTC)
- `Microsoft.AspNetCore.App` (Kestrel, incluido en SDK)
- `System.Drawing.Common` (captura de pantalla)

---

## Configuración del Entorno

### 1. Clonar el Repositorio

```powershell
git clone https://github.com/quiro90/VirtualWebDisplay.git
cd VirtualWebDisplay
```

### 2. Restaurar Dependencias

```powershell
dotnet restore VirtualWebDisplay_Parsec.csproj
```

### 3. Verificar Estructura

Asegurarse que la estructura de carpetas sea:

```
VirtualWebDisplay_Parsec/
├── UI/
│   ├── TrayIcon/
│   │   └── VirtualDisplayTrayController.cs
│   ├── Forms/
│   │   ├── ResolutionConfigurationForm.cs
│   │   ├── ScreenTabControls.cs
│   │   └── InstallDialog.cs
│   └── HtmlTemplates/
│       ├── IHtmlTemplate.cs
│       ├── WebImagePageTemplate.cs
│       ├── RtcPageTemplate.cs
│       ├── SecurityPageTemplate.cs              ← Fase 2
│       └── ViewerLimitPageTemplate.cs           ← Fase 2
├── Configuration/
│   ├── Models/
│   │   ├── VirtualScreenConfig.cs
│   │   └── VirtualWebDisplaySettings.cs
│   ├── VirtualScreenSettingsStore.cs
│   ├── VirtualDisplayProfiles.cs
│   ├── TransmissionModeOptions.cs
│   └── VirtualDisplayPlacementOptions.cs
├── Parsec/
│   └── VirtualDisplayManager.cs
├── Streaming/
│   ├── Models/
│   │   ├── WebRtcSessionOffer.cs
│   │   └── WebRtcSessionAnswer.cs
│   ├── CaptureService.cs
│   └── WebRtcStreamService.cs
├── Infrastructure/
│   ├── ScreenRuntimeContext.cs
│   ├── NetworkAddressHelper.cs
│   ├── LocalCertificateProvider.cs
│   ├── SingleInstanceManager.cs
│   ├── RuntimeAccessHelper.cs                  ← Fase 2
│   └── RuntimeCleanupHelper.cs                 ← Fase 2
├── Controllers/
│   └── SecurityLoginRequest.cs                 ← Fase 2
├── Program.cs
└── VirtualWebDisplay_Parsec.csproj
```

---

## Estructura del Proyecto

### Organización de Namespaces

```
VirtualWebDisplay                      (raíz - solo Program.cs)
├── VirtualWebDisplay.UI.TrayIcon      (gestión de tray icon)
├── VirtualWebDisplay.UI.Forms         (formularios WinForms)
├── VirtualWebDisplay.UI.HtmlTemplates (templates HTML)
├── VirtualWebDisplay.Configuration    (persistencia)
├── VirtualWebDisplay.Configuration.Models (modelos de config)
├── VirtualWebDisplay.Parsec           (interfaz con driver VDD)
├── VirtualWebDisplay.Streaming        (captura y transmisión)
├── VirtualWebDisplay.Streaming.Models (DTOs de streaming)
└── VirtualWebDisplay.Infrastructure   (servicios transversales)
```

### Capas de Arquitectura

Ver **ARCHITECTURE.md** para diagramas detallados.

- **UI Layer**: Interfaz gráfica (tray, formularios, templates HTML)
- **Web Layer**: Servidor HTTP/HTTPS (Program.cs, Kestrel)
- **Configuration Layer**: Gestión de configuración (JSON)
- **Streaming Layer**: Captura de pantalla y transmisión (JPEG, WebRTC)
- **Parsec Layer**: Interfaz con driver de pantalla virtual
- **Infrastructure Layer**: Servicios compartidos (certificados, red, runtime)

---

## Compilación

### Desde Visual Studio

1. Abrir `VirtualWebDisplay_Parsec.sln`
2. Configurar:
   - Platform: `x64` o `Any CPU`
   - Configuration: `Debug` o `Release`
3. Build > Build Solution (`Ctrl+Shift+B`)

### Desde Línea de Comandos

```powershell
# Debug
dotnet build VirtualWebDisplay_Parsec.csproj

# Release
dotnet build VirtualWebDisplay_Parsec.csproj --configuration Release

# Publicar (single-file executable)
dotnet publish VirtualWebDisplay_Parsec.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output ./publish `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true
```

**Output**:
- Debug: `bin\Debug\net10.0-windows\VirtualWebDisplay_Parsec.exe`
- Release: `bin\Release\net10.0-windows\VirtualWebDisplay_Parsec.exe`
- Publish: `publish\VirtualWebDisplay_Parsec.exe` (ejecutable independiente)

---

## Ejecución y Depuración

### Ejecutar desde Visual Studio

1. Presionar `F5` (Start Debugging) o `Ctrl+F5` (Start Without Debugging)
2. Verificar:
   - Tray icon aparece en bandeja del sistema
   - Console muestra:
     ```
     info: Microsoft.Hosting.Lifetime[14]
           Now listening on: http://0.0.0.0:5000
     info: Microsoft.Hosting.Lifetime[14]
           Now listening on: https://0.0.0.0:5001
     ```
3. Abrir navegador: `https://localhost:5001`

### Ejecutar desde Línea de Comandos

```powershell
# Desde carpeta del proyecto
dotnet run --project VirtualWebDisplay_Parsec.csproj

# O ejecutar directamente el .exe
.\bin\Debug\net10.0-windows\VirtualWebDisplay_Parsec.exe
```

### Configuración de Launch (Visual Studio)

Archivo: `Properties\launchSettings.json`

```json
{
  "profiles": {
    "VirtualWebDisplay_Parsec": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Breakpoints Recomendados

**Para debugging de inicio**:
- `Program.cs` línea ~30: Después de `LoadSettings()`
- `Program.cs` línea ~60: Antes de `CreateRuntimeAsync()`

**Para debugging de captura**:
- `CaptureService.cs` línea ~80: Inicio de loop de captura
- `CaptureService.cs` línea ~120: Codificación JPEG

**Para debugging de WebRTC**:
- `WebRtcStreamService.cs` línea ~40: `CreateAnswerAsync()`
- `WebRtcStreamService.cs` línea ~150: Envío de frames

---

## Agregar Nuevas Características

### Ejemplo 1: Agregar Nuevo Perfil de Resolución

**Archivo**: `Configuration/VirtualDisplayProfiles.cs`

```csharp
public static class VirtualDisplayProfiles
{
    public static readonly (int Width, int Height)[] Resolutions = new[]
    {
        (1920, 1080),   // Full HD
        (2560, 1440),   // 2K
        (3840, 2160),   // 4K
        (5120, 2880),   // 5K <-- NUEVO
    };
}
```

**Actualizar UI**: `UI/Forms/ScreenTabControls.cs`

```csharp
private void PopulateResolutionComboBox()
{
    foreach (var (width, height) in VirtualDisplayProfiles.Resolutions)
    {
        resolutionComboBox.Items.Add($"{width}x{height}");
    }
}
```

### Ejemplo 2: Agregar Endpoint HTTP Personalizado

**Archivo**: `Program.cs`

Agregar después de otros `MapGet`:

```csharp
app.MapGet("/stats", async (HttpContext context) =>
{
    var stats = new
    {
        Uptime = DateTime.Now - _startTime,
        ActiveScreens = runtimes.Count,
        TotalFramesCaptured = _totalFrames
    };

    await context.Response.WriteAsJsonAsync(stats);
});
```

### Ejemplo 3: Agregar Opción de Configuración

**1. Actualizar Modelo**: `Configuration/Models/VirtualScreenConfig.cs`

```csharp
public record VirtualScreenConfig
{
    // Propiedades existentes...

    public bool EnableAudio { get; init; } = false; // <-- NUEVA
}
```

**2. Actualizar UI**: `UI/Forms/ScreenTabControls.cs`

```csharp
private CheckBox enableAudioCheckBox;

private void InitializeControls()
{
    enableAudioCheckBox = new CheckBox
    {
        Text = "Enable Audio Streaming",
        Location = new Point(10, 220),
        AutoSize = true
    };
    Controls.Add(enableAudioCheckBox);
}

public VirtualScreenConfig GetConfiguration()
{
    return new VirtualScreenConfig
    {
        // Propiedades existentes...
        EnableAudio = enableAudioCheckBox.Checked // <-- NUEVO
    };
}
```

**3. Implementar Funcionalidad**:

Crear `Streaming/AudioCaptureService.cs` (similar a `CaptureService.cs`).

### Ejemplo 4: Agregar Modo de Transmisión H.264

Ver sección "Extensibilidad" en **ARCHITECTURE.md**.

---

## Convenciones de Código

### Naming Conventions

- **Clases**: PascalCase (`VirtualDisplayManager`)
- **Métodos**: PascalCase (`CreateRuntimeAsync`)
- **Propiedades**: PascalCase (`CaptureIntervalMs`)
- **Parámetros**: camelCase (`screenConfig`)
- **Campos privados**: camelCase con `_` prefix (`_captureService`)
- **Constantes**: PascalCase (`DefaultJpegQuality`)

### Organización de Archivos

```csharp
// 1. Using statements (agrupados y ordenados)
using System;
using System.Threading.Tasks;
using VirtualWebDisplay.Configuration;

// 2. Namespace (file-scoped en .NET 10)
namespace VirtualWebDisplay.Streaming;

// 3. Clase principal
public class CaptureService : BackgroundService
{
    // 4. Campos privados
    private readonly VirtualScreenConfig _config;

    // 5. Constructor
    public CaptureService(VirtualScreenConfig config)
    {
        _config = config;
    }

    // 6. Métodos públicos
    public byte[] GetLatestFrame() { ... }

    // 7. Métodos protegidos/override
    protected override async Task ExecuteAsync(CancellationToken token) { ... }

    // 8. Métodos privados
    private void EncodeToJpeg() { ... }
}
```

### Async/Await

- **Siempre** usar `async`/`await` para operaciones I/O
- Nombrar métodos async con sufijo `Async` (`CreateAnswerAsync`)
- Propagar `CancellationToken` en operaciones largas

```csharp
// ✅ Correcto
public async Task<string> LoadDataAsync(CancellationToken cancellationToken)
{
    await Task.Delay(1000, cancellationToken);
    return "data";
}

// ❌ Incorrecto
public async Task<string> LoadData()  // Falta sufijo "Async"
{
    Thread.Sleep(1000);  // No usar Thread.Sleep en código async
    return "data";
}
```

### Disposable Pattern

Implementar `IDisposable` o `IAsyncDisposable` para clases con recursos no administrados:

```csharp
public class MyResource : IAsyncDisposable
{
    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // Cleanup asíncrono
        await CleanupAsync();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
```

### Records vs Classes

- **Records**: Para DTOs inmutables (`VirtualScreenConfig`, `WebRtcSessionOffer`)
- **Classes**: Para objetos con comportamiento mutable (`CaptureService`, `VirtualDisplayManager`)

```csharp
// ✅ Record para DTO
public record VirtualScreenConfig
{
    public int Width { get; init; }
    public int Height { get; init; }
}

// ✅ Class para servicio con estado
public class CaptureService : BackgroundService
{
    private byte[] _latestFrame;
    // ...
}
```

### Comentarios

- **Evitar** comentarios obvios
- **Usar** XML comments para APIs públicas
- **Usar** comentarios `//` para explicar lógica compleja

```csharp
/// <summary>
/// Crea una conexión WebRTC y genera un SDP answer.
/// </summary>
/// <param name="offer">SDP offer del cliente</param>
/// <returns>SDP answer para completar negociación</returns>
public async Task<WebRtcSessionAnswer> CreateAnswerAsync(WebRtcSessionOffer offer)
{
    // Configurar DataChannel con baja latencia (sin ordenamiento ni retransmisión)
    var dataChannel = await peerConnection.createDataChannel("frames", new RTCDataChannelInit
    {
        ordered = false,       // No esperar orden de paquetes
        maxRetransmits = 0     // No retransmitir paquetes perdidos
    });

    // ...
}
```

---

## Testing

### Unit Tests (Pendiente)

Framework recomendado: **xUnit** + **Moq**

Estructura de proyecto de tests:

```
VirtualWebDisplay.Tests/
├── UI/
│   └── TrayIcon/
│       └── VirtualDisplayTrayControllerTests.cs
├── Configuration/
│   └── VirtualScreenSettingsStoreTests.cs
├── Streaming/
│   ├── CaptureServiceTests.cs
│   └── WebRtcStreamServiceTests.cs
└── Infrastructure/
    └── NetworkAddressHelperTests.cs
```

**Ejemplo de Test**:

```csharp
public class VirtualScreenSettingsStoreTests
{
    [Fact]
    public void LoadSettings_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        // Arrange
        var store = new VirtualScreenSettingsStore();

        // Act
        var settings = store.LoadSettings();

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.Screen1);
        Assert.Equal(1920, settings.Screen1.Width);
    }
}
```

### Integration Tests

Verificar flujo completo:

```csharp
[Fact]
public async Task EndToEnd_CreateVirtualDisplay_CaptureFrame_Success()
{
    // Arrange
    var config = VirtualScreenConfig.GetDefault();
    var context = await ScreenRuntimeContext.CreateRuntimeAsync(config);

    // Act
    await context.StartAsync();
    await Task.Delay(200); // Esperar primera captura
    var frame = context.CaptureService.GetLatestFrame();

    // Assert
    Assert.NotNull(frame);
    Assert.True(frame.Length > 0);

    // Cleanup
    await context.DisposeAsync();
}
```

### Manual Testing

**Checklist**:

1. ✅ Aplicación inicia sin errores
2. ✅ Tray icon aparece
3. ✅ Abrir "Configuración" muestra formulario
4. ✅ Cambiar resolución y aplicar → pantalla virtual se recrea
5. ✅ Navegar a `https://localhost:5001` muestra página web
6. ✅ Modo Web Image: imagen se actualiza periódicamente
7. ✅ Modo WebRTC: video en tiempo real sin retraso notable
8. ✅ Mover cursor en pantalla virtual → visible en stream
9. ✅ Cerrar aplicación → pantalla virtual desaparece

---

## Debugging Tips

### 1. Pantalla Virtual No Se Crea

**Problema**: `VirtualDisplayManager.TryCreate()` retorna `null`.

**Diagnóstico**:
- Verificar que Parsec VDD está instalado:
  ```powershell
  # Debe existir:
  C:\Windows\System32\drivers\parsecvdd.sys
  ```
- Revisar Device Manager > Display adapters
- Ejecutar como Administrador (a veces requerido)

**Solución**:
- Reinstalar Parsec VDD
- Reiniciar Windows después de instalación

---

### 2. WebRTC No Conecta

**Problema**: Browser muestra "Connecting..." indefinidamente.

**Diagnóstico**:
- Abrir DevTools (`F12`) > Console, buscar errores
- Verificar que HTTPS está habilitado (puerto configurado + 1)
- Revisar que certificado SSL fue generado:
  ```powershell
  # Debe existir:
  C:\Users\<Usuario>\.virtualwebdisplay\localhost.pfx
  ```

**Solución**:
1. Descargar e instalar certificado:
   - Navegar a `https://localhost:5001/cert`
   - Guardar `localhost.cer`
   - Doble click > Install Certificate
   - Store Location: "Local Machine"
   - Place in: "Trusted Root Certification Authorities"

2. Verificar firewall no bloquea puerto HTTPS

---

### 3. Frames No Se Actualizan

**Problema**: Imagen en navegador permanece estática.

**Diagnóstico**:
- Poner breakpoint en `CaptureService.ExecuteAsync` línea ~120
- Verificar que `_hasFrameChanged` es `true`
- Revisar configuración `CaptureIntervalMs` no sea extremadamente alta

**Solución**:
- Reducir `CaptureIntervalMs` a 50ms (default)
- Verificar que pantalla virtual tiene contenido visible (no pantalla negra)
- Probar deshabilitar "Detect Changes" temporalmente (forzar codificación siempre)

---

### 4. High CPU Usage

**Problema**: Aplicación consume ~30-50% CPU.

**Diagnóstico**:
- Captura de pantalla es operación intensiva
- Resoluciones altas (4K) requieren más procesamiento
- Calidad JPEG alta (95-100) es más lenta

**Solución**:
- Reducir resolución a 1920x1080 o 1280x720
- Aumentar `CaptureIntervalMs` a 100-200ms (10-5 FPS)
- Reducir `JpegQuality` a 60-70
- En modo WebRTC, considerar H.264 hardware encoding (requiere desarrollo adicional)

---

### 5. Mutex Error al Iniciar

**Problema**: "Another instance is already running".

**Causa**: Instancia previa no cerró correctamente (mutex no liberado).

**Solución**:
```powershell
# Terminar proceso manualmente
taskkill /F /IM VirtualWebDisplay_Parsec.exe

# O reiniciar Windows (libera todos los mutex)
```

---

## Problemas Comunes

### Compilación Fallida

**Error**: `The type or namespace name 'X' could not be found`

**Solución**:
- Verificar que todas las dependencias están instaladas:
  ```powershell
  dotnet restore VirtualWebDisplay_Parsec.csproj
  ```
- Limpiar y recompilar:
  ```powershell
  dotnet clean
  dotnet build
  ```

---

### WinForms Designer No Carga

**Error**: "The designer could not be shown for this file because none of the classes within it can be designed"

**Solución**:
- Abrir formulario en modo código (click derecho > View Code)
- Verificar que clase hereda de `Form` o `UserControl`
- Verificar que constructor no tiene lógica compleja (puede fallar en design time)

---

### Certificado SSL Rechazado en Chrome

**Error**: "NET::ERR_CERT_AUTHORITY_INVALID"

**Solución**:
1. Descargar certificado: `https://localhost:5001/cert`
2. Instalar en "Trusted Root Certification Authorities"
3. Reiniciar Chrome
4. Verificar en Chrome: `chrome://settings/certificates` > "Authorities" > buscar "localhost"

---

### Pantalla Virtual Parpadea o Desaparece

**Problema**: Pantalla virtual se muestra intermitentemente en Windows.

**Causa**: Keep-alive loop de `VirtualDisplayManager` no está ejecutándose.

**Solución**:
- Verificar que `VirtualDisplayManager.Update()` se llama cada 100ms
- No bloquear thread de update con operaciones largas
- Si es necesario pausar, llamar `Update()` manualmente

---

## Recursos Adicionales

- **AGENT.md**: Contexto técnico completo para IA
- **ARCHITECTURE.md**: Diagramas y decisiones de diseño
- **docs/TROUBLESHOOTING.md**: Problemas y soluciones detalladas
- **docs/CONFIGURATION.md**: Estructura del archivo de configuración

### Documentación Externa

- [.NET 10 Documentation](https://docs.microsoft.com/dotnet/)
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core/)
- [SIPSorcery WebRTC](https://github.com/sipsorcery-org/sipsorcery)
- [Parsec VDD](https://github.com/nomi-san/parsec-vdd)

---

## Contacto y Contribución

Para preguntas o sugerencias:
- **GitHub Issues**: https://github.com/quiro90/VirtualWebDisplay/issues
- **Pull Requests**: Bienvenidos (seguir convenciones de este documento)

¡Happy Coding! 🚀
