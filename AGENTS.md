# AGENTS.md

> Guía mínima para IA. La **fuente de verdad** de arquitectura y código es el vault Obsidian: **`VirtualWebDisplay.Obsidian/`**.
> **Punto de entrada**: `VirtualWebDisplay.Obsidian/00 - Inicio (MOC).md` (índice único). Para planificación, tareas y trabajo pendiente, usar **OpenSpec**.

## Qué es VirtualWebDisplay

App Windows (.NET 10, WinForms tray + ASP.NET Core Minimal API) que crea hasta **2 pantallas virtuales** vía Parsec VDD y las retransmite por HTTP/HTTPS usando **WebRTC (H.264)** o **Web Image (JPEG polling)**, con entrada táctil remota opcional. Resumen rápido: ver `VirtualWebDisplay.Obsidian/01 - Visión General.md` y `02 - Stack Tecnológico.md`.

## Arquitectura (resumen)

- **Entry**: `Program.cs` → `ApplicationBootstrapper` → `ApplicationLifecycleManager` (loop start/stop/restart).
- **Estado**: `ServiceStateManager` (single source of truth: Stopped/Starting/Started/Stopping).
- **Por pantalla**: `ScreenRuntimeContext` agrega `VirtualDisplayManager` + `DxgiCaptureService` + `H264EncoderService` + `WebRtcStreamService` + `ScreenSecurityGate` + `ViewerLimiter`.
- **Capas**: `UI/` · `Web/` (raíz + `Program.cs`) · `Configuration/` · `Streaming/` · `Parsec/` · `Infrastructure/` · `Localization/` · `wwwroot/`.
- **DI**: servicios en `Web/Services/` (interfaces `IXxxService`), handlers en `Web/Handlers/`.
- **Driver**: `IDriverVerifier` abstrae la verificación del driver; `ParsecVddDriverVerifier` es la implementación Windows. La cadena de DI va desde `ApplicationBootstrapper` hasta `VirtualDisplayManager`.

Diagramas, namespaces exactos, componentes por capa y flujos detallados: ver `VirtualWebDisplay.Obsidian/Arquitectura/Arquitectura por Capas.md` y el índice `VirtualWebDisplay.Obsidian/00 - Inicio (MOC).md`.

## Ideas de código limpio (principios del proyecto)

- **SOLID + DRY**: una responsabilidad por clase; abstraer dependencias externas (`IDriverVerifier`); eliminar duplicación con helpers (`PollingHelper`, `StartupErrorMessages`).
- **DI end-to-end**: ningún componente crea directamente su `IDriverVerifier`; se inyecta desde `ApplicationBootstrapper`.
- **State machine explícita** para el ciclo de vida del servicio (`ServiceStateManager`).
- **Background services** para captura/encoder/stream; `IAsyncDisposable`/`IDisposable` para recursos.
- **Records para DTOs**; métodos estáticos para helpers puros; `async/await` para I/O.
- **Namespaces según carpeta**: `VirtualWebDisplay.[Carpeta].[Subcarpeta]`.
- **No reflexión** (Native AOT friendly): usar Source Generators.
- **P/Invoke `unsafe`** aislado en `Parsec/` y `Infrastructure/Interop/`.

## Regla obligatoria: actualizar el vault al implementar

Ante **cualquier** cambio de código (mínimo, de funcionalidad o reingeniería), **actualiza la nota(s) pertinente(s) en `VirtualWebDisplay.Obsidian/`**, o créala si no existe. Una tarea **no se considera completa** si el vault no refleja el cambio.

Procedimiento:
1. Identifica qué notas describen el componente/flujo/config cambiado (usa `aliases` y `tags` del frontmatter YAML).
2. Actualiza esas notas (campos, endpoints, flujos, gotchas, `updated: YYYY-MM-DD`).
3. Si el cambio introduce un concepto/componente nuevo sin nota, **créala** atómica (un concepto por archivo), en español, con frontmatter `tags`/`aliases`/`type`/`updated`, `[[wikilinks]]` y callouts Obsidian.
4. Si el cambio afecta el índice, actualiza también `00 - Inicio (MOC).md` (índice único del vault).
5. La fuente viviente es el vault: documenta solo el **estado actual** del sistema. **No dejes** en las notas TODOs, tareas pendientes, bugs sin resolver, workarounds temporales, pasos intermedios de migración, planes de implementación ni decisiones en discusión — eso va a **OpenSpec**.

## Workflow: usar OpenSpec

OpenSpec es la **única fuente de verdad para planificación, tareas y trabajo pendiente**. Para cambios de funcionalidad o reingeniería, usa **OpenSpec** (workflow de spec-driven development) para definir el cambio, validarlo y archivar el trabajo. Consulta la skill `openspec` antes de implementar cambios significativos. El vault describe el estado actual; OpenSpec describe lo que se va a hacer.

## Entrada al vault

- `VirtualWebDisplay.Obsidian/00 - Inicio (MOC).md` — índice único del vault (para humanos e IA). Punto de entrada con visión general de todos los temas.

## Stack

.NET 10 (net10.0-windows) · WinForms · ASP.NET Core / Kestrel · SIPSorcery (WebRTC) · Parsec VDD (driver externo) · System.Drawing. Build: `dotnet build`. Tests: `VirtualWebDisplay.Tests` (xUnit).
