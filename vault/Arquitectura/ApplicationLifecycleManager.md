---
tags: [arquitectura, lifecycle, bootstrap]
aliases: [ApplicationLifecycleManager, Lifecycle, Bucle de servicio]
type: componente
updated: 2026-07-08
---

# ApplicationLifecycleManager

**Namespace**: `VirtualWebDisplay.Infrastructure`
**Archivo**: `Infrastructure/Hosting/ApplicationLifecycleManager.cs`

Bucle principal de **arranque/parada/restart** del servicio. Coordina con el tray icon y limpia recursos al salir.

## Responsabilidades

- Construye el `WebApplication`, configura Kestrel (ver [[KestrelConfigurator]] implícito en `Web/Hosting/`).
- Crea runtimes por pantalla ([[RuntimeFactory]] → [[ScreenRuntimeContext]]).
- Arranca servicios ([[RuntimeStartupHelper]]).
- Mapea endpoints ([[Endpoints HTTP]]).
- `app.UseStaticFiles()` sirve `wwwroot/` (ver [[Cliente Web (wwwroot)]]).

## Método principal

- `RunServiceLoopAsync()` — recibe `IDriverVerifier` y `enabledPorts` para evitar verificaciones duplicadas.

## Flujo

1. `WebApplication.CreateBuilder` + `Build()` (DI disponible: `ILoggerFactory`).
2. `RuntimeFactory.TryCreate(...)` construye 1–2 `ScreenRuntimeContext` con loggers reales.
3. `KestrelConfigurator.Configure(builder, ports, tlsCert)` asigna HTTP/HTTPS.
4. Por cada runtime: crea monitor, detecta índice, restaura posición/resolución (`VirtualResolutionWatcher` desde `virtualscreen.display.json`), arranca servicios.
5. `tray.ConfigureRuntimeActions(...)` → `ServiceStateManager.CompleteStart` (Starting → Started).
6. Publica endpoints y ejecuta `app.RunAsync()`.
7. En `finally`: `DisposeRuntimesAsync` en orden inverso.

> [!warning] Importante
> El check de updates ([[UpdateCheckService]]) corre **en `Program.cs` antes** de `ShowStartupConfiguration`, **NO** dentro de este loop. No moverlo aquí.

## Enlaces

- [[Arranque del Sistema]]
- [[ServiceStateManager]]
- [[Program (Entry Point)]]