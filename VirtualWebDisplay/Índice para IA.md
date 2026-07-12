---
tags: [indice, ia, referencia-rapida]
aliases: [Índice para IA, AI Index, Quick Reference, Índice IA]
type: indice
updated: 2026-07-08
---

# Índice para IA

Índice denso optimizado para consumo rápido por LLMs. Cada sección enlaza a la nota atómica correspondiente.

## Qué es VirtualWebDisplay

App .NET 10 (WinForms tray + ASP.NET Core Minimal API) que crea hasta **2 pantallas virtuales** vía Parsec VDD y las transmite por WebRTC (H.264) o JPEG polling. Touch remoto opcional. Native AOT. Ver [[01 - Visión General]] y [[02 - Stack Tecnológico]].

## Arquitectura en 1 minuto

- Entry: [[Program (Entry Point)]] → [[ApplicationBootstrapper]] → [[ApplicationLifecycleManager]].
- Estado: [[ServiceStateManager]] (single source of truth, Stopped/Starting/Started/Stopping).
- Startup de runtimes: [[RuntimeStartupHelper]] (crea displays + resuelve MonitorIndex + arranca servicios).
- Por pantalla: [[ScreenRuntimeContext]] agrega VirtualDisplayManager + DxgiCaptureService + H264EncoderService + WebRtcStreamService + ScreenSecurityGate + ViewerLimiter.
- Capas y namespaces: [[Arquitectura por Capas]].
- Diagramas: [[Diagramas del Sistema]].
- DI: `Web/Services/` (interfaces `IXxxService`) + `Web/Handlers/`.

## Componentes clave

| Componente | Nota |
|---|---|
| VirtualDisplayManager | [[VirtualDisplayManager]] — P/Invoke unsafe, Parsec VDD |
| DxgiCaptureService | [[DxgiCaptureService]] — DXGI capture, JPEG on-demand |
| H264EncoderService | [[H264EncoderService]] — FFmpeg x264 |
| WebRtcStreamService | [[WebRtcStreamService]] — SIPSorcery RTP |
| InputHandler (Touch) | [[InputHandler (Touch)]] — descompuesto en sub-componentes |
| VirtualDisplayTrayController | [[VirtualDisplayTrayController]] — WinForms tray |
| UpdateCheckService | [[UpdateCheckService]] — GitHub releases |
| VirtualScreenSettingsStore | [[VirtualScreenSettingsStore]] — persistencia config |
| IDriverVerifier | [[IDriverVerifier (Abstracción)]] — abstracción driver + DI chain |

## Web API

- Endpoints: [[Endpoints HTTP]] (tabla completa).
- Modos: [[Modos de Transmisión]] · [[WebImage (JPEG Polling)]] · [[WebRTC (H.264)]].
- Runtime por puerto: [[Resolución de Runtime por Puerto]] (HTTPS = Port+1).
- HTML: [[HTML Templates]].

## Configuración

- [[Configuración de Usuario]] — `%USERPROFILE%\.virtualwebdisplay\`.
- [[VirtualScreenConfig (Campos)]] — tabla de campos.
- [[Perfiles de Resolución]] · [[Placement y Posición]] · [[Resoluciones Personalizadas VDD]].
- Hot-reload: [[Cambio de Configuración en Runtime]] (vía UI WinForms, **no** por HTTP).

## Seguridad

- [[Seguridad por Pantalla]] — password + CapToken (16 hex, Ordinal compare).
- [[Rate Limiting y Brute Force]] — 429 / lockout.
- [[Límite de Viewers]] — reject tras límite.
- [[Certificado SSL (HTTPS)]] — `localca.pfx`/`localca.crt`, HTTPS=Port+1.

## Touch

- [[Entrada Táctil]] · [[Gestos Táctiles]] · [[touch-input.js]].
- Gates granulares hot-reload: `TouchInputEnabled`, `TouchHoldEnabled`, `TouchScrollEnabled`, `TouchZoomEnabled` + delays.
- Constantes compartidas C#↔JS: `TouchInputConstants`.

## Cliente Web

- [[Cliente Web (wwwroot)]] · [[Módulos JavaScript]] · [[HTML Templates]].
- Cache busting `?v={AppVersion}` ([[ESLint y Versionado]]).

## Flujos

- [[Arranque del Sistema]] · [[Flujo de Captura y Streaming]] · [[Creación de Pantalla Virtual]] · [[Cambio de Configuración en Runtime]].

## Desarrollo

- [[Guía de Desarrollo]] · [[Build y Compilación]] · [[Native AOT]] · [[ESLint y Versionado]] · [[Testing]] · [[Convenciones de Código]].
- Solution: `VirtualWebDisplay_Parsec.slnx` (app + tests xUnit).
- `.csproj` principal: `VirtualWebDisplay_Parsec/VirtualWebDisplay.csproj` (gotcha: no se llama como la carpeta raíz).

## Troubleshooting

- [[Guía de Troubleshooting]] · [[Problemas de Instalación y Driver]] · [[Problemas de Red y Firewall]] · [[Problemas de WebRTC y SSL]].

## Gotchas críticos

> [!warning] Cosas que muerden
> - **Nombre del .csproj** ≠ nombre de la carpeta raíz.
> - **HTTPS = Port+1** (no un puerto independiente).
> - **CapToken** se regenera cada boot, comparación `Ordinal`.
> - **Rotation** removido del flujo activo (legacy).
> - **AOT** → sin reflexión general, usar Source Generators.
> - **P/Invoke unsafe** requiere Windows x64 + Parsec VDD driver instalado.
> - **`/config` es GET-only**: el hot-reload se hace desde la UI WinForms, no por HTTP.
> - **`VirtualDisplayPlacementOptions.Normalize`** solo acepta inglés; `windows_managed` se detecta antes de `Normalize`.

## Entry point del vault

→ [[00 - Inicio (MOC)]] para navegación humana.