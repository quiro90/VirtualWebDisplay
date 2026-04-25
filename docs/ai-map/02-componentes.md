# Mapeo por componentes

## `Program.cs`
**Rol:** composición de toda la aplicación.

### Responsabilidades
- garantizar instancia única con `SingleInstanceManager`,
- cargar configuración con `VirtualScreenSettingsStore`,
- abrir la UI inicial del tray,
- detectar IP/hostname local,
- verificar disponibilidad del driver virtual,
- crear `ScreenRuntimeContext` por cada pantalla habilitada,
- exponer endpoints HTTP,
- generar HTML cliente para `WebImage` y `WebRTC`.

### Funciones clave
- `DetectLocalIp()`
- `ShowInstallDialog(...)`
- `BuildWebImagePage(...)`
- `BuildRtcPage(...)`
- `DisposeRuntimesAsync(...)`
- `ResolveRuntime(HttpContext)`

### Observación
Las páginas HTML del cliente están embebidas en strings. Si se toca la UX web, hoy el lugar correcto es este archivo.

---

## `ScreenRuntimeContext.cs`
**Rol:** contenedor de runtime por pantalla.

### Agrupa
- `VirtualScreenConfig`
- `VirtualDisplayManager`
- `CaptureService`
- `WebRtcStreamService`
- URLs de acceso (`HostUrl`, `IpUrl`)

### Métodos clave
- `StartAsync(...)`: inicia captura y broadcaster WebRTC.
- `StopAsync()`: detiene servicios en orden seguro.
- `Dispose()/DisposeAsync()`: libera servicios y display virtual.

### Idea
Es la unidad operativa principal por pantalla. Si en el futuro se agrega una tercera pantalla, el patrón ya existe aquí.

---

## `VirtualDisplayManager.cs`
**Rol:** crear, mantener, posicionar y destruir el monitor virtual de Windows.

### Responsabilidades
- verificar si `Parsec VDD` está instalado,
- abrir handle nativo al adaptador,
- agregar display virtual,
- mantener vivo el driver con un loop de `Update`,
- identificar qué `Screen` nuevo creó Windows,
- aplicar resolución y posición relativa al monitor principal,
- eliminar el display al liberar recursos.

### API pública importante
- `VerifyDriverAvailability()`
- `TryCreate(VirtualScreenConfig)`
- `TryReconfigure(VirtualScreenConfig)`
- `Dispose()`

### Detalles internos importantes
- usa `EnumDisplaySettings` y `ChangeDisplaySettingsEx` para topología/resolución,
- normaliza la posición con `right/left/top/bottom`,
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
- codificar a JPEG con calidad configurable,
- guardar en memoria el último frame.

### API pública importante
- `GetCurrentFrame()`
- `ExecuteAsync(...)`

### Notas
- hereda de `BackgroundService`,
- comparte el último frame por referencia para que `/cap`, `/mjpeg` y `WebRtcStreamService` reutilicen la misma captura.

---

## `WebRtcStreamService.cs`
**Rol:** negociar peers WebRTC y enviar frames JPEG fragmentados por `RTCDataChannel`.

### Responsabilidades
- crear respuestas SDP (`CreateAnswerAsync`),
- registrar peers activos,
- detectar cierre/desconexión y limpiar peers,
- tomar el último JPEG disponible desde `CaptureService`,
- enviar metadata JSON + chunks binarios.

### API pública importante
- `CreateAnswerAsync(WebRtcSessionOffer, CancellationToken)`
- `ExecuteAsync(...)`
- `StopAsync(...)`
- `DisposeAsync()`

### Protocolo interno de frame
1. mensaje texto: `{"type":"frame","size":N}`
2. luego múltiples chunks binarios de hasta `16 KB`
3. el cliente JS rearma el JPEG y actualiza la imagen visible

---

## `VirtualDisplayTrayController.cs`
**Rol:** interfaz operativa en bandeja del sistema y formulario de configuración.

### Responsabilidades
- correr una UI STA separada para el tray,
- mostrar formulario de configuración inicial y de runtime,
- persistir selección del usuario,
- mostrar URLs disponibles por pantalla,
- permitir abrir la URL y salir de la app.

### Zonas internas
- `VirtualDisplayTrayController`: ciclo de vida del tray y menús.
- `ResolutionConfigurationForm`: formulario modal.
- `ScreenTabControls`: construcción de la UI por pantalla.

### Qué configura
- perfil de dispositivo,
- orientación,
- tamaño custom,
- posición del monitor virtual,
- puerto,
- método de transmisión,
- intervalo de captura,
- calidad JPEG.

---

## `VirtualScreenSettingsStore.cs`
**Rol:** persistencia local de configuración de usuario.

### Responsabilidades
- cargar configuración JSON,
- migrar formato legado `VirtualScreen`,
- devolver defaults robustos ante errores,
- guardar de forma segura mediante archivo temporal + replace/move,
- ocultar carpeta y archivo en Windows.

### Puntos clave
- carpeta: `%USERPROFILE%\.virtualwebdisplay`
- archivo: `virtualscreen.user.json`
- tolera `IOException`, `UnauthorizedAccessException`, `JsonException`

---

## `VirtualWebDisplaySettings.cs`
**Rol:** raíz de configuración persistida.

### Estructura
- `Screen1`
- `Screen2`

### Reglas clave
- `Screen1` siempre queda habilitada,
- evita puertos duplicados,
- aplica defaults válidos para ambas pantallas.

---

## `VirtualScreenConfig.cs`
**Rol:** modelo de configuración por pantalla.

### Campos más relevantes
- `Enabled`
- `Width` / `Height`
- `Profile`, `Landscape`, `CustomWidth`, `CustomHeight`
- `TransmissionMethod`
- `CaptureIntervalSeconds`
- `JpegQuality`
- `Port`
- `RotateForPortrait`
- `MonitorIndex`
- `VirtualDisplayPlacement`
- `BrowserImageFit`

### Significado práctico
Es el contrato central entre UI, creación del monitor, captura y exposición web.

---

## `VirtualDisplayProfiles.cs`
**Rol:** catálogo de perfiles y resolución efectiva.

### Responsabilidades
- exponer perfiles conocidos (`Kindle`, `KindlePaperWhite12`, `IPadMini`, `IPad`, `Custom`),
- mapear perfil + orientación a tamaño efectivo,
- inferir perfil si una resolución coincide,
- aproximar resoluciones nativas a modos realmente soportados por Parsec VDD.

### Punto importante
Este archivo codifica mucho del conocimiento de producto: los destinos esperados son lectores/tablets usados como pantalla secundaria vía web.

---

## `VirtualDisplayPlacementOptions.cs`
**Rol:** helper compartido para normalizar y etiquetar la posición del monitor virtual.

### Responsabilidades
- normalizar `right/left/top/bottom` y equivalentes en español,
- devolver la etiqueta visible para UI,
- calcular la posición relativa al monitor principal.

### Impacto
Evita divergencias entre `VirtualDisplayManager.cs` y `VirtualDisplayTrayController.cs`.

---

## `NetworkAddressHelper.cs`
**Rol:** helper compartido para IP local y URLs de acceso.

### Responsabilidades
- detectar IP local IPv4,
- construir URL HTTP a partir de host y puerto.

### Impacto
Centraliza una regla usada por `Program.cs`, `ScreenRuntimeContext.cs` y `VirtualDisplayTrayController.cs`.

---

## `TransmissionModeOptions.cs`
**Rol:** reglas del modo de transmisión.

### Responsabilidades
- normalizar `WebImage` y `Rtc`,
- elegir método recomendado según perfil,
- validar límites de intervalo y JPEG,
- exponer helpers `IsWebImage` / `IsRtc`.

### Regla actual
- Kindle / Kindle PaperWhite 12 -> `WebImage`
- resto -> `Rtc`

---

## `SingleInstanceManager.cs`
**Rol:** impedir múltiples instancias del mismo ejecutable.

### Estrategia
- `Mutex` nombrado por hash del path del ejecutable,
- `EventWaitHandle` para solicitar cierre de instancia previa,
- si no responde, intenta cerrarla o matarla.

### Impacto
Evita conflictos de puertos, tray duplicado y displays virtuales sobrantes.

---

## `WeatherForecast.cs` y `Controllers/WeatherForecastController.cs`
**Rol actual:** residuo de plantilla `ASP.NET Core`.

No participan en la funcionalidad principal de pantallas virtuales. Pueden documentarse como ajenos al dominio principal.