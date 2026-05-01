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

**Optimizaciones recientes**:
- `ApplyScreenPropertyChange(screenId, Action)`: helper genérico para eliminar duplicación.
- `ApplyTouchGestureChange(screenId, gesture, enabled, delay)`: handler consolidado para activar/desactivar y configurar el retardo de gestos (Zoom, Hold, Scroll).
- Consolidación de eventos y delegación.
- Hot-reload: todos los cambios táctiles se aplican sin reiniciar el servicio.

### `UI/Forms/ResolutionConfigurationForm.cs`
Rol: formulario principal de configuración.

**Gestión de estado del servicio**:
- Usa `ServiceState` enum en lugar de múltiples booleanos (`_wasStarted`, `_serviceActionPending`, `_pendingStartAction`)
- Pattern matching para texto del botón: `_serviceState switch { Started => "Stop", Starting => "Starting...", ... }`
- `NotifyServiceStarted()` / `NotifyServiceStopped()`: actualizan UI según estado
- Thread-safe: llamados vía `InvokeOnFormSafely()` desde otros threads

**Indicadores de pantalla (nuevo)**:
- Muestra indicadores `1↗: 📺` y `2↗: 📺` en la parte inferior izquierda
- **Solo visibles cuando el servicio está iniciado** (control mediante `_serviceState == ServiceState.Started`)
- Click en número/flecha → abre navegador con la URL
- Click en 📺 → copia URL al portapapeles
- Tooltip dinámico muestra "Ingrese a: http://IP:PORT" (localizado EN/ES)
- Se actualiza automáticamente cuando cambia el puerto
- Screen 2 solo visible si está habilitada Y el servicio está corriendo
- Arquitectura:
  - `CreateScreenIndicator()`: factory method para crear indicadores (DRY)
  - `UpdateScreenIndicatorsVisibility()`: método centralizado para gestionar visibilidad
  - `ScreenIndicator_Click()`: handler genérico usando `Tag` para identificar el control
  - Integrado con `NotifyServiceStarted()` / `NotifyServiceStopped()`

### `UI/Forms/ScreenTabControls.cs`
Rol: controles por pantalla. Emite eventos en caliente para:
- `TouchInputChanged` (Táctil/Normal toggle general)
- `TouchPreserveCursorChanged` (Recordar posición del puntero)
- `TouchZoomChanged`, `TouchHoldChanged`, `TouchScrollChanged` (Toggle granular y delays)

**Métodos públicos**:
- `GetAccessUrl()`: retorna la URL de acceso actual `http://{IP}:{Port}` (usado por indicadores)
- `ApplyLocalization()`: actualiza textos según idioma activo
- `SetServiceRunning(bool)`: bloquea/desbloquea controles según estado del servicio
- `SetRuntimeSecurityCode(string)`: actualiza el código de seguridad mostrado

**Cambios recientes**:
- ❌ Eliminados `_accessUrlPrefixLabel` y `_httpUrlLink` de las tabs (movido a indicadores del formulario)
- ✅ La URL ahora se accede mediante indicadores en el formulario principal
- ✅ Método `GetAccessUrl()` agregado para exponer la URL sin mostrarla en la tab

**Arquitectura de Touch Mode**:
- Sustitución de opciones excluyentes por checkboxes granulares.
- Checkboxes individuales por cada gesto (Zoom, Mantener toque, Scroll) con sus respectivos tiempos de respuesta en milisegundos (`NumericUpDown`).
- Master/slave logic: El CheckBox maestro "Entrada táctil" (`TouchInputEnabled`) habilita o deshabilita los sub-controles de la UI.
- Activar el checkbox de un gesto, habilita inmediatamente la configuración de tiempo para este.
- Todos los cambios se aplican y persisten al instante, sin reinicio
- Localización completa vía AppText (EN/ES) con cambio de idioma en vivo

## HTML templates cliente

### `Web/HtmlTemplates/WebImagePageTemplate.cs`
Modo polling `/cap`. Usa `div#screen` con `background-image` para evitar drag/long-press nativo en iPad Safari. Transmite parámetros granulares al script de touch `TouchInput.init`.

### `Web/HtmlTemplates/RtcPageTemplate.cs`
Modo WebRTC (DataChannel + render cliente). Transmite parámetros granulares al script de touch `TouchInput.init`.

### `wwwroot/js/touch/touch-input.js`
Script touch compartido para ambos modos.
Gestos actuales:
- 1 dedo: tap/click izquierdo.
- 1 dedo hold: drag (configurable por ms de hold).
- 2 dedos: scroll vertical y horizontal (ambos sentidos, inversión natural, configurable por ms de hold).
- 2 dedos pellizco (zoom): escalado web local.
- 2 dedos tap: click derecho
- 3+ dedos: click central
El script emite eventos luego de respetar los bloqueos configurados de delay y los toggles por gesto. Todos los sentidos están invertidos para sensación natural.
### `Controllers/Handlers/InputHandler.cs`
Rol: endpoint `/input/touch` para traducir eventos táctiles a mouse. Soporta drag, tap y scroll vertical y horizontal (ambos sentidos, inversión natural). Verifica sub-gates como `TouchHoldEnabled` y `TouchScrollEnabled` para permitir el paso del evento a la VM.

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
- UX web: `Web/HtmlTemplates/*`
- Persistencia/config: `Configuration/Models/*` + `VirtualScreenSettingsStore.cs`
tore.cs`
