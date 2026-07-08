---
tags: [config, modelo, campos]
aliases: [VirtualScreenConfig, Campos de config, Screen Config]
type: referencia
updated: 2026-07-08
---

# VirtualScreenConfig (Campos)

**Archivo**: `Configuration/Models/VirtualScreenConfig.cs` — config de **una** pantalla.

## Campos funcionales

| Campo | Default | Descripción |
|---|---|---|
| `Enabled` | true | Si se crea esta pantalla |
| `Profile` | "" | Id del perfil ([[Perfiles de Resolución]]) |
| `Landscape` | false | Rota el perfil a landscape |
| `CustomWidth` / `CustomHeight` | 800/1280 | Tamaño cuando `Profile = Custom` |
| `Width` / `Height` | 800/1280 | Tamaño efectivo final (calculado) |
| `Port` | 8000 | Puerto HTTP (HTTPS = `Port + 1`) |
| `TransmissionMethod` | "Rtc" | `WebImage` o `Rtc` |
| `CaptureIntervalSeconds` | 0.25 | Ritmo de captura/emisión (ambos modos) |
| `JpegQuality` | 40 | 10–100 (validada) |
| `MaxViewers` | 1 | Máx viewers simultáneos (`0` = sin límite) |
| `TouchInputEnabled` | false | Touch remoto por pantalla (en caliente) |
| `TouchPreserveCursor` | false | Preserva cursor al tocar (en caliente) |
| `TouchZoomEnabled` | true | Gesto zoom/pellizco (en caliente) |
| `TouchZoomDelayMs` | 50 | ms para activar zoom (en caliente) |
| `TouchHoldEnabled` | true | Hold para drag (en caliente) |
| `TouchHoldDelayMs` | 250 | ms de presión para drag (en caliente) |
| `TouchScrollEnabled` | true | Scroll 2 dedos (en caliente) |
| `TouchScrollDelayMs` | 250 | ms de presión para scroll (en caliente) |
| `MonitorIndex` | -1 | -1=auto, 0=primario, 1+=otros |
| `VirtualDisplayPlacement` | "right" | right/left/top/bottom/**duplicate** |
| `BrowserImageFit` | "contain" | fill/cover/contain |
| `ScreenSecurityEnabled` | false | Clave de 6 chars para esa pantalla |
| `NetworkMode` | "WiFi" | WiFi estándar / USB tethering (máx 1 viewer, sin seguridad) |
| `H264Framerate` | — | FPS H.264 (modo RTC) |
| `H264BitrateKbps` | — | Bitrate H.264 (modo RTC) |
| `SavedPositionX` / `SavedPositionY` | — | Posición Windows persistida |

## `BrowserImageFit`

- `fill` — estira para llenar (puede deformar).
- `cover` — llena recortando bordes sobrantes.
- `contain` — preserva proporción, puede mostrar franjas negras.

> [!info]
> `BrowserImageFit` se aplica en el **CSS del HTML servido**, no en el JPEG generado.

## Clone / CopyTo

`Clone()` y `CopyTo(...)` copian todos los campos, incluidos `SavedPositionX/Y` y `H264*`. El form trabaja sobre un **clone** y aplica con `CopyTo`. Tests en `VirtualScreenConfigCopyTests`.

## Enlaces

- [[Configuración de Usuario]]
- [[Placement y Posición]]
- [[Modos de Transmisión]]
- [[Gestos Táctiles]]