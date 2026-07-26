---
tags: [seguridad, rate-limit, brute-force]
aliases: [Rate Limiting, Brute Force, RateLimiter]
type: referencia
updated: 2026-07-08
---

# Rate Limiting y Brute Force

**Archivos**: `Web/Security/RateLimiter.cs`, `Web/Handlers/RateLimiterRegistry.cs`.

## Auth (login)

- **5 intentos** por cliente/IP.
- Ventana de **45 segundos**.
- Al superar el límite → `429` con tiempo de espera.

## Touch input

- Throttling y rate limiting configurables para evitar saturación de red con miles de eventos táctiles.
- `MinThrottleMs` y constantes en `TouchInputConstants` (single source of truth C# ↔ JS).
- `RateLimiterRegistry` encapsula rate limiting por runtime.

> [!info] Constantes compartidas
> `Configuration/TouchInputConstants.cs` centraliza `TapMaxMovePx`, `DragStaleTimeoutMs`, `MinThrottleMs`, etc. para mantener C# y [[touch-input.js]] sincronizados (DRY).

## Enlaces

- [[Seguridad por Pantalla]]
- [[InputHandler (Touch)]]
- [[touch-input.js]]

## Continuar con
- [[Seguridad por Pantalla]]
- [[Certificado SSL (HTTPS)]]
