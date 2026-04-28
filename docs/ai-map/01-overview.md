# Visión general y arquitectura

## Stack principal
- `net10.0-windows`
- `Microsoft.NET.Sdk.Web`
- `UseWindowsForms=true`
- `SIPSorcery` para WebRTC

## Objetivo del sistema

VirtualWebDisplay crea hasta 2 pantallas virtuales Windows (Parsec VDD) y las expone por web local en modo `WebImage` o `Rtc`, con seguridad opcional, límite de viewers y entrada táctil remota.

## Arquitectura conceptual (actual)

```text
Program.cs
  -> SingleInstanceManager
  -> VirtualScreenSettingsStore (JSON en .virtualwebdisplay)
  -> VirtualDisplayTrayController
  -> ApplicationLifecycleManager.RunAsync(...)
      -> RuntimeFactory.GetEnabledPorts(...)
      -> KestrelConfigurator.Configure(...)
      -> RuntimeFactory.TryCreate(...)
      -> RuntimeStartupHelper.StartRuntimesAsync(...)
      -> WebApiEndpoints.Map(...)

ScreenRuntimeContext (por pantalla)
  -> VirtualDisplayManager
  -> CaptureService
  -> WebRtcStreamService
  -> ScreenSecurityGate
  -> ViewerLimiter

Controllers/Handlers
  -> AuthHandler      (/auth/login)
  -> IndexHandler     (/)
  -> CaptureHandler   (/cap, /mjpeg)
  -> WebRtcHandler    (/webrtc/offer)
  -> InputHandler     (/input/touch, /input/stats)
```

## Endpoints clave

- `GET /`
- `POST /auth/login`
- `GET /cap`
- `GET /mjpeg`
- `POST /webrtc/offer`
- `POST /input/touch`
- `GET /input/stats`
- `GET /config`
- `GET /cert`

## Touch input (actual)

- Gestos:
  - 1 dedo: tap/click izquierdo, hold = drag
  - 2 dedos: scroll vertical y horizontal (ambos sentidos, inversión natural, configurable por ms de hold)
  - 2 dedos tap: click derecho
  - 3+ dedos: click central
- El scroll se activa tras el hold configurado por pantalla (Gestos ms). El sentido es natural: deslizar a la izquierda mueve el contenido a la derecha, y viceversa (igual para arriba/abajo).
- La configuración de gestos y modo táctil es editable en caliente, por pantalla, y se persiste al instante.
- Se preserva la posición del cursor local (no movimiento persistente del puntero original).
- El gate de habilitación es **backend** (`runtime.Config.TouchInputEnabled`) y puede cambiar en caliente desde la app.

## Nota WebImage en iPad/Safari

Para evitar drag-and-drop/long-press nativo, el modo WebImage renderiza la salida en `div#screen` con `background-image` y bloqueo explícito de eventos nativos.

## Configuración de usuario

- Persistencia: `%USERPROFILE%\.virtualwebdisplay\virtualscreen.user.json`
- Cambios generales de runtime (puertos/monitor virtual) aplican al reiniciar servicio.
- `TouchInputEnabled` sí aplica en caliente.
