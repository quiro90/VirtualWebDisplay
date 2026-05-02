﻿# Visión general y arquitectura

## Stack principal
- `net10.0-windows`
- `Microsoft.NET.Sdk.Web`
- `UseWindowsForms=true`
- `SIPSorcery` para WebRTC

## Objetivo del sistema

VirtualWebDisplay crea hasta 2 pantallas virtuales Windows (Parsec VDD) y las expone por web local (Wi-Fi o USB Tethering) en modo `WebImage` o `Rtc`, con seguridad opcional, límite de viewers y entrada táctil remota.

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

- Gestos (configurables granularmente por UI):
  - 1 dedo: tap/click izquierdo.
  - 1 dedo hold: drag (configurable por ms de hold).
  - 2 dedos: scroll vertical y horizontal (ambos sentidos, inversión natural, configurable por ms de hold).
  - 2 dedos pellizco (zoom): escalado local web, configurable por ms de hold y sensibilidad.
  - 2 dedos tap: click derecho.
  - 3+ dedos tap: click central.
- El scroll y el arrastre se activan tras sus respectivos holds (configurable por pantalla en UI).
- El zoom local está aislado y evita interferencias con el scroll de servidor.
- La configuración de gestos, delays y enablers es completamente granular, editable en caliente por pantalla, y se persiste al instante.
- Se puede configurar si preservar la posición del cursor local al hacer taps.
- El gate principal de habilitación es **backend** (`runtime.Config.TouchInputEnabled`) para control general y hay sub-gates granulares. Todo puede cambiar en caliente desde la app.

## Nota WebImage en iPad/Safari

Para evitar drag-and-drop/long-press nativo, el modo WebImage renderiza la salida en `div#screen` con `background-image` y bloqueo explícito de eventos nativos.

## Configuración de usuario

- Persistencia: `%USERPROFILE%\.virtualwebdisplay\virtualscreen.user.json`
- Cambios generales de runtime (puertos/monitor virtual) aplican al reiniciar servicio.
- `TouchInputEnabled` sí aplica en caliente.
