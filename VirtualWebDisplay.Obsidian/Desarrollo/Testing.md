---
tags: [desarrollo, testing, xunit]
aliases: [Testing, Tests, xUnit, Unit Tests]
type: guia
updated: 2026-07-08
---

# Testing

## Proyecto

`VirtualWebDisplay.Tests/` — xUnit, ~22 archivos de tests (+ helpers). Estructura espeja el proyecto: `Configuration/`, `Infrastructure/`, `Web/Api/`, `Web/Handlers/`, `Web/Security/`.

## Correr tests

```powershell
dotnet test
```

## Cobertura

Tests cubren:
- Handlers de Web API ([[Endpoints HTTP]]).
- Servicios DI (`Web/Services/`).
- Touch input ([[InputHandler (Touch)]], `TouchInputCoordinateResolver`, `TouchInputRequestValidator`, `InputCoordinateMapper`, `DragStateTracker`).
- Config ([[VirtualScreenConfig (Campos)]]).
- Seguridad ([[Seguridad por Pantalla]], [[Rate Limiting y Brute Force]], [[Límite de Viewers]]).
- Helpers y utilidades.

## Patrón

- Tests unitarios por clase/servicio.
- Mocks para dependencias Win32/P/Invoke (no se puede testear VDD real en CI).

## Enlaces

- [[Guía de Desarrollo]]
- [[InputHandler (Touch)]]
- [[Endpoints HTTP]]

## Continuar con
- [[Build y Compilación]]
- [[Guía de Desarrollo]]
