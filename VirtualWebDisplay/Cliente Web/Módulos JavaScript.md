---
tags: [cliente, js, modulos]
aliases: [Módulos JavaScript, JS Modules, logger, keepalive, webimage-client, webrtc-client]
type: referencia
updated: 2026-07-08
---

# Módulos JavaScript

JS modular servido desde `wwwroot/js/` (ver [[Cliente Web (wwwroot)]]). Migrado desde JS embebido en C# (`TouchInputScriptHelper.cs`, **eliminado**) a archivos `.js` independientes.

## Módulos

### `common/keepalive.js` — `window.Keepalive`
- `Keepalive.start(intervalMs)` · `Keepalive.stop()` — pings periódicos para mantener sesión.

### `common/logger.js` — `window.Logger`
- 5 niveles (SILENT/ERROR/WARN/INFO/DEBUG).

### `touch/touch-input.js` — `window.TouchInput`
- Ver [[touch-input.js]] y [[Gestos Táctiles]].

### `webimage/webimage-client.js` — `window.WebImageClient`
```javascript
WebImageClient.init({
    elementId: 'screen', intervalMs: 250, imageFit: 'cover'
});
```
- Polling a `/cap/{token}`. Preload de imágenes, retry con backoff 4x, tracking de viewport para iOS Safari.

### `webrtc/webrtc-client.js` — `window.WebRtcClient`
```javascript
WebRtcClient.init({ videoId: 'screen', statusElementId: 'status', texts: {...} });
```
- Recibe H.264 por `VideoTrack` RTP nativo, render en `<video>` (sin reensamblado manual). Retry y reconexión ante stalls detectados por `getStats()`.

## Inicialización en templates

Los templates ([[HTML Templates]]) cargan los `.js` y llaman `init(...)` con config inyectada server-side.

## Enlaces

- [[Cliente Web (wwwroot)]]
- [[HTML Templates]]
- [[ESLint y Versionado]]