# Mapeo por componentes

## Bootstrap y ciclo de vida

### `Program.cs`
Rol: composition root (top-level statements). Carga settings, instancia tray/certificado y delega ejecucion a `ApplicationLifecycleManager`.

### `Infrastructure/ApplicationLifecycleManager.cs`
Rol: loop principal de arranque/parada. Construye app web, configura Kestrel, crea runtimes, mapea endpoints y limpia recursos.

### `Infrastructure/RuntimeFactory.cs`
Rol: verifica driver y construye `ScreenRuntimeContext` para pantallas habilitadas.

### `Infrastructure/KestrelConfigurator.cs`
Rol: bind HTTP/HTTPS por pantalla (`Port` y `Port+1`).

## Runtime por pantalla

### `Infrastructure/ScreenRuntimeContext.cs`
Agrupa runtime por pantalla:
- `VirtualDisplayManager`
- `CaptureService`
- `WebRtcStreamService`
- `ScreenSecurityGate`
- `ViewerLimiter`

### `Parsec/VirtualDisplayManager.cs`
Rol: crear/destruir monitor virtual (Parsec VDD), aplicar resolucion y placement.

### `Streaming/CaptureService.cs`
Rol: captura periodica del monitor y codificacion JPEG.

### `Streaming/WebRtcStreamService.cs`
Rol: negociacion WebRTC y envio de frames por DataChannel.

## Endpoints y handlers

### `Controllers/WebApiEndpoints.cs`
Registro central de rutas:
- `/`, `/auth/login`, `/cap`, `/mjpeg`, `/webrtc/offer`, `/input/touch`, `/input/stats`, `/config`, `/cert`

### `Controllers/Handlers/*.cs`
- `AuthHandler`: login por codigo
- `IndexHandler`: HTML principal por modo (`WebImage`/`Rtc`)
- `CaptureHandler`: captura JPEG y MJPEG
- `WebRtcHandler`: oferta/respuesta SDP
- `InputHandler`: touch remoto y metricas

## UI local (WinForms tray)

### `UI/TrayIcon/VirtualDisplayTrayController.cs`
Rol: icono de bandeja, menu y coordinacion de start/stop.

### `UI/TrayIcon/ConfigurationFormPresenter.cs`
Rol: abrir formularios y aplicar cambios sobre settings/runtimes.

### `UI/Forms/ResolutionConfigurationForm.cs`
Rol: formulario principal de configuracion.

### `UI/Forms/ScreenTabControls.cs`
Rol: controles por pantalla. Emite eventos en caliente para:
- `TouchInputChanged` (Táctil/Normal)
- `TouchGestureHoldDelayChanged` (Gestos ms)
Ambos se aplican y persisten al instante, sin reinicio. La UI de ambos campos está alineada en una sola línea, sin label extra.

## HTML templates cliente

### `UI/HtmlTemplates/WebImagePageTemplate.cs`
Modo polling `/cap`. Usa `div#screen` con `background-image` para evitar drag/long-press nativo en iPad Safari.

### `UI/HtmlTemplates/RtcPageTemplate.cs`
Modo WebRTC (DataChannel + render cliente).

### `UI/HtmlTemplates/TouchInputScriptHelper.cs`
Script touch compartido para ambos modos.
Gestos actuales:
- 1 dedo: tap/click izquierdo, hold = drag
- 2 dedos: scroll vertical y horizontal (ambos sentidos, inversión natural, configurable por ms de hold)
- 2 dedos tap: click derecho
- 3+ dedos: click central
El script emite scroll horizontal y vertical tras el hold configurado, y ambos sentidos están invertidos para sensación natural.
### `Controllers/Handlers/InputHandler.cs`
Rol: endpoint `/input/touch` para traducir eventos táctiles a mouse. Soporta drag, tap y scroll vertical y horizontal (ambos sentidos, inversión natural). La lógica es compacta y no duplica código: ambos ejes se procesan en un solo método.

## Configuracion y persistencia

### `Configuration/VirtualScreenSettingsStore.cs`
Persistencia en `%USERPROFILE%\\.virtualwebdisplay\\virtualscreen.user.json`.

### `Configuration/Models/VirtualWebDisplaySettings.cs`
Objeto raiz (`Screen1`, `Screen2`, `UiLanguage`, `WindowTheme`).

### `Configuration/Models/VirtualScreenConfig.cs`
Config por pantalla: puertos, modo, intervalo, calidad, fit, seguridad, viewers y touch.

## Donde tocar segun el cambio

- Captura o calidad: `CaptureService.cs`
- Monitor virtual / placement: `VirtualDisplayManager.cs`
- Rutas/API: `WebApiEndpoints.cs` + `Controllers/Handlers/*`
- Touch: `InputHandler.cs` + `TouchInputScriptHelper.cs`
- UI local: `UI/Forms/*` y `UI/TrayIcon/*`
- UX web: `UI/HtmlTemplates/*`
- Persistencia/config: `Configuration/Models/*` + `VirtualScreenSettingsStore.cs`
