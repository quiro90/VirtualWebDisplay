---
tags: [desarrollo, eslint, versionado]
aliases: [ESLint y Versionado, ESLint, Versionado, Cache Busting]
type: guia
updated: 2026-07-08
---

# ESLint y Versionado

## ESLint (JavaScript)

- Config en `package.json` + `.eslintrc.json` en raíz del repo.
- Aplica a `wwwroot/js/**/*.js`.
- Reglas: no-unused-vars, no-undef, prefer-const, etc.
- Validar: `npm run lint` (ver `package.json`).

## Versionado de assets (cache busting)

- `TemplateVersionHelper` genera `?v={AppVersion}` para `<script>` y `<link>`.
- `AppVersion` viene del `.csproj` (`Version=1.0.5`) y de `package.json` (`version=1.0.5`).
- Garantiza que el navegador no cachee JS viejo al actualizar.
- Aplica a todos los módulos ([[Módulos JavaScript]]).

## Fuente

Esta nota (vault). Antes existía `docs/ESLINT_Y_VERSIONADO.md` (legacy, eliminado).

## Enlaces

- [[Módulos JavaScript]]
- [[Cliente Web (wwwroot)]]
- [[HTML Templates]]
- [[Convenciones de Código]]

## Continuar con
- [[Módulos JavaScript]]
- [[Guía de Desarrollo]]
