# Visión general y arquitectura

## Stack principal
- `net10.0-windows`
- `Microsoft.NET.Sdk.Web`
- `UseWindowsForms=true`
- paquete externo: `SIPSorcery`

## Dependencias importantes

### Plataforma y UI
- `System.Windows.Forms`
  - tray icon
  - formularios de configuración
  - acceso a `Screen.AllScreens`
- `System.Drawing`
  - captura y codificación JPEG

### Interop Windows
- `user32.dll`
  - enumerar y reconfigurar displays
  - obtener cursor visible
- `setupapi.dll` + `kernel32.dll`
  - abrir handle del dispositivo `Parsec VDD`
  - enviar IOCTLs para agregar/quitar/update del monitor virtual

### Red y servidor
- `ASP.NET Core Minimal API`
  - sirve `/`, `/cap`, `/mjpeg`, `/webrtc/offer`, `/config`
- binding multipuerto con `UseUrls(...)`

### Streaming
- `SIPSorcery.Net`
  - negociación WebRTC
  - `RTCPeerConnection`
  - `RTCDataChannel`

## Arquitectura conceptual

```text
Configuración persistida
    -> `VirtualScreenSettingsStore`
    -> `VirtualWebDisplaySettings`
    -> `VirtualScreenConfig`

Arranque
    -> `Program.cs`
    -> valida instancia única
    -> carga settings
    -> muestra UI inicial
    -> crea runtimes por pantalla

Runtime por pantalla
    -> `ScreenRuntimeContext`
        -> `VirtualDisplayManager`
        -> `CaptureService`
        -> `WebRtcStreamService`

Acceso desde navegador/dispositivo
    -> HTTP local por puerto dedicado
    -> página HTML según modo (`WebImage` o `Rtc`)
    -> consumo de frames JPEG
```

## Regla de diseño dominante
El dominio real de la aplicación no es “servir páginas web”, sino **crear una extensión virtual del escritorio Windows y exponerla de forma simple por navegador**. Casi toda decisión de diseño gira alrededor de ese objetivo:

- perfiles orientados a Kindle/iPad,
- opción de rotación para portrait,
- ajuste de `object-fit` en navegador,
- soporte de varios puertos para varias pantallas,
- recomendación de `WebImage` para e-ink y `Rtc` para tablets.

## Unidades funcionales
1. **Bootstrap**: `Program.cs`, `SingleInstanceManager`.
2. **Persistencia**: `VirtualScreenSettingsStore`, `VirtualWebDisplaySettings`.
3. **Modelo de pantalla**: `VirtualScreenConfig`, `VirtualDisplayProfiles`, `TransmissionModeOptions`.
4. **Ciclo de vida del monitor virtual**: `VirtualDisplayManager`.
5. **Captura de imagen**: `CaptureService`.
6. **Emisión por red**: rutas HTTP en `Program.cs` + `WebRtcStreamService`.
7. **Operación de usuario**: `VirtualDisplayTrayController`.

## Punto de entrada real
Aunque existe infraestructura web, el verdadero orquestador es `Program.cs`. Ese archivo:
- carga settings,
- fuerza instancia única,
- verifica `Parsec VDD`,
- crea runtimes por pantalla,
- arranca captura y WebRTC,
- publica endpoints,
- configura el tray,
- y libera recursos al salir.