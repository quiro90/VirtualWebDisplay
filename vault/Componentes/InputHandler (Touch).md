---
tags: [componente, touch, input, handler]
aliases: [InputHandler, Touch Handler, InputService]
type: componente
updated: 2026-07-08
---

# InputHandler (Touch)

**Namespace**: `VirtualWebDisplay.Web.Handlers` (servicio DI: `IInputService`)
**Archivos**: `Web/Handlers/InputHandler.cs` + componentes extraídos

Endpoint `POST /input/touch` que traduce eventos táctiles del navegador a clicks de mouse en la pantalla remota. Stats en `GET /input/stats`.

> [!important] Gate backend
> El gate principal es **backend** (`runtime.Config.TouchInputEnabled`) para evitar desincronización de estado con el cliente. Si está `false`, el backend ignora los eventos (`204`). Sub-gates granulares: `TouchHoldEnabled`, `TouchScrollEnabled`, etc. — ver [[Gestos Táctiles]].

## Componentes extraídos (refactor por fases)

Para reducir complejidad ciclomática, el handler se descompuso en:
- `TouchInputActions` — normalización/clasificación de acciones/tipos.
- `TouchInputCoordinateResolver` — mapeo de coordenadas + resolución de monitor.
- `TouchInputRequestValidator` — validación pura.
- `InputCoordinateMapper` — mapeo absoluto a coords de escritorio.
- `DragStateTracker` — estado de drag por request.
- `InputTelemetry` — métricas.
- `RateLimiterRegistry` — rate limiting.
- `TouchStatsSnapshot` — snapshot de stats.
- `TouchInputRequest` (modelo en `Web/Api/`).

> [!warning] Sin estado static mutable
> Tras un bugfix crítico, se eliminaron campos `static` de request y se migró a **estado local por request** para evitar race conditions entre requests y pantallas. `InputService` mantiene una instancia por ciclo DI.

## Comportamiento

- 1 dedo → click izquierdo · hold → drag.
- 2 dedos → click derecho · scroll (inversión natural).
- 3+ dedos → click central.
- Ver [[Gestos Táctiles]] y [[touch-input.js]].

## Tests

`InputHandlerTests`, `TouchInputActionsTests`, `TouchInputCoordinateResolverTests`, `TouchInputRequestValidatorTests`, `InputCoordinateMapperTests` — ver [[Testing]].

## Enlaces

- [[Entrada Táctil]]
- [[touch-input.js]]
- [[Endpoints HTTP]]