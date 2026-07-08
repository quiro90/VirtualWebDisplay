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
    D --> E[QADDSW / ICD-10 workaround<br/>via setupapi unsafe]
    E --> F[Attach display<br/>ChangeDisplaySettingsEx]
    C -- Sí --> F
    F --> G[Apply resolution + rotation<br/>from VirtualScreenConfig]
    G --> H[Register in VirtualDisplayManager]
    H --> I[DxgiCaptureService init<br/>on that display]
```

## P/Invoke unsafe

[[VirtualDisplayManager]] usa P/Invoke **unsafe** (`setupapi.dll`, `user32.dll`) — exige driver Parsec VDD instalado. Ver [[IDriverVerifier (Abstracción)]].

## Resolución

- Aplica `Width` × `Height` × `RefreshRate` desde [[VirtualScreenConfig (Campos)]].
- [[Perfiles de Resolución]] predefinidos o custom ([[Resoluciones Personalizadas VDD]]).
- `VirtualResolutionWatcher` detecta cambios hardware → actualiza `virtualscreen.display.json`.

## Límite

Máximo **2 pantallas** virtuales (configurado en `ApplicationLifecycleManager`).

## Enlaces

- [[VirtualDisplayManager]]
- [[ScreenRuntimeContext]]
- [[IDriverVerifier (Abstracción)]]
- [[Perfiles de Resolución]]
- [[Resoluciones Personalizadas VDD]]