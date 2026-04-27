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
- `Program.cs` arma todo.
- Cada pantalla virtual activa se representa con un `ScreenRuntimeContext`.
- Cada runtime tiene:
  - `VirtualDisplayManager` para crear/reconfigurar el monitor virtual,
  - `CaptureService` para capturar JPEGs del monitor,
  - `WebRtcStreamService` para retransmitir frames por `WebRTC DataChannel`,
  - `ViewerLimiter` para limitar receptores simultáneos por pantalla.
- `VirtualDisplayTrayController` expone configuración y control desde el tray (incluye `ResolutionConfigurationForm` embebida).
- `VirtualScreenSettingsStore` guarda la configuración del usuario en `%USERPROFILE%\.virtualwebdisplay\virtualscreen.user.json`.

## Archivos principales del dominio
| Archivo | Rol |
|---|---|
| `Program.cs` | Bootstrapper, compositor, endpoints HTTP, páginas HTML |
| `ScreenRuntimeContext.cs` | Unidad operativa por pantalla |
| `VirtualDisplayManager.cs` | Crear/destruir monitor virtual vía Win32 |
| `CaptureService.cs` | Captura periódica + codificación JPEG |
| `WebRtcStreamService.cs` | Negociación WebRTC + emisión de frames |
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