---
tags: [touch, gestos, gestos]
aliases: [Gestos Táctiles, Touch Gestures, Gestos]
type: referencia
updated: 2026-07-08
---

# Gestos Táctiles

Soportados por [[touch-input.js]] y validados por [[InputHandler (Touch)]].

## Gestos

| Gesto | Acción |
|---|---|
| 1 dedo tap | Click izquierdo |
| 1 dedo hold (drag) | Arrastrar (configurable por `TouchHoldDelayMs`) |
| 2 dedos scroll | Scroll vertical y horizontal, ambos sentidos, **inversión natural** (`TouchScrollDelayMs`) |
| 2 dedos pellizco (zoom) | Escalado **web local** (no envía al host, `TouchZoomDelayMs`) |
| 2 dedos tap | Click derecho |
| 3+ dedos tap | Click central |

## Configuración granular (por pantalla, en caliente)

- `TouchInputEnabled` — master toggle (backend).
- `TouchPreserveCursor` — preserva posición del cursor al tocar.
- `TouchZoomEnabled` + `TouchZoomDelayMs`
- `TouchHoldEnabled` + `TouchHoldDelayMs`
- `TouchScrollEnabled` + `TouchScrollDelayMs`

> [!info] Inversión natural
> Scroll y arrastre invertidos para sensación natural. El scroll de 2 dedos invierte el signo de `dy` en `MouseInputHelper.Scroll`.

## Aislamiento del zoom

El zoom local está aislado para **no interferir** con el scroll de servidor.

## Enlaces

- [[Entrada Táctil]]
- [[touch-input.js]]
- [[VirtualScreenConfig (Campos)]]

## Continuar con
- [[InputHandler (Touch)]]
- [[touch-input.js]]
