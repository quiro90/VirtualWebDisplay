---
tags: [web, runtime, puertos, routing]
aliases: [Resolución de Runtime, Runtime por Puerto, ResolveRuntime]
type: referencia
updated: 2026-07-26
---

# Resolución de Runtime por Puerto

Todos los [[ScreenRuntimeContext|runtimes]] escuchan en el **mismo proceso**. Cada request HTTP debe decidir qué runtime atenderla.

## Mecanismo

`RuntimeAccessHelper.ResolveRuntime(HttpContext)` (y `RuntimeAccessService` DI):
1. Compara `context.Connection.LocalPort` con `runtime.Config.Port`.
2. **HTTPS** = `Port + 1` (resuelve correctamente HTTP **y** HTTPS).
3. Si ninguno coincide → usa `runtimes[0]` como **fallback**.

> [!warning] Resolución HTTP y HTTPS
> `RuntimeAccessHelper` resuelve el runtime correcto para HTTP **y** HTTPS (puerto y `puerto+1`) mediante `TryResolveRuntimeByPort` + `MatchesRuntimePort`.

## Helpers (`RuntimeAccessHelper`)

- `IsAuthorized(HttpContext, runtime)` — verifica autorización.
- `SecurityCookieName(runtime)` — nombre de cookie autofirmado.
- `ResolveViewerKey(HttpContext, runtime)` — clave de viewer (cookie o IP).
- `NormalizeBrowserImageFit(string?)` — fill/cover/contain.
- `TryResolveAuthorizedRuntime(...)` — resolve + auth centralizado (usado por handlers).

## Respuestas HTTP centralizadas

Helpers unificados para evitar código repetido en handlers:
`AuthorizedResult`, `HtmlContent`, `NotFoundResult`, `TooManyRequestsResult`, `InternalServerErrorResult`, `ServiceUnavailableResult`, `BadRequestError`, `ViewerLimitExceededResult`.

## Tests

`VirtualWebDisplay.Tests/Infrastructure/RuntimeAccessHelperTests.cs` — puertos HTTP/HTTPS, fallback, cookies, IP fallback, respuestas 401.

## Enlaces

- [[ScreenRuntimeContext]]
- [[Endpoints HTTP]]
- [[Seguridad por Pantalla]]

## Continuar con
- [[KestrelConfigurator]]
- [[Endpoints HTTP]]
