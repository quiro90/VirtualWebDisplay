# Mapeo por componentes

## `Program.cs`
**Rol:** composition root puro (~50 líneas, top-level statements).

### Responsabilidades
- inicializar `AppearanceSettingsStore` y aplicar cultura/idioma (`AppText.ApplyCulture`),
- cargar configuración con `VirtualScreenSettingsStore`,
- garantizar instancia única con `SingleInstanceManager`,
- detectar IP/hostname local con `NetworkAddressHelper`,
- instanciar `VirtualDisplayTrayController` y mostrar UI inicial,
- obtener certificado TLS local (`LocalCertificateProvider.GetOrCreate`),
- delegar el ciclo de vida completo a `ApplicationLifecycleManager.RunAsync(...)`.

### Ya NO contiene
- construcción de runtimes (→ `RuntimeFactory`),
- configuración de Kestrel (→ `KestrelConfigurator`),
- bucle while de stop/restart (→ `ApplicationLifecycleManager`),
- registro de endpoints HTTP (→ `WebApiEndpoints` + Handlers),
- P/Invoke de cursor (→ `CursorNativeMethods`).

---

## `Infrastructure/ApplicationLifecycleManager.cs`
**Rol:** gestionar el ciclo de vida completo de la aplicación (bucle principal).

### Responsabilidades
- iterar el bucle `while(keepRunning)`,
- llamar a `RuntimeFactory.TryCreate(...)` para construir runtimes,
- crear y configurar `WebApplication` vía `KestrelConfigurator`,
- arrancar runtimes con `RuntimeStartupHelper`,
- coordinar acciones del tray (exit / stop),
- registrar endpoints con `WebApiEndpoints.Map`,
- limpiar recursos con `RuntimeCleanupHelper` en el bloque `finally`,
- gestionar el flujo stop → esperar → reiniciar con recarga de apariencia.

---

## `Infrastructure/RuntimeFactory.cs`
**Rol:** construir la lista de `ScreenRuntimeContext` activos y verificar el driver.

### Responsabilidades
- `GetEnabledPorts(settings)` — verifica disponibilidad del driver Parsec VDD y devuelve los puertos habilitados; retorna `null` y muestra `InstallDialog` si el driver falta. Llamado **antes** de construir el DI container para que Kestrel se pueda configurar con puertos sin instanciar servicios.
- `TryCreate(settings, hostName, localIp, loggerFactory?)` — construye `ScreenRuntimeContext` para Screen1 y (si habilitada) Screen2, propagando `ILoggerFactory` a cada runtime.

---

## `Infrastructure/KestrelConfigurator.cs`
**Rol:** configurar puertos HTTP y HTTPS en Kestrel por runtime.

### API pública
- `Configure(builder, IReadOnlyList<ScreenRuntimeContext>, cert)` — overload de compatibilidad; extrae puertos del config de cada runtime.
- `Configure(builder, IReadOnlyList<int> ports, cert)` — overload primario; registra `ListenAnyIP(port)` y `ListenAnyIP(port+1, UseHttps)` por cada puerto. Usado desde `ApplicationLifecycleManager` antes de crear los runtimes completos.

---

## `Infrastructure/Interop/CursorNativeMethods.cs`
**Rol:** encapsular todos los P/Invoke relacionados con el cursor de Windows.

### Contiene
- structs `POINT`, `CURSORINFO`, `ICONINFO`,
- imports de `GetCursorInfo`, `GetIconInfo`, `DrawIcon`, `DestroyIcon`, `GetCursorPos`,
- constante `CURSOR_SHOWING`.

---

## `Controllers/WebApiEndpoints.cs`
**Rol:** orquestador de endpoints HTTP — registra rutas y delega en handlers.

### Endpoints registrados
| Ruta | Handler |
|---|---|
| `POST /auth/login` | `AuthHandler` |
| `GET /` | `IndexHandler` |
| `GET /cap`, `GET /mjpeg` | `CaptureHandler` |
| `POST /webrtc/offer` | `WebRtcHandler` |

---

## `Controllers/Handlers/`
**Rol:** un handler por grupo de endpoints, cada uno con responsabilidad única.

| Archivo | Responsabilidad |
|---|---|
| `AuthHandler.cs` | Validar clave y emitir cookie HTTP-only |
| `IndexHandler.cs` | Servir la página HTML raíz según modo (WebImage / Rtc) |
| `CaptureHandler.cs` | Servir frames JPEG (`/cap`) y stream MJPEG (`/mjpeg`) |
| `WebRtcHandler.cs` | Negociar oferta SDP WebRTC (`/webrtc/offer`) |

---

## `ScreenRuntimeContext.cs`
**Rol:** contenedor de runtime por pantalla virtual activa.

### Agrupa
- `VirtualScreenConfig` (configuración activa)
- `VirtualDisplayManager` (monitor Win32)
- `CaptureService` (captura JPEG)
- `WebRtcStreamService` (emisión WebRTC)
- `ViewerLimiter` (control de receptores simultáneos por pantalla)
- URLs de acceso (`HostUrl`, `IpUrl`)

### Constructor
Acepta `ILoggerFactory?` opcional. Si se provee, crea loggers tipados para `CaptureService` y `WebRtcStreamService`; si no, usa `NullLoggerFactory` (útil en tests).
- URLs de acceso (`HostUrl`, `IpUrl`)

### Métodos clave
- `StartAsync(CancellationToken)`: inicia captura y broadcaster WebRTC.
- `StopAsync()`: detiene servicios en orden seguro.
- `Dispose()` / `DisposeAsync()`: libera servicios y display virtual.

### Nota
Es la unidad operativa principal por pantalla. Si en el futuro se agrega una tercera pantalla, el patrón ya existe aquí.

`ViewerLimiter` resuelve el cupo total por pantalla combinando tres casos: polling de `WebImage`, conexiones persistentes `MJPEG` y peers `WebRTC`.

---

## `VirtualDisplayManager.cs`
**Rol:** crear, mantener, posicionar y destruir el monitor virtual de Windows.

### Responsabilidades
- verificar si `Parsec VDD` está instalado,
- abrir handle nativo al adaptador,
- agregar display virtual vía IOCTL `Add`,
- mantener vivo el driver con un loop de `Update`,
- identificar qué `Screen` nuevo creó Windows (`WindowsMonitorIndex`),
- aplicar resolución y posición relativa al monitor principal,
- eliminar el display al liberar recursos.

### URL de descarga del driver
`InstallUrl = "https://builds.parsec.app/vdd/parsec-vdd-0.45.0.0.exe"` (descarga directa del instalador oficial).

### API pública importante
- `VerifyDriverAvailability()` — static, verifica el driver antes de crear runtimes
- `TryCreate(VirtualScreenConfig)`
- `TryReconfigure(VirtualScreenConfig)`
- `WindowsMonitorIndex` — índice asignado por Windows al monitor creado
- `Dispose()`

### Detalles internos importantes
- usa `EnumDisplaySettings` y `ChangeDisplaySettingsEx` para topología/resolución,
- usa `VirtualDisplayPlacementOptions.GetPosition(...)` para calcular la posición,
- ajusta el `VirtualScreenConfig` a la resolución realmente soportada por el driver,
- contiene `DriverApi`, que encapsula IOCTLs `Add`, `Remove` y `Update`.

### Cuándo tocar este archivo
- problemas al crear el monitor virtual,
- nueva lógica de colocación respecto al monitor principal,
- compatibilidad con otros modos/resoluciones del driver.

---

## `CaptureService.cs`
**Rol:** capturar periódicamente el monitor configurado y convertirlo a JPEG.

### Responsabilidades
- resolver qué monitor capturar (`MonitorIndex`),
- copiar la pantalla con `Graphics.CopyFromScreen`,
- dibujar el cursor encima si está visible (via `CursorNativeMethods`),
- rotar si `StreamRotationDegrees` está configurado,
- codificar a JPEG con la calidad configurada (`JpegQuality`),
- respetar el intervalo de captura (`CaptureIntervalSeconds`),
- guardar en memoria el último frame.

### API pública importante
- `GetCurrentFrame()` — devuelve el último JPEG capturado como `byte[]`
- `ExecuteAsync(CancellationToken)` — loop principal (hereda de `BackgroundService`)

### Notas
- hereda de `BackgroundService`,
- recibe `ILogger<CaptureService>` en el constructor (propagado desde `ILoggerFactory` vía `ScreenRuntimeContext`),
- errores de captura se registran con `LogWarning` incluyendo el `MonitorIndex` afectado; el loop continúa (errores transitorios esperados: pantalla bloqueada, monitor desconectado),
- todo el P/Invoke de cursor está delegado a `Infrastructure/Interop/CursorNativeMethods.cs`,
- comparte el último frame por referencia para que `/cap`, `/mjpeg` y `WebRtcStreamService` reutilicen la misma captura sin duplicar memoria,
- tanto `WebImage` como `Rtc` usan los mismos parámetros `CaptureIntervalSeconds` y `JpegQuality`.

---

## `WebRtcStreamService.cs`
**Rol:** negociar peers WebRTC y enviar frames JPEG fragmentados por `RTCDataChannel`.

### Responsabilidades
- crear respuestas SDP (`CreateAnswerAsync`),
- registrar peers activos,
- detectar cierre/desconexión y limpiar peers,
- tomar el último JPEG disponible desde `CaptureService`,
- enviar metadata JSON + chunks binarios al cliente.

### API pública importante
- `CreateAnswerAsync(WebRtcSessionOffer, CancellationToken)`
- `ExecuteAsync(CancellationToken)`
- `StopAsync(CancellationToken)`
- `DisposeAsync()`

### Protocolo interno de frame
1. mensaje texto: `{"type":"frame","size":N}`
2. luego múltiples chunks binarios de hasta `16 KB`
3. el cliente JS rearma el JPEG y actualiza la imagen visible

---

## `VirtualDisplayTrayController.cs`
**Rol:** gestionar el tray icon y coordinar la UI de configuración.

### Responsabilidades
- ejecutar el bucle de UI de Windows Forms en un hilo STA dedicado,
- mostrar el formulario de configuración inicial (`ShowStartupConfiguration`),
- coordinar acciones de runtime (exit / stop) vía `ConfigureRuntimeActions`,
- delegar construcción del menú contextual a `TrayMenuBuilder`,
- delegar la presentación del formulario de configuración a `ConfigurationFormPresenter`,
- emitir notificaciones balloon tip al arrancar/detener.

### Clases colaboradoras (extraídas)
| Clase | Responsabilidad |
|---|---|
| `UI/TrayIcon/TrayMenuBuilder.cs` | `Build(...)` estático — construye el `ContextMenuStrip` |
| `UI/TrayIcon/ConfigurationFormPresenter.cs` | Abrir/cerrar `ResolutionConfigurationForm`, notificaciones |

### Métodos clave
- `ConfigureRuntimeActions(Action exit, Action stop, IReadOnlyList<ScreenRuntimeContext>)`
- `NotifyServiceStopped()` / `WaitForServiceStartAsync()`
- `PostToUi(Action)` — despacha al hilo STA seguro

---

## `ResolutionConfigurationForm.cs`
**Rol:** formulario principal de configuración de pantallas.

### Comportamiento al correr el servicio
Cuando el servicio está activo (`_wasStarted = true`), `SetConfigurationControlsLocked(true)` deshabilita todos los controles de configuración de ambas tabs excepto el botón de configuración de Windows. Esto garantiza que el usuario no pueda cambiar resolución/placement/modo mientras el driver VDD está en uso.

- `SetConfigurationControlsLocked(bool)` — deshabilita `_enableScreen2Check` y llama `SetServiceRunning` en ambos `ScreenTabControls`
- Al recibir `NotifyServiceStarted` → bloquea; al recibir `NotifyServiceStopped` → desbloquea
- El menú de configuración incluye la opción **"Resoluciones personalizadas..."** que abre `CustomModesDialog`

---

## `ScreenTabControls.cs`
**Rol:** controles de configuración para una pestaña de pantalla.

### Bloqueo mientras el servicio corre
`SetServiceRunning(bool running)` bloquea/desbloquea todos los controles gestionados (`_managedControls`) mientras el servicio está activo. Solo el botón de configuración de Windows (`_windowsDisplayButton`) permanece habilitado.

---

## `CustomModesDialog.cs`
**Rol:** diálogo para editar las resoluciones personalizadas del driver Parsec VDD.

### Descripción
Permite configurar hasta **5 slots** de resolución personalizada (ancho × alto @ Hz) que se escriben en el registro de Windows bajo `HKLM\SOFTWARE\Parsec\vdd\{0..4}`.

### Flujo UAC
- Si el proceso actual **no** es administrador → relanza la app con `--set-custom-modes "<data>"` vía `Process.Start` con `Verb="runas"`, para elevar permisos solo en esa operación.
- Si ya es administrador → escribe directamente al registro.
- En ambos casos de éxito, muestra confirmación y cierra el diálogo.

### Panel de advertencia
Muestra un panel amarillo advirtiendo que se necesita reiniciar el driver para aplicar los cambios. El panel usa `Tag = "preserve-color"` para que `FormThemeApplicator.ApplyThemeRecursive` no sobreescriba su `BackColor`.

---

## `VddCustomModesStore.cs`
**Rol:** leer y escribir los modos de resolución personalizados del driver Parsec VDD en el registro de Windows.

### Registro
- Ruta: `HKLM\SOFTWARE\Parsec\vdd\{0..4}` (5 slots, índice 0-4)
- Valores por slot: `width` (DWORD), `height` (DWORD), `hz` (DWORD)

### API
- `Read()` → `List<CustomMode>` (no lanza excepciones; slot vacío = `CustomMode(0,0,0)`)
- `Write(List<CustomMode>)` → puede lanzar `UnauthorizedAccessException` si no es admin
- `IsAdmin()` → `bool`

### `CustomMode`
```csharp
record CustomMode(int Width, int Height, int Hz);
```
Un slot con todos los valores en 0 se considera vacío (no se envía al driver).

---

### Responsabilidades
- ejecutar el bucle de UI de Windows Forms en un hilo STA dedicado,
- mostrar el formulario de configuración inicial (`ShowStartupConfiguration`),
- coordinar acciones de runtime (exit / stop) vía `ConfigureRuntimeActions`,
- delegar construcción del menú contextual a `TrayMenuBuilder`,
- delegar la presentación del formulario de configuración a `ConfigurationFormPresenter`,
- emitir notificaciones balloon tip al arrancar/detener.

### Clases colaboradoras (extraídas)
| Clase | Responsabilidad |
|---|---|
| `UI/TrayIcon/TrayMenuBuilder.cs` | `Build(...)` estático — construye el `ContextMenuStrip` |
| `UI/TrayIcon/ConfigurationFormPresenter.cs` | Abrir/cerrar `ResolutionConfigurationForm`, notificaciones |

### Métodos clave
- `ConfigureRuntimeActions(Action exit, Action stop, IReadOnlyList<ScreenRuntimeContext>)`
- `NotifyServiceStopped()` / `WaitForServiceStartAsync()`
- `PostToUi(Action)` — despacha al hilo STA seguro

---

## `VirtualScreenSettingsStore.cs`
**Rol:** cargar y guardar la configuración del usuario.

### Detalles
- directorio: `%USERPROFILE%\.virtualwebdisplay`
- archivo: `virtualscreen.user.json`
- soporta migración desde formato legado (campo `VirtualScreen` -> `Screen1` + `Screen2` defaults)
- `CreateDefaults()` centraliza los valores por defecto para evitar inconsistencias

---

## `VirtualWebDisplaySettings.cs`
**Rol:** objeto raíz de configuración.

### Estructura
- `Screen1`: `VirtualScreenConfig` (siempre habilitada, puerto 8000 por defecto)
- `Screen2`: `VirtualScreenConfig` (deshabilitada por defecto, puerto 8001)
- `EnsureValid()`: normaliza puertos, profiles y modos; garantiza que Screen1.Enabled=true

---

## `VirtualScreenConfig.cs`
**Rol:** configuración completa de una pantalla virtual.

### Campos clave
| Campo | Tipo | Descripción |
|---|---|---|
| `Enabled` | bool | Si se crea esta pantalla |
| `Profile` | string | Id del perfil de resolución |
| `CustomWidth`/`CustomHeight` | int | Resolución cuando Profile = Custom |
| `Width`/`Height` | int | Resolución efectiva final |
| `Port` | int | Puerto HTTP de esta pantalla |
| `TransmissionMethod` | string | `WebImage` o `Rtc` |
| `CaptureIntervalSeconds` | double | Intervalo de captura (compartido por ambos modos) |
| `JpegQuality` | int | Calidad JPEG 10-100 (compartido por ambos modos) |
| `RotateForPortrait` | bool | Rota el bitmap 90° antes de codificar |
| `MonitorIndex` | int | -1=auto, 0=primario, 1+=otros |
| `VirtualDisplayPlacement` | string | right/left/top/bottom |
| `BrowserImageFit` | string | fill/cover/contain |

---

## `VirtualDisplayProfiles.cs`
**Rol:** catálogo de resoluciones predefinidas.

### Perfiles disponibles
Todos se almacenan en portrait; la app rota si `Landscape=true`.
Incluyen resoluciones desde 720×1280 hasta 1200×1920, más `Custom`.
La resolución recomendada es **1080×1920**.

### API relevante
- `VirtualDisplayProfiles.All` — lista completa de perfiles
- `EnsureValidSelection(VirtualScreenConfig)` — normaliza profile, landscape y dimensiones
- `GetEffectiveSize(profileId, landscape, customW, customH)` — calcula `Width`/`Height` finales
- `IsCustom(profileId)`

---

## `TransmissionModeOptions.cs`
**Rol:** constantes y validación de modos de transmisión.

### Constantes
- `WebImage` = `"WebImage"`
- `Rtc` = `"Rtc"`

### Nota importante
Ambos modos comparten `CaptureIntervalSeconds` y `JpegQuality` de `VirtualScreenConfig`.

---

## `VirtualDisplayPlacementOptions.cs`
**Rol:** centralizar normalización y cálculo de posición del monitor virtual.

### API
- `Normalize(string?)` — acepta español e inglés, devuelve right/left/top/bottom
- `GetDisplayLabel(string?)` — etiqueta en español
- `GetPosition(Rectangle primaryBounds, string?, int width, int height)` — coordenadas Win32

---

## `NetworkAddressHelper.cs`
**Rol:** detección de IP local y construcción de URLs de acceso.

### API
- `DetectLocalIp()`
- `BuildAccessUrl(string host, int port)`

---

## `SingleInstanceManager.cs`
**Rol:** garantizar que solo haya una instancia activa; permitir que una nueva instancia cierre la anterior.

### API
- `CreateForCurrentExecutable()`
- `EnsureSingleInstance(TimeSpan timeout)`
- `StartShutdownListener(Action onShutdownRequested)`
