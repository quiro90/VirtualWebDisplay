---
tags: [flujo, vdd, display]
aliases: [Creación de Pantalla Virtual, Virtual Display Creation, Parsec VDD Setup]
type: flujo
updated: 2026-07-08
---

# Creación de Pantalla Virtual

Adjunta displays virtuales Parsec VDD al iniciar cada [[ScreenRuntimeContext]].

## Flujo

```mermaid
flowchart TD
    A[ScreenRuntimeContext init] --> B[VirtualDisplayManager]
    B --> C{DeviceId assignado?}
    C -- No --> D[FindFreeVddDevice]
    D --> E[ParsecVddDriverApi.OpenHandle<br/>+ AddDisplay via DeviceIoControl<br/>IoCtlCode.Add = 0x22E004]
    E --> F[Attach display<br/>ChangeDisplaySettingsEx]
    C -- Sí --> F
    F --> G[Apply resolution<br/>from VirtualScreenConfig]
    G --> H[Register in VirtualDisplayManager]
    H --> I[DxgiCaptureService init<br/>on that display]
```

## P/Invoke unsafe

[[VirtualDisplayManager]] usa P/Invoke **unsafe** (`setupapi.dll`, `user32.dll`, `kernel32.dll`) a través de `ParsecVddDriverApi` — exige driver Parsec VDD instalado. La creación del display se hace con `DeviceIoControl` sobre el handle del driver (IoCtlCode `0x22E004`). Ver [[IDriverVerifier (Abstracción)]].

## Resolución

- Aplica `Width` × `Height` × `RefreshRate` desde [[VirtualScreenConfig (Campos)]].
- [[Perfiles de Resolución]] predefinidos o custom ([[Resoluciones Personalizadas VDD]]).
- `VirtualResolutionWatcher` detecta cambios hardware → actualiza `virtualscreen.display.json`.

## Límite

Máximo **2 pantallas** virtuales (definido por `VirtualWebDisplaySettings`: `Screen1` + `Screen2`).

## Enlaces

- [[VirtualDisplayManager]]
- [[ScreenRuntimeContext]]
- [[IDriverVerifier (Abstracción)]]
- [[Perfiles de Resolución]]
- [[Resoluciones Personalizadas VDD]]