---
tags: [web, streaming, jpeg, polling, webimage]
aliases: [WebImage, JPEG Polling, Web Image]
type: referencia
updated: 2026-07-08
---

# WebImage (JPEG Polling)

Modo compatible con **cualquier navegador**. El cliente hace polling periódico a `/cap/{token}`.

## Flujo

1. Navegador abre `GET /` → `WebImagePageTemplate` ([[HTML Templates]]).
2. JS hace polling a `/cap/{token}?s=N` cada `CaptureIntervalSeconds * 1000ms`. El `capToken` se inyecta server-side en `WebImageClient.init`.
3. `/cap/{token}` devuelve el último JPEG disponible (ver [[Endpoints HTTP]]).
4. Cliente actualiza `background-image` de `div#screen`.
5. `object-fit` según `BrowserImageFit` (fill/cover/contain).

## Endpoints

- `GET /cap/{token}` — frame JPEG actual.
- `GET /mjpeg` — stream multipart MJPEG continuo (`multipart/x-mixed-replace`).

## Optimizaciones

- **Detección de cambios** (FNV-1a sobre ~1% píxeles) — solo codifica si el frame cambió.
- **JPEG bajo demanda** — solo codifica si hay polling `/cap` reciente o consumidores `/mjpeg`. Ver [[DxgiCaptureService]].
- **Cache de codec JPEG** (búsqueda única en constructor).

## iPad/Safari

> [!important] Anti drag/long-press
> `div` en lugar de `<img>`, CSS `touch-action: none`, `-webkit-touch-callout: none`, `user-select: none`, `preventDefault()` en eventos táctiles relevantes.

## Casos de uso

- e-readers / Kindle / e-ink.
- Dashboards y monitoring (intervalo alto: 50–200ms).
- Dispositivos con navegadores antiguos.

## Enlaces

- [[Modos de Transmisión]]
- [[DxgiCaptureService]]
- [[Módulos JavaScript]] (`webimage-client.js`)
- [[HTML Templates]]