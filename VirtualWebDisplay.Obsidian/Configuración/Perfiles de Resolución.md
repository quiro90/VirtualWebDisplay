---
tags: [config, perfiles, resolucion]
aliases: [Perfiles de Resolución, VirtualDisplayProfiles, Resoluciones]
type: referencia
updated: 2026-07-08
---

# Perfiles de Resolución

> [!warning] No hay lista fija de perfiles
> **No existe** `VirtualDisplayProfiles.All` ni ninguna lista hardcoded de resoluciones en el código. La resolución la define el usuario. El campo `Profile` en `VirtualScreenConfig` es solo un **id/label de UI** (`string`, default `""`, **`[JsonIgnore]`**) — no enumera nada.

## Cómo se define la resolución

1. **Estado en memoria** (`VirtualScreenConfig`): `Width`/`Height` (default 1080×1920), `CustomWidth`/`CustomHeight`, `Profile`, `Landscape` — todos **`[JsonIgnore]`** (no se persisten en `virtualscreen.user.json`).
2. **Persistencia real** → `VirtualDisplayResolutionStore` (`virtualscreen.display.json`): guarda la resolución + posición X/Y activas en Windows. Lo vigilan `VirtualResolutionWatcher` + el runtime.
3. **Resoluciones custom del driver** → `VddCustomModesStore` (registro `HKLM\SOFTWARE\Parsec\vdd\{0..4}`, hasta **5 slots** `width`/`height`/`hz`) — ver [[Resoluciones Personalizadas VDD]] y `CustomModesDialog`.

## Default

`Width = 1080`, `Height = 1920` (portrait). Es el único valor fijo; el resto es user-driven.

> [!note] Rotación removida
> La **rotación de stream** (`StreamRotationDegrees`) fue eliminada del flujo activo para simplificar el mapeo de coordenadas táctiles. La orientación depende del monitor/resolución + `BrowserImageFit`. El flag `Landscape` solo afecta cómo la UI prepara el `Width`/`Height` antes de crear el display.

## Enlaces

- [[VirtualScreenConfig (Campos)]]
- [[Resoluciones Personalizadas VDD]]
- [[Placement y Posición]]

## Continuar con
- [[Resoluciones Personalizadas VDD]]
- [[Placement y Posición]]
