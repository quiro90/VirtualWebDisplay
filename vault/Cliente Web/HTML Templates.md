---
tags: [cliente, html, templates]
aliases: [HTML Templates, WebImagePageTemplate, RtcPageTemplate, SecurityPageTemplate]
type: referencia
updated: 2026-07-08
---

# HTML Templates

**Carpeta**: `Web/HtmlTemplates/` — generadores de HTML servido al navegador.

## Templates

| Template | Modo / Uso |
|---|---|
| `IHtmlTemplate` | Interfaz base |
| `WebImagePageTemplate` | Modo [[WebImage (JPEG Polling)]] |
| `RtcPageTemplate` | Modo [[WebRTC (H.264)]] |
| `SecurityPageTemplate` | Página de login ([[Seguridad por Pantalla]]) |
| `ViewerLimitPageTemplate` | Página de límite de viewers ([[Límite de Viewers]]) |
| `InfoPageShell` | Shell para páginas informativas |
| `TemplateVersionHelper` | Versión dinámica (cache busting) |
| `TemplateParameterHelper` | Procesamiento centralizado de parámetros (DRY) |

## WebImagePageTemplate

- `div#screen` con `background-image` (no `<img>`) — evita drag/long-press nativo en iPad Safari.
- Inyecta `capToken` en `WebImageClient.init`.
- Transmite parámetros granulares de touch a `TouchInput.init`.
- `object-fit` según `BrowserImageFit`.

## RtcPageTemplate

- `<video>` para reproducir `VideoTrack` H.264.
- Transmite parámetros granulares de touch a `TouchInput.init`.

## Selección

[[Endpoints HTTP|`GET /`]] → `IndexPageService` (DI) resuelve el template según el modo del runtime. Si seguridad activa y no autenticado → `SecurityPageTemplate`. Si viewer limit → `ViewerLimitPageTemplate`.

## Enlaces

- [[Módulos JavaScript]]
- [[Modos de Transmisión]]
- [[Cliente Web (wwwroot)]]
- [[ESLint y Versionado]]