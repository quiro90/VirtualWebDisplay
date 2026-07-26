---
tags: [stack, tech, dependencies]
aliases: [Stack, Tecnologías, Dependencias]
type: reference
updated: 2026-07-08
---

# 02 — Stack Tecnológico

## Núcleo

| Tecnología | Uso |
|---|---|
| **.NET 10** (`net10.0-windows`) | Plataforma base |
| **C# 13** | Lenguaje |
| **ASP.NET Core / Kestrel** | Servidor web HTTP/HTTPS |
| **WinForms** (`UseWindowsForms=true`) | UI: tray icon + formularios |
| **Minimal API** | Endpoints HTTP |

## Streaming / Captura

| Tecnología | Uso |
|---|---|
| **SIPSorcery** `10.0.5` | WebRTC (negociación SDP, RTP) |
| **Vortice.DXGI** / **Vortice.Direct3D11** `3.8.3` | DXGI Desktop Duplication (captura) |
| **Sdcb.FFmpeg** `7.0.0` + runtime `7.1.0` | Codificación H.264 (NVENC/AMF/libx264) |
| **System.Drawing.Common** | Fallback GDI + JPEG |

## Driver virtual

- **Parsec VDD** (externo, no NuGet) — driver de pantalla virtual. Interfaz vía P/Invoke unsafe en [[VirtualDisplayManager]] y [[IDriverVerifier (Abstracción)]].
- Instalador directo: `https://builds.parsec.app/vdd/parsec-vdd-0.45.0.0.exe`

## Cliente web (JavaScript)

- **ESLint** `^8.57.0` (dev) — ver [[ESLint y Versionado]].
- Módulos vanilla servidos desde `wwwroot/js/` — ver [[Módulos JavaScript]].

## Configuración del proyecto

> [!info] `VirtualWebDisplay_Parsec/VirtualWebDisplay.csproj`
> - `Microsoft.NET.Sdk.Web` · `Nullable=enable` · `ImplicitUsings=enable`
> - `AllowUnsafeBlocks=true` (P/Invoke del driver)
> - `OutputType=WinExe` · `AssemblyName=VirtualWebDisplay`
> - `Version=1.0.4` (usada para cache busting de JS)
> - `PublishAot=true` — ver [[Native AOT]]

## Solution

- `VirtualWebDisplay_Parsec.slnx` — solution con 2 proyectos:
  - `VirtualWebDisplay_Parsec/VirtualWebDisplay.csproj` (app)
  - `VirtualWebDisplay.Tests/VirtualWebDisplay.Tests.csproj` (xUnit) — ver [[Testing]]

## Enlaces

- [[Arquitectura por Capas]]
- [[Build y Compilación]]
- [[Native AOT]]

## Continuar con
- [[Arquitectura por Capas]]
- [[Program (Entry Point)]]
