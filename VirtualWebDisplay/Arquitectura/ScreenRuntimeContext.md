---
tags: [arquitectura, runtime, facade]
aliases: [ScreenRuntimeContext, Runtime por pantalla, Contexto de pantalla]
type: componente
updated: 2026-07-08
---

# ScreenRuntimeContext

**Namespace**: `VirtualWebDisplay.Infrastructure.Runtime`
**Archivo**: `Infrastructure/Runtime/ScreenRuntimeContext.cs`

> [!summary] Unidad operativa por pantalla
> Cada pantalla virtual activa se representa con un `ScreenRuntimeContext`. **Un runtime por pantalla**; el patrón escala agregando más contextos.

## Agrega (Facade)

Los servicios se obtienen vía `IScreenRuntimeServicesFactory` (DI) y se exponen como **interfaces**, no como tipos concretos:

| Servicio | Interfaz | Rol |
|---|---|---|
| [[VirtualDisplayManager]] | (concreto, `IDriverVerifier` inyectado) | Crear/destruir monitor virtual |
| [[DxgiCaptureService]] | `IFrameCaptureService` / `IFrameSource` | Captura DXGI/GDI + JPEG bajo demanda |
| [[H264EncoderService]] | `IH264EncoderService` | Codificación H.264 |
| [[WebRtcStreamService]] | `IWebRtcStreamService` | WebRTC VideoTrack RTP |
| `ScreenSecurityGate` | (concreto) | Login/rate-limit por pantalla — ver [[Seguridad por Pantalla]] |
| `ViewerLimiter` | (concreto) | Cupo de viewers — ver [[Límite de Viewers]] |

## CapToken

> [!warning] Token de instancia
> `CapToken` = 16 chars hex (`Guid.NewGuid().ToString("N")[..16]`) generado en boot. Cambia en cada reinicio. Usado por `GET /cap/{token}` — comparación `StringComparison.Ordinal` (~50ns). Ver [[WebImage (JPEG Polling)]].

## Lifecycle

- `StartAsync()` arranca display + captura + encoder + stream.
- `StopAsync()` / `DisposeAsync()` detiene en orden y destruye el monitor virtual.
- Recibe `IDriverVerifier` por DI — ver [[IDriverVerifier (Abstracción)]].

## Resolución por puerto

Cada runtime tiene su `Config.Port` (HTTP) y `Port+1` (HTTPS). Los endpoints resuelven el runtime correcto comparando `HttpContext.Connection.LocalPort` — ver [[Resolución de Runtime por Puerto]].

## Tests

- `VirtualWebDisplay.Tests/Infrastructure/ScreenRuntimeContextFactoryTests.cs`

## Enlaces

- [[ApplicationLifecycleManager]]
- [[ServiceStateManager]]
- [[Arranque del Sistema]]