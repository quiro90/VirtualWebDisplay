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
- construir `ScreenRuntimeContext` para Screen1 y (si está habilitada) Screen2,
- verificar que el driver Parsec VDD está instalado cuando se requieren pantallas no duplicadas,
- mostrar `InstallDialog` y retornar `null` si el driver falta.

---

## `Infrastructure/KestrelConfigurator.cs`
**Rol:** configurar puertos HTTP y HTTPS en Kestrel por runtime.

### API pública
- `Configure(WebApplicationBuilder builder, IReadOnlyList<ScreenRuntimeContext> runtimes, X509Certificate2 tlsCert)` — registra `ListenAnyIP(port)` y `ListenAnyIP(port+1, UseHttps)` por cada runtime.

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
