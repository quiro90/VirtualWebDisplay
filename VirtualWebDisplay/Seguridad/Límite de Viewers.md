---
tags: [seguridad, viewers, limite]
aliases: [Límite de Viewers, ViewerLimiter, MaxViewers]
type: referencia
updated: 2026-07-08
---

# Límite de Viewers

**Archivo**: `Web/Security/ViewerLimiter.cs` · campo `MaxViewers` ([[VirtualScreenConfig (Campos)]]).

## Reglas

- `MaxViewers` = máximo de viewers **simultáneos por pantalla**.
- `0` = **sin límite**.
- Si el cupo está alcanzado:
  - `GET /` → página informativa (`ViewerLimitPageTemplate`), **no llega a login** aunque la seguridad esté activa.
  - `GET /cap/{token}`, `GET /mjpeg`, `POST /webrtc/offer` → `429`.

## Contabilización

`ViewerLimiter` contabiliza:
- Polling activo de **WebImage** (`/cap`).
- Conexiones **MJPEG** abiertas.
- Peers **WebRTC** activos.

Expiración de viewers por polling inactivo.

## Tests

`VirtualWebDisplay.Tests/Web/Security/ViewerLimiterTests.cs` — capacidad y conteos.

## Enlaces

- [[Seguridad por Pantalla]]
- [[Endpoints HTTP]]
- [[Modos de Transmisión]]