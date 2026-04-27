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
  - formularios de configuración (embebidos en `VirtualDisplayTrayController`)
  - acceso a `Screen.AllScreens`
- `System.Drawing`
  - captura y codificación JPEG

### Interop Windows
- `user32.dll`
  - enumerar y reconfigurar displays
  - obtener cursor visible
- `setupapi.dll` + `kernel32.dll`
  - abrir handle del dispositivo `Parsec VDD`
  - enviar IOCTLs para agregar/quitar/actualizar el monitor virtual

### Red y servidor
- `ASP.NET Core Minimal API`
  - sirve `/`, `/auth/login`, `/cap`, `/mjpeg`, `/webrtc/offer`, `/config`
- binding multipuerto con `ConfigureKestrel(... ListenAnyIP ...)` (HTTP en `Port`, HTTPS en `Port+1`)

### Streaming
- `SIPSorcery.Net`
  - negociación WebRTC
  - `RTCPeerConnection`
  - `RTCDataChannel`

## Arquitectura conceptual

```text
Configuración persistida
    -> VirtualScreenSettingsStore
    -> VirtualWebDisplaySettings  (Screen1 + Screen2)
    -> VirtualScreenConfig        (config de una pantalla)

Arranque (Program.cs — composition root ~50 líneas)
    -> valida instancia única (SingleInstanceManager)
    -> carga settings (VirtualScreenSettingsStore)
    -> muestra UI inicial (VirtualDisplayTrayController)
    -> obtiene TLS cert (LocalCertificateProvider)
    -> ApplicationLifecycleManager.RunAsync(...)
        -> RuntimeFactory.GetEnabledPorts(...)  — verifica driver Parsec VDD, devuelve lista de puertos
        -> WebApplication.CreateBuilder + Build  — DI container listo (ILoggerFactory disponible)
        -> RuntimeFactory.TryCreate(...)         — construye runtimes con loggers reales
        -> KestrelConfigurator.Configure(ports)  — asigna puertos HTTP/HTTPS a Kestrel (overload de puertos)
        -> RuntimeStartupHelper.StartRuntimesAsync(...)
        -> WebApiEndpoints.Map(app, runtimes)    — registra endpoints HTTP
        -> await app.RunAsync()
        -> RuntimeCleanupHelper (al salir)
        -> bucle stop/restart coordinado con tray

Runtime por pantalla
    -> ScreenRuntimeContext
        -> VirtualDisplayManager   (monitor Win32)
        -> CaptureService          (captura JPEG periódica)
        -> WebRtcStreamService     (emisión WebRTC)
    -> ViewerLimiter          (cupo de viewers por pantalla)

Endpoints HTTP (Controllers/)
    -> WebApiEndpoints.cs          — orquestador, delega en handlers
    -> Handlers/AuthHandler.cs     — POST /auth/login
    -> Handlers/IndexHandler.cs    — GET /
    -> Handlers/CaptureHandler.cs  — GET /cap, GET /mjpeg
    -> Handlers/WebRtcHandler.cs   — POST /webrtc/offer

Acceso desde navegador/dispositivo
    -> HTTP local por puerto dedicado por pantalla
    -> si el cupo (`MaxViewers`) ya fue alcanzado, muestra mensaje y no continúa
    -> si `ScreenSecurityEnabled=true` y hay cupo, muestra login por clave (cookie HTTP-only)
    -> página HTML según modo (WebImage o Rtc)
    -> consumo de frames JPEG
```

## Regla de diseño dominante
El dominio real de la aplicación no es "servir páginas web", sino **crear una extensión virtual del escritorio Windows y exponerla de forma simple por navegador**. Casi toda decisión de diseño gira alrededor de ese objetivo:

- perfiles de resolución orientados a Kindle/iPad y otros dispositivos,
- rotación configurable del frame capturado (`StreamRotationDegrees`: 0°, 90°, 180°, 270°),
- ajuste de `object-fit` en navegador (`BrowserImageFit`: `fill`, `cover`, `contain`),
- soporte de varios puertos para varias pantallas simultáneas,
- recomendación de `WebImage` para e-ink y `Rtc` para tablets.

## Unidades funcionales
1. **Bootstrap**: `Program.cs`, `SingleInstanceManager`.
2. **Persistencia**: `VirtualScreenSettingsStore`, `VirtualWebDisplaySettings`.
3. **Modelo de pantalla**: `VirtualScreenConfig`, `VirtualDisplayProfiles`, `TransmissionModeOptions`, `VirtualDisplayPlacementOptions`.
4. **Ciclo de vida del monitor virtual**: `VirtualDisplayManager`.
5. **Captura de imagen**: `CaptureService`.
6. **Emisión por red**: rutas HTTP en `Program.cs` + `WebRtcStreamService`.
7. **Operación de usuario**: `VirtualDisplayTrayController` (incluye `ResolutionConfigurationForm` como clase privada interna).

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

## Configuración de usuario
- Persistida en: `%USERPROFILE%\.virtualwebdisplay\virtualscreen.user.json`
- Soporte de migración desde formato legado (sección `VirtualScreen` única -> Screen1+Screen2)
- Los cambios desde la UI se guardan inmediatamente pero **requieren reinicio** para que las pantallas y puertos se recreen con los nuevos valores.
