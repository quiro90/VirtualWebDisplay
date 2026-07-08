---
tags: [desarrollo, guia]
aliases: [Guía de Desarrollo, Dev Guide, Setup Local]
type: guia
updated: 2026-07-08
---

# Guía de Desarrollo

## Requisitos

- **.NET 10 SDK** (net10.0-windows).
- **Visual Studio 2022** o `dotnet` CLI.
- **Parsec VDD driver** instalado (para probar displays virtuales).
- **Node.js** + npm (para ESLint de `wwwroot/js/`).

## Solution

`VirtualWebDisplay_Parsec.slnx` — 2 proyectos:
- `VirtualWebDisplay_Parsec/` — app principal (WinForms + ASP.NET Core).
- `VirtualWebDisplay.Tests/` — tests xUnit.

> [!warning] Gotcha
> El `.csproj` principal es `VirtualWebDisplay_Parsec/VirtualWebDisplay.csproj` (NO se llama como la carpeta raíz `VirtualWebDisplay/`).

## Setup

```powershell
dotnet restore
dotnet build
dotnet run --project VirtualWebDisplay_Parsec
```

## Tests

```powershell
dotnet test
```

Ver [[Testing]].

## Estructura de carpetas

Ver [[Arquitectura por Capas]] y [[00 - Inicio (MOC)]].

## Convenciones

- [[Convenciones de Código]]
- [[ESLint y Versionado]]
- [[Build y Compilación]] (incluye [[Native AOT]])

## Enlaces

- [[00 - Inicio (MOC)]]
- [[Build y Compilación]]
- [[Testing]]
- [[Convenciones de Código]]