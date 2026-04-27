# Mapeo por componentes

## `Program.cs`
**Rol:** composición y arranque de toda la aplicación.

### Responsabilidades
- garantizar instancia única con `SingleInstanceManager`,
- cargar configuración con `VirtualScreenSettingsStore`,
- abrir la UI inicial del tray (`VirtualDisplayTrayController`),
- detectar IP/hostname local con `NetworkAddressHelper`,
- verificar disponibilidad del driver virtual (`VirtualDisplayManager.VerifyDriverAvailability()`),
- crear `ScreenRuntimeContext` por cada pantalla habilitada,
- exponer endpoints HTTP,
- generar HTML cliente para `WebImage` y `WebRTC`.

### Funciones clave
- `ShowInstallDialog(...)`
- `BrowserImageFit(string? fit)` — normaliza a `fill`, `cover` o `contain`
- `BuildWebImagePage(string title, string browserImageFit, int intervalMs)`
- `BuildRtcPage(string title, string browserImageFit)`
- `DisposeRuntimesAsync(IEnumerable<ScreenRuntimeContext>)`
- `ResolveRuntime(HttpContext)` — resuelve el runtime según el puerto local de la conexión

### HTML embebido
Las páginas HTML del cliente están embebidas como strings interpolados. Ambas páginas:
- usan `object-fit` configurado por `BrowserImageFit` (`fill` = estirar, `cover` = recortar, `contain` = barras),
- usan `width: 100vw; height: 100vh` para ocupar toda la pantalla del cliente,
- son responsivas a cambios de viewport con `syncViewport()`.

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
- dibujar el cursor encima si está visible,
- rotar si `RotateForPortrait` está activo,
- codificar a JPEG con la calidad configurada (`JpegQuality`),
- respetar el intervalo de captura (`CaptureIntervalSeconds`),
- guardar en memoria el último frame.

### API pública importante
- `GetCurrentFrame()` — devuelve el último JPEG capturado como `byte[]`
- `ExecuteAsync(CancellationToken)` — loop principal (hereda de `BackgroundService`)

### Notas
- hereda de `BackgroundService`,
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
**Rol:** gestionar el tray icon y el formulario de configuración de pantallas.

### Responsabilidades
- ejecutar el bucle de UI de Windows Forms en un hilo STA dedicado,
- mostrar el formulario de configuración inicial (`ShowStartupConfiguration`),
- construir el menú contextual del tray,
- actualizar el texto del tray con el estado de los runtimes,
- mostrar balloon tips al arrancar y al guardar,
- persistir cambios de configuración vía `VirtualScreenSettingsStore`.

### Clase interna: `ResolutionConfigurationForm`
Form embebido dentro de `VirtualDisplayTrayController` que contiene dos tabs (Pantalla 1 / Pantalla 2), cada una con `ScreenTabControls` que expone:
- combo de perfil de resolución
- inputs de ancho/alto (habilitados solo en perfil personalizado)
- botón de rotación ↕
- combo de posición del monitor virtual (derecha/izquierda/arriba/abajo)
- input de puerto (editable solo en el arranque inicial)
- combo de modo de transmisión (WebImage / WebRTC)
- input de intervalo de captura (segundos)
- slider de calidad JPEG
- combo de rotación de imagen (0° / 90° / 180° / 270°)
- **combo de ajuste de imagen** (`BrowserImageFit`): Estirar/Recortar/Contener

### Método clave
- `ConfigureRuntimeActions(Action exitRequested, IReadOnlyList<ScreenRuntimeContext>)`
- `UpdateStatus(string status)`
- `PostToUi(Action)` — despacha al hilo STA seguro

### Copia y clonado de config
- La copia entre configs se hace directamente vía `VirtualScreenConfig.CopyTo(target)`, inlineado en `ApplySelection`.
- El clonado del par Screen1+Screen2 se hace inline en `ResolutionConfigurationForm` con `settings.Screen1.Clone()` / `settings.Screen2.Clone()`.

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
