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
- `05-deuda-tecnica-y-residuos.md`: residuos eliminados, deuda técnica vigente y prioridades de limpieza.

## Idea mental rápida
- La app es una mezcla de `ASP.NET Core Minimal API` + `Windows Forms` + interop Win32.
- `Program.cs` arma todo.
- Cada pantalla virtual activa se representa con un `ScreenRuntimeContext`.
- Cada runtime tiene:
  - `VirtualDisplayManager` para crear/reconfigurar el monitor virtual,
  - `CaptureService` para capturar JPEGs del monitor,
  - `WebRtcStreamService` para retransmitir frames por `WebRTC DataChannel`.
- `VirtualDisplayTrayController` expone configuración y control desde el tray.
- `VirtualScreenSettingsStore` guarda la configuración del usuario en `%USERPROFILE%\.virtualwebdisplay\virtualscreen.user.json`.

## Qué no es core
Los archivos `WeatherForecast.cs` y `Controllers/WeatherForecastController.cs` parecen residuo de la plantilla web y **no forman parte del flujo principal** de pantallas virtuales.