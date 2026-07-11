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
| `Profile` | "" | Id del perfil UI (**`[JsonIgnore]`**, no se persiste) |
| `Landscape` | false | Rota el perfil a landscape (**`[JsonIgnore]`**) |
| `CustomWidth` / `CustomHeight` | 1080/1920 | Tamaño cuando `Profile = Custom` (**`[JsonIgnore]`**) |
| `Width` / `Height` | 1080/1920 | Tamaño efectivo final (calculado, **`[JsonIgnore]`** — lo gestiona `VirtualDisplayResolutionStore`) |
| `Port` | 8000 | Puerto HTTP (HTTPS = `Port + 1`) |
| `TransmissionMethod` | "Rtc" | `WebImage` o `Rtc` |
| `CaptureIntervalSeconds` | 0.004 | Ritmo de captura/emisión en segundos (ambos modos) |
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
| `VirtualDisplayPlacement` | "windows_managed" | `right`/`left`/`top`/`bottom`/`duplicate`/`windows_managed` |
| `BrowserImageFit` | "contain" | fill/cover/contain |
| `ScreenSecurityEnabled` | false | Clave de 6 chars para esa pantalla |
| `H264Framerate` | 30 | FPS H.264 (modo RTC, `0` = default) |
| `H264BitrateKbps` | 2000 | Bitrate H.264 (modo RTC, `0` = default) |
| `SavedPositionX` / `SavedPositionY` | null | Posición Windows persistida (**`[JsonIgnore]`**, la guarda el runtime al detener) |

> [!info] Campos `[JsonIgnore]`
> `Width`, `Height`, `Profile`, `Landscape`, `CustomWidth`, `CustomHeight`, `SavedPositionX`, `SavedPositionY` **no se persisten** en `virtualscreen.user.json`: son estado de UI/runtime. La resolución activa la gestiona `VirtualDisplayResolutionStore` (`virtualscreen.display.json`). Ver [[Configuración de Usuario]].

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