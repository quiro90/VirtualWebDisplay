---
tags: [moc, vault, index, indice, ia]
aliases: [Map of Content, Índice, Inicio, Índice para IA, AI Index, Quick Reference]
type: indice
updated: 2026-07-26
---

# 🗺️ Índice — VirtualWebDisplay

**VirtualWebDisplay** crea hasta **2 pantallas virtuales** en Windows (driver Parsec VDD) y las transmite por web local (Wi-Fi o USB tethering) usando **WebRTC** (H.264) o **Web Image** (JPEG polling), con seguridad opcional, límite de viewers y entrada táctil remota. App .NET 10 Native AOT.

> [!summary] Modelo mental rápido
> App = `ASP.NET Core Minimal API` + `Windows Forms` (tray) + interop Win32.
> `Program.cs` bootstrappea → [[ApplicationBootstrapper]] → [[ApplicationLifecycleManager]].
> [[ServiceStateManager]] gestiona el estado (Stopped/Starting/Started/Stopping).
> Cada pantalla virtual = un [[ScreenRuntimeContext]].

> [!tip] Cómo usar este índice
> Punto único de entrada. Recorre las secciones de arriba a abajo por relevancia: arquitectura y lógica central primero, detalles y troubleshooting al fondo. Cada enlace `[[...]]` abre la nota atómica correspondiente — lee solo lo que necesites. Al final de cada nota encontrarás una sección **Continuar con** que sugiere qué leer después.

---

## 🏗️ Arquitectura — lógica central

| Nota | Qué encontrarás |
|---|---|
| [[Arquitectura por Capas]] | Capas, namespaces exactos, componentes por carpeta |
| [[Diagramas del Sistema]] | Diagramas Mermaid del arranque y streaming |
| [[ServiceStateManager]] ⭐ | Estado centralizado: Stopped/Starting/Started/Stopping |
| [[ScreenRuntimeContext]] | Unidad por pantalla: agrega todos los servicios |
| [[ApplicationLifecycleManager]] | Bucle de servicio start/stop/restart |
| [[ApplicationBootstrapper]] | Orquestador de inicio, single point de DI |
| [[RuntimeStartupHelper]] | Arranque de runtimes por pantalla |
| [[RuntimeFactory]] | Factory de runtimes, usa IDriverVerifier |
| [[IDriverVerifier (Abstracción)]] | Abstracción de driver + cadena de DI |
| [[KestrelConfigurator]] | Configuración del servidor web |

## 🧩 Componentes clave

| Nota | Qué encontrarás |
|---|---|
| [[Program (Entry Point)]] | Punto de entrada, DI, mapeo de endpoints |
| [[VirtualDisplayManager]] | P/Invoke unsafe, crear/destruir monitor virtual |
| [[DxgiCaptureService]] | Captura DXGI/GDI, JPEG on-demand, black-frame |
| [[H264EncoderService]] | FFmpeg: NVENC → AMF → libx264 (QSV excluido) |
| [[WebRtcStreamService]] | SIPSorcery, WebRTC VideoTrack RTP |
| [[InputHandler (Touch)]] | Touch remoto, gates granulares |
| [[VirtualDisplayTrayController]] | WinForms tray icon, menú, STA thread |
| [[VirtualScreenSettingsStore]] | Persistencia JSON de configuración |
| [[UpdateCheckService]] | Check de versiones vía GitHub releases |

## 🌐 Web API

| Nota | Qué encontrarás |
|---|---|
| [[Endpoints HTTP]] | Tabla completa de endpoints y autenticación |
| [[Modos de Transmisión]] | WebImage vs WebRTC, cuándo usar cada uno |
| [[WebImage (JPEG Polling)]] | JPEG bajo demanda, cache, /cap |
| [[WebRTC (H.264)]] | Negociación SDP, VideoTrack, RTP |
| [[Resolución de Runtime por Puerto]] | HTTPS = Port+1, cómo se resuelve el runtime |
| [[HTML Templates]] | Templates HTML por modo de transmisión |

## 🔄 Flujos

| Nota | Qué encontrarás |
|---|---|
| [[Arranque del Sistema]] | Desde Program.cs hasta servicio corriendo |
| [[Flujo de Captura y Streaming]] | Captura continua → H.264/JPEG → browser |
| [[Creación de Pantalla Virtual]] | AddDisplay → detección → ArrangeVirtualDisplay |
| [[Cambio de Configuración en Runtime]] | Hot-reload vía UI WinForms (no HTTP) |

## ⚙️ Configuración

| Nota | Qué encontrarás |
|---|---|
| [[Configuración de Usuario]] | Ubicación del JSON, migración, archivos ocultos |
| [[VirtualScreenConfig (Campos)]] | Tabla de campos con tipos y defaults |
| [[Perfiles de Resolución]] | Perfiles predefinidos de resolución |
| [[Placement y Posición]] | right/left/top/bottom/duplicate/windows_managed |
| [[Resoluciones Personalizadas VDD]] | Resoluciones custom del driver |

## 🔒 Seguridad

| Nota | Qué encontrarás |
|---|---|
| [[Seguridad por Pantalla]] | Password + CapToken (16 hex, Ordinal compare) |
| [[Rate Limiting y Brute Force]] | 429/lockout, 5 intentos/45s |
| [[Límite de Viewers]] | Reject tras límite de viewers concurrentes |
| [[Certificado SSL (HTTPS)]] | `localca.pfx`/`localca.crt`, autofirmado |

## 👆 Touch Input

| Nota | Qué encontrarás |
|---|---|
| [[Entrada Táctil]] | Arquitectura general, gates granulares hot-reload |
| [[Gestos Táctiles]] | Tap, hold+drag, scroll 2 dedos, zoom |
| [[touch-input.js]] | Script JS compartido WebImage/WebRTC |

## 🌐 Cliente Web

| Nota | Qué encontrarás |
|---|---|
| [[Cliente Web (wwwroot)]] | Estructura de wwwroot, assets estáticos |
| [[Módulos JavaScript]] | Módulos JS, cache busting `?v={AppVersion}` |
| [[HTML Templates]] | Templates por modo de transmisión |

## 🛠️ Desarrollo

| Nota | Qué encontrarás |
|---|---|
| [[Guía de Desarrollo]] | Setup, flujo de trabajo, convenciones |
| [[Build y Compilación]] | `dotnet build`, `dotnet run`, publish |
| [[Native AOT]] | PublishAot, TrimMode, gotchas de reflexión |
| [[ESLint y Versionado]] | Linting JS, cache busting por versión |
| [[Testing]] | xUnit, estructura de tests |
| [[Convenciones de Código]] | Nombres, namespaces, records, DI |

## 🐛 Troubleshooting

| Nota | Qué encontrarás |
|---|---|
| [[Guía de Troubleshooting]] | Índice de problemas comunes |
| [[Problemas de Instalación y Driver]] | Parsec VDD no detectado, instalación |
| [[Problemas de Red y Firewall]] | Puertos, ICE, conectividad |
| [[Problemas de WebRTC y SSL]] | WebRTC no conecta, certificados |

---

## ⚠️ Errores comunes no obvios

> [!warning] Errores comunes no obvios
> - **Nombre del .csproj** ≠ nombre de la carpeta raíz (`VirtualWebDisplay_Parsec/VirtualWebDisplay.csproj`).
> - **HTTPS = Port+1** (no un puerto independiente).
> - **CapToken** se regenera cada boot, comparación `Ordinal`.
> - **AOT** → sin reflexión general, usar Source Generators.
> - **P/Invoke unsafe** requiere Windows x64 + Parsec VDD driver instalado.
> - **`/config` es GET-only**: el hot-reload se hace desde la UI WinForms, no por HTTP.
> - **`VirtualDisplayPlacementOptions.Normalize`** solo acepta inglés; `windows_managed` se detecta antes de `Normalize` (si llegara a `Normalize`, caería en el default `Right`).
> - **QSV excluido** del encoder H.264: SEH crash nativo (0xC0000005) no capturable.

---

## 📚 Notas de introducción

- [[01 - Visión General]] — qué es y para qué sirve
- [[02 - Stack Tecnológico]] — tecnologías y dependencias