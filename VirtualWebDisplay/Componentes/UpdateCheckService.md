---
tags: [componente, updates, github]
aliases: [UpdateCheckService, Check de Updates, GitHubReleaseInfo]
type: componente
updated: 2026-07-08
---

# UpdateCheckService

**Archivos**:
- `Infrastructure/Updates/UpdateCheckService.cs`
- `Infrastructure/Updates/GitHubReleaseInfo.cs`

Consulta la **GitHub Releases API** para detectar versiones más nuevas.

## Comportamiento

- Compara versión remota con `TemplateVersionHelper.AppVersion` (del ensamblado).
- Devuelve `GitHubReleaseInfo` si hay update, `null` si no.
- **Falla silenciosamente** (sin internet/timeout no crashea la app).
- Se dispara **una sola vez** al inicio desde `Program.cs` (antes de `ShowStartupConfiguration`), independiente de que el usuario inicie el servicio.
- Delay de **5 segundos** para no interferir con el arranque visual.
- **Ignora prereleases** (`prerelease: true`).

> [!warning] No mover al loop
> El check corre en `Program.cs`, **NO** dentro de [[ApplicationLifecycleManager]]. Moverlo rompería la independencia del arranque.

## DTO

`GitHubReleaseInfo`: `tag_name`, `html_url`, `body`, `prerelease`.

## Enlaces

- [[Program (Entry Point)]]
- [[ApplicationLifecycleManager]]