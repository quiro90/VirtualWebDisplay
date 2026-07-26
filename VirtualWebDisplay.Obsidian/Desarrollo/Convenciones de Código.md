---
tags: [desarrollo, convenciones, codigo]
aliases: [Convenciones de Código, Code Conventions, Naming]
type: guia
updated: 2026-07-26
---

# Convenciones de Código

## C#

- **Namespaces**: `VirtualWebDisplay.{Area}` (ej. `VirtualWebDisplay.Web.Services`).
- **DI**: servicios en `Web/Services/` con interfaz `IXxxService`, handlers en `Web/Handlers/`.
- **Static files**: `wwwroot/js/{common,touch,webimage,webrtc}/` (ver [[Módulos JavaScript]]).
- **Config**: `VirtualScreenConfig` con campos `virtualscreen.*.json` (ver [[Configuración de Usuario]]).
- **State**: `ServiceStateManager` = single source of truth (ver [[ServiceStateManager]]).

## JavaScript

- Cada módulo expone `window.XxxClient` o `window.Xxx`.
- Sin frameworks, vanilla JS.
- ESLint obligatorio (ver [[ESLint y Versionado]]).

## Enlaces

- [[Arquitectura por Capas]]
- [[ESLint y Versionado]]
- [[Guía de Desarrollo]]

## Continuar con
- [[Arquitectura por Capas]]
- [[Guía de Desarrollo]]
