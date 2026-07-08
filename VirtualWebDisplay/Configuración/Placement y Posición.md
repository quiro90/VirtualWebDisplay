---
tags: [config, placement, posicion, monitor]
aliases: [Placement, Posición del Monitor, VirtualDisplayPlacementOptions, duplicate]
type: referencia
updated: 2026-07-08
---

# Placement y Posición

**Archivo**: `Configuration/VirtualDisplayPlacementOptions.cs` — normalización + cálculo de posición Win32 del monitor virtual.

## Opciones (`VirtualDisplayPlacement`)

```
┌─────────────┐
│    Above    │
└─────────────┘
┌──────┬─────────┬──────┐
│ Left │ Primary │ Right│
└──────┴─────────┴──────┘
       ┌─────────────┐
       │    Below    │
       └─────────────┘
```

Valores: `right`, `left`, `top`/`above`, `bottom`/`below`, **`duplicate`**.

> [!important] duplicate
> Cuando `VirtualDisplayPlacement = "duplicate"`, **no se crea ningún monitor virtual**. Se captura el monitor primario existente a su resolución actual. Útil para transmitir la pantalla principal sin crear hardware virtual.

## Offsets personalizados

- `OffsetX` / `OffsetY` (píxeles, pueden ser negativos) — desplazamiento respecto a la posición calculada.

```json
{ "Placement": "Right", "OffsetX": 100, "OffsetY": -50 }
```

## Normalización

`VirtualDisplayPlacementOptions` acepta **español e inglés** y normaliza. Expone etiqueta visible + cálculo de posición Win32.

## Dónde tocar

- **Placement/posición del monitor** → aquí.
- **Creación del monitor** → [[VirtualDisplayManager]].

## Enlaces

- [[VirtualScreenConfig (Campos)]]
- [[VirtualDisplayManager]]
- [[Creación de Pantalla Virtual]]