---
tags: [touch, input, gestos]
aliases: [Entrada Táctil, Touch Input, Touch Remoto]
type: referencia
updated: 2026-07-08
---

# Entrada Táctil

Control remoto táctil desde el navegador. Traduce eventos touch a **movimientos absolutos de mouse** con precisión de píxel.

## Arquitectura

- **Cliente**: [[touch-input.js]] (gestos, throttling, delays).
- **Backend**: [[InputHandler (Touch)]] → `POST /input/touch` → emulación de mouse.
- **Gate principal backend**: `TouchInputEnabled` por pantalla (en caliente).
- **Sub-gates granulares**: `TouchHoldEnabled`, `TouchScrollEnabled`, `TouchZoomEnabled` (en caliente).
- **Constantes compartidas**: `TouchInputConstants` (C# ↔ JS, DRY).

## Configuración en caliente

Todos los gestos, delays y enablers son **granulares**, editables en caliente por pantalla y **persisten al instante**. Ver [[Gestos Táctiles]] y [[VirtualScreenConfig (Campos)]].

## Endpoints

- `POST /input/touch` — eventos touch (si `TouchInputEnabled=false` → `204`).
- `GET /input/stats` — métricas (`eventsPerSecond`, `avgLatencyMs`, errores, rate limit).

## Dónde tocar

- **Touch remoto y gestos** → [[InputHandler (Touch)]] + [[touch-input.js]].
- **UI de configuración táctil** → `UI/Forms/ScreenTabControls.cs` + `ConfigurationFormPresenter`.

## Enlaces

- [[Gestos Táctiles]]
- [[touch-input.js]]
- [[InputHandler (Touch)]]
- [[Rate Limiting y Brute Force]]

## Continuar con
- [[Gestos Táctiles]]
- [[InputHandler (Touch)]]
