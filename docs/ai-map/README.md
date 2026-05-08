# Mapa IA de `VirtualWebDisplay`

## Propósito del proyecto
`VirtualWebDisplay` está diseñado para **extender pantallas virtuales de Windows hacia dispositivos secundarios mediante acceso web**. El flujo base es:

1. crear uno o dos monitores virtuales en Windows usando `Parsec VDD`,
2. capturar su contenido,
3. retransmitirlo por HTTP local,
4. abrir ese contenido desde tablets, e-readers u otros dispositivos en la red.

Este mapa está pensado para que otra IA o un desarrollador nuevo entienda rápido **qué hace cada archivo, cómo se relacionan las piezas y dónde tocar según el cambio**.

## Índice
- `01-overview.md`: visión general, arquitectura y dependencias.
- `02-componentes.md`: mapeo por archivo, clase y responsabilidad.
- `03-flujos.md`: recorridos de ejecución importantes.
- `04-configuracion-y-api.md`: configuración persistida, endpoints y modos de transmisión.

## Idea mental rápida
- La app es una mezcla de `ASP.NET Core Minimal API` + `Windows Forms` + interop Win32.
- `Program.cs` bootstrappea y delega el ciclo en `ApplicationLifecycleManager`.
- **`ServiceStateManager`** gestiona el estado del servicio (Stopped/Starting/Started/Stopping) de forma centralizada y thread-safe.
- Cada pantalla virtual activa se representa con un `ScreenRuntimeContext`.
- Cada runtime tiene:
  - `VirtualDisplayManager` para crear/reconfigurar el monitor virtual,
  - `DxgiCaptureService` para capturar en DXGI/GDI y exponer frames raw/JPEG,
  - `H264EncoderService` para codificar H.264,
  - `WebRtcStreamService` para retransmitir H.264 por `WebRTC VideoTrack` (RTP),
  - `ScreenSecurityGate` para login/rate-limit por pantalla,
  - `ViewerLimiter` para limitar receptores simultáneos por pantalla.
- `VirtualDisplayTrayController` expone configuración y control desde el tray (incluye `ResolutionConfigurationForm` embebida).
- `VirtualScreenSettingsStore` guarda la configuración del usuario en `%USERPROFILE%\.virtualwebdisplay\virtualscreen.user.json`.
- La entrada táctil usa `POST /input/touch`, con gate backend por `TouchInputEnabled` y stats en `GET /input/stats`.

## Archivos principales del dominio
| Archivo | Rol |
|---|---|
| `Program.cs` | Bootstrapper, inicia `ApplicationBootstrapper` |
| `ApplicationBootstrapper.cs` | Orquestador de inicio, crea `IDriverVerifier` |
| `ApplicationLifecycleManager.cs` | Bucle de servicio (start/stop/restart) |
| `ScreenRuntimeContext.cs` | Unidad operativa por pantalla (recibe `IDriverVerifier`) |
| `VirtualDisplayManager.cs` | Crear/destruir monitor virtual (usa `IDriverVerifier` por DI) |
| `ParsecVddDriverApi.cs` | P/Invoke compartida de bajo nivel (unsafe) |
| `IDriverVerifier.cs` | Interfaz de verificación de drivers |
| `ParsecVddDriverVerifier.cs` | Implementación para Parsec VDD |
| `PollingHelper.cs` | Helper genérico de timeouts/polling |
| `StartupErrorMessages.cs` | Centralización de mensajes de error |
| `DxgiCaptureService.cs` | Captura DXGI/GDI + JPEG bajo demanda |
| `H264EncoderService.cs` | Codificación H.264 (NVENC/AMF/libx264) |
| `WebRtcStreamService.cs` | Negociación WebRTC + envío RTP H.264 |
| `InputHandler.cs` | Endpoints de touch remoto y métricas |
| `ViewerLimiter.cs` | Control de cupo de viewers por pantalla |
| `VirtualDisplayTrayController.cs` | Tray icon + formulario de configuración |
| `VirtualScreenSettingsStore.cs` | Carga y persistencia de JSON de usuario |
| `VirtualWebDisplaySettings.cs` | Objeto raíz de configuración (Screen1 + Screen2) |
| `VirtualScreenConfig.cs` | Config de una sola pantalla virtual |
| `VirtualDisplayProfiles.cs` | Catálogo de perfiles de resolución |
| `TransmissionModeOptions.cs` | Constantes y validación de modos WebImage/Rtc |
| `VirtualDisplayPlacementOptions.cs` | Normalización y cálculo de posición del monitor |
| `NetworkAddressHelper.cs` | Detección de IP local y construcción de URLs |
| `SingleInstanceManager.cs` | Mutex de instancia única + shutdown listener |