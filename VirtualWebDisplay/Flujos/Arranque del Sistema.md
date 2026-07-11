---
tags: [flujo, arranque, lifecycle]
aliases: [Arranque del Sistema, Startup Flow, Boot Sequence]
type: flujo
updated: 2026-07-08
---

# Arranque del Sistema

Secuencia desde `Main` hasta servir HTTP.

## Diagrama

```mermaid
flowchart TD
    A[Program.Main] --> B[ApplicationBootstrapper.RunAsync]
    B --> C[ParsecVddDriverVerifier + RuntimeFactory.GetEnabledPorts]
    C --> D[ApplicationLifecycleManager.RunServiceLoopAsync]
    D --> E[Load Config<br/>VirtualScreenSettingsStore]
    E --> F[WebApplication.CreateBuilder/Build<br/>DI: Web/Services + Web/Handlers]
    F --> G[RuntimeFactory.TryCreate per screen<br/>ScreenRuntimeContext]
    G --> H[VirtualDisplayManager.AttachDisplay per screen]
    H --> I[KestrelConfigurator.Configure<br/>HTTP + HTTPS Port+1]
    I --> J[WebApiEndpoints.Map + app.RunAsync]
    J --> K[Tray UI WinForms]
    K --> L[ServiceStateManager = Started]
```

## Pasos

1. **[[Program (Entry Point)|Program.Main]]** — punto de entrada. Lanza `ApplicationBootstrapper.CheckForUpdateInBackgroundAsync` (fire-and-forget) y llama `ApplicationBootstrapper.RunAsync`.
2. **[[ApplicationBootstrapper]]** — crea el `ParsecVddDriverVerifier`, llama `RuntimeFactory.GetEnabledPorts` para verificar el driver, y delega a `ApplicationLifecycleManager.RunServiceLoopAsync`.
3. **[[ApplicationLifecycleManager]]** — orquesta el ciclo de vida del servicio.
4. **[[Configuración de Usuario|Config load]]** — `VirtualScreenSettingsStore` lee `%USERPROFILE%\.virtualwebdisplay\virtualscreen.user.json`.
5. **DI Container** — `WebApplication.CreateBuilder/Build` registra servicios (`Web/Services/`) y handlers (`Web/Handlers/`).
6. **[[ScreenRuntimeContext]]** por pantalla (vía `RuntimeFactory.TryCreate`) — agrega `VirtualDisplayManager` + `DxgiCaptureService` + `H264EncoderService` + `WebRtcStreamService` + `ScreenSecurityGate` + `ViewerLimiter`.
7. **[[VirtualDisplayManager]]** — adjunta displays Parsec VDD ([[RuntimeStartupHelper]] inicia los runtimes).
8. **Kestrel** — arranca HTTP en `Port` y HTTPS en `Port+1` ([[Certificado SSL (HTTPS)]], [[KestrelConfigurator]]).
9. **[[VirtualDisplayTrayController|Tray UI]]** — icono en bandeja, menú contextual.
10. **[[ServiceStateManager]]** — transición `Starting → Started`.

## Apagado

- `ApplicationLifetime.ApplicationStopping` → `ServiceStateManager = Stopping` → detiene Kestrel → libera displays → `Stopped`.

## Enlaces

- [[ApplicationBootstrapper]]
- [[ApplicationLifecycleManager]]
- [[Program (Entry Point)]]
- [[ServiceStateManager]]
- [[ScreenRuntimeContext]]
- [[Creación de Pantalla Virtual]]