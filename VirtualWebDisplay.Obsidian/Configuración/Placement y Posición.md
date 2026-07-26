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

Valores: `right`, `left`, `top`/`up`, `bottom`/`down`, **`duplicate`**, **`windows_managed`**.

> [!important] duplicate
> Cuando `VirtualDisplayPlacement = "duplicate"`, **no se crea ningún monitor virtual**. Se captura el monitor primario existente a su resolución actual. Útil para transmitir la pantalla principal sin crear hardware virtual.

> [!important] windows_managed
> Cuando `VirtualDisplayPlacement = "windows_managed"` (default del campo en `VirtualScreenConfig`), la app **no fuerza una posición** y deja que Windows ubique el monitor virtual. `VirtualDisplayPlacementOptions.Normalize` no reconoce este valor (cae al default `right`), por eso el modo `windows_managed` se detecta **antes** de llamar a `Normalize` (en `VirtualDisplayManager`) para saltar el cálculo de posición.

## Normalización

`VirtualDisplayPlacementOptions.Normalize` acepta **solo inglés**: `left`, `top`/`up`, `bottom`/`down`, `duplicate`. Cualquier otro valor (incluido `windows_managed`) cae al default `right`. **No acepta español** (los textos localizados se manejan en la UI vía `GetLocalizationKey`, no en `Normalize`). Expone etiqueta visible + cálculo de posición Win32 (`GetPosition`).

## Dónde tocar

- **Placement/posición del monitor** → aquí.
- **Creación del monitor** → [[VirtualDisplayManager]].

## Enlaces

- [[VirtualScreenConfig (Campos)]]
- [[VirtualDisplayManager]]
- [[Creación de Pantalla Virtual]]

## Continuar con
- [[Creación de Pantalla Virtual]]
- [[VirtualDisplayManager]]
