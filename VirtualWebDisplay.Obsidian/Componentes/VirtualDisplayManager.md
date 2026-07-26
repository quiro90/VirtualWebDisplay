---
tags: [componente, parsec, unsafe, driver]
aliases: [VirtualDisplayManager, Display Manager, VDD Manager]
type: componente
updated: 2026-07-26
---

# VirtualDisplayManager

**Namespace**: `VirtualWebDisplay.Parsec`
**Archivo**: `Parsec/VirtualDisplayManager.cs`

Interfaz con el driver **Parsec VDD** para crear/destruir pantallas virtuales.

> [!danger] Unsafe / P/Invoke
> Código **unsafe** con Win32 APIs (`CreateFile`, `DeviceIoControl`, `ChangeDisplaySettingsEx`). Modificar con conocimiento profundo de P/Invoke y recursos no administrados.

## Características

- Constructor con DI: recibe [[IDriverVerifier (Abstracción)|IDriverVerifier]] (no usa métodos estáticos).
- Usa `ParsecVddDriverApi` compartida.
- **Keep-alive loop**: `Update()` cada 100ms para mantener la pantalla activa (sin esto, el display parpadea/desaparece).
- Configura resolución, posición y frecuencia.
- `Disposable` para cleanup automático.

## Métodos clave

- `TryCreate(config)` → crea monitor, abre adaptador, IOCTL `Add`, keep-alive, detecta el `Screen` nuevo, aplica resolución/posición, actualiza `MonitorIndex`/`Width`/`Height`/`SavedPositionX`/`SavedPositionY`.
- `TryReconfigure(config)` → cambiar resolución/posición.
- `Dispose()` → destruye monitor virtual.

## Modo `duplicate`

> [!note]
> Cuando `VirtualDisplayPlacement = "duplicate"`, **no se crea ningún monitor virtual**. Se captura el monitor primario existente a su resolución actual. Ver [[Placement y Posición]].

## Detección

`VirtualResolutionWatcher` restaura la resolución y posición (X/Y) previas desde `virtualscreen.display.json`.

## Dónde tocar

- **Creación del monitor virtual** → aquí.
- **Placement/posición** → `VirtualDisplayPlacementOptions` ([[Placement y Posición]]).

## Enlaces

- [[IDriverVerifier (Abstracción)]]
- [[Creación de Pantalla Virtual]]
- [[ScreenRuntimeContext]]

## Continuar con
- [[DxgiCaptureService]]
- [[Creación de Pantalla Virtual]]
