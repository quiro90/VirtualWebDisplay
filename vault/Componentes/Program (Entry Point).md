---
tags: [componente, entry-point, bootstrap]
aliases: [Program.cs, Entry Point, Program]
type: componente
updated: 2026-07-08
---

# Program (Entry Point)

**Archivo**: `Program.cs` (composition root, top-level statements)

## Responsabilidades

1. `SingleInstanceActivator` (UI, basada en hash del ejecutable) + `SingleInstanceManager` (servicio). Si hay instancia previa, señala la ventana existente y sale.
2. Carga settings con [[VirtualScreenSettingsStore]] `.Load()`.
3. Crea `VirtualDisplayTrayController` (hilo STA en background; crea internamente [[ServiceStateManager]] en `Stopped`).
4. `RuntimeFactory.GetEnabledPorts(settings)` verifica el driver ([[IDriverVerifier (Abstracción)]]).
5. Muestra formulario inicial (`tray.ShowStartupConfiguration()`).
6. `WebApplication.CreateBuilder` + `Build()`.
7. `RuntimeFactory.TryCreate(...)` → 1–2 [[ScreenRuntimeContext]].
8. Delega el ciclo a [[ApplicationLifecycleManager]].

> [!info] Check de updates
> `ApplicationBootstrapper.CheckForUpdateInBackgroundAsync` se dispara **una sola vez** al inicio, antes de `ShowStartupConfiguration`, con 5s de delay. Ignora prereleases. Falla silenciosamente. Ver [[UpdateCheckService]].

## Argumento CLI UAC

`--set-custom-modes "1920x1080@60;1280x720@60;..."` para el flujo de elevación de resoluciones personalizadas (ver [[Resoluciones Personalizadas VDD]]). Helpers locales: `TryGetCustomModesArgument`, `ParseCustomModesArgument`.

## Enlaces

- [[ApplicationLifecycleManager]]
- [[Arranque del Sistema]]
- [[VirtualDisplayTrayController]]