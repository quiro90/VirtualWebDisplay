---
tags: [seguridad, auth, login]
aliases: [Seguridad por Pantalla, Screen Security, ScreenSecurityGate, Auth]
type: referencia
updated: 2026-07-08
---

# Seguridad por Pantalla

**Archivos**: `Web/Security/ScreenSecurityGate.cs`, `Web/Services/AuthService.cs`, modelo `Web/Api/SecurityLoginRequest.cs`, template `SecurityPageTemplate.cs`.

## Modelo

- Autenticación **dinámica e independiente por pantalla**.
- Requiere **clave de 6 caracteres alfanuméricos** generada por el host.
- `ScreenSecurityEnabled` por pantalla ([[VirtualScreenConfig (Campos)]]).
- Login → `POST /auth/login` → cookie HTTP-only (nombre autofirmado por runtime).

## Flujo

1. `GET /` → si `ScreenSecurityEnabled=true` y no autenticado → **página de login**.
2. `POST /auth/login` valida clave.
3. Correcto → `200` + cookie. Incorrecto → `401`.
4. Endpoints protegidos (`/cap`, `/mjpeg`, `/webrtc/offer`, `/config`) requieren cookie.

> [!note] Orden con viewer limit
> Si el límite de viewers está alcanzado, `GET /` devuelve la página de límite **antes** de mostrar login, aunque la seguridad esté activa. Ver [[Límite de Viewers]].

## Helpers

- `RuntimeAccessHelper.IsAuthorized`, `SecurityCookieName`, `ResolveViewerKey` (cookie o IP).
- `TryResolveAuthorizedRuntime` — resolve + auth centralizado.

## Enlaces

- [[Rate Limiting y Brute Force]]
- [[Límite de Viewers]]
- [[Endpoints HTTP]]
- [[Resolución de Runtime por Puerto]]

## Continuar con
- [[Rate Limiting y Brute Force]]
- [[Límite de Viewers]]
