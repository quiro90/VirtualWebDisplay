---
tags: [config, perfiles, resolucion]
aliases: [Perfiles de Resolución, VirtualDisplayProfiles, Resoluciones]
type: referencia
updated: 2026-07-08
---

# Perfiles de Resolución

Definidos en `VirtualDisplayProfiles.All`. Todos en **portrait**; se rotan si `Landscape=true`.

## Perfiles conocidos

| Id | Resolución |
|---|---|
| `1200x1920` | 1200 × 1920 |
| `1200x1800` | 1200 × 1800 |
| `1200x1600` | 1200 × 1600 |
| `1152x2048` | 1152 × 2048 |
| `1080x3840` | 1080 × 3840 |
| `1080x2560` | 1080 × 2560 |
| `1080x1920` | 1080 × 1920 **(recomendada)** |
| `1050x1680` | 1050 × 1680 |
| `900x1600` | 900 × 1600 |
| `900x1440` | 900 × 1440 |
| `800x1280` | 800 × 1280 |
| `768x1366` | 768 × 1366 |
| `720x1280` | 720 × 1280 |
| `Custom` | Personalizado (usa `CustomWidth`/`CustomHeight`) |

## Resoluciones personalizadas del driver

Hasta **5 slots** configurables en el registro Windows — ver [[Resoluciones Personalizadas VDD]].

> [!note] Rotación removida
> La **rotación de stream** fue eliminada del flujo activo para simplificar el mapeo de coordenadas táctiles. La orientación depende del monitor/resolución + `BrowserImageFit`.

## Enlaces

- [[VirtualScreenConfig (Campos)]]
- [[Resoluciones Personalizadas VDD]]
- [[Placement y Posición]]