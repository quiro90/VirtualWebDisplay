---
tags: [arquitectura, capas]
aliases: [Capas, Layered Architecture]
type: arquitectura
updated: 2026-07-08
---

# Arquitectura por Capas

VirtualWebDisplay sigue un **diseño por capas** con separación clara de responsabilidades. Cada pantalla virtual activa se representa con un [[ScreenRuntimeContext]].

## Capas

| Capa | Carpeta | Responsabilidad |
|---|---|---|
| **UI** | `UI/` | Tray icon, formularios WinForms, templates HTML, theming |
| **Web** | `Web/` (raíz + `Program.cs`) | Kestrel, endpoints HTTP, handlers, servicios |
| **Configuration** | `Configuration/` | Modelos + persistencia JSON |
| **Streaming** | `Streaming/` | Captura DXGI/GDI, H.264, WebRTC |
| **Parsec** | `Parsec/` | Interfaz P/Invoke con driver VDD |
| **Infrastructure** | `Infrastructure/` | Estado, ciclo de vida, runtime, red, cert, updates |

## Namespaces

```
VirtualWebDisplay                        (raíz — solo Program.cs)
├── VirtualWebDisplay.UI.Forms
├── VirtualWebDisplay.UI.Helpers
├── VirtualWebDisplay.UI.Messaging
├── VirtualWebDisplay.UI.Theme
├── VirtualWebDisplay.UI.TrayIcon
├── VirtualWebDisplay.Web.Api
├── VirtualWebDisplay.Web.Handlers
├── VirtualWebDisplay.Web.Hosting
├── VirtualWebDisplay.Web.HtmlTemplates
├── VirtualWebDisplay.Web.Security
├── VirtualWebDisplay.Web.Services
├── VirtualWebDisplay.Configuration
├── VirtualWebDisplay.Configuration.Models
├── VirtualWebDisplay.Parsec
├── VirtualWebDisplay.Streaming
├── VirtualWebDisplay.Streaming.Models
├── VirtualWebDisplay.Infrastructure.Drivers
├── VirtualWebDisplay.Infrastructure.Hosting
├── VirtualWebDisplay.Infrastructure.Runtime
├── VirtualWebDisplay.Infrastructure.Tasks
└── VirtualWebDisplay.Infrastructure.Updates
```

## Componentes por capa

- **UI**: `VirtualDisplayTrayController`, `ResolutionConfigurationForm`, `ScreenTabControls`, `CustomModesDialog`, `FormThemeApplicator`, helpers (`ShellHelper`, `UiDispatcherHelper`, `WindowDragHelper`).
- **Web**: `Program.cs` (host), `WebApiEndpoints`, handlers en `Web/Handlers/`, servicios en `Web/Services/` (interfaces `IXxxService`), templates HTML en `Web/HtmlTemplates/` (`WebImagePageTemplate`, `RtcPageTemplate`, `SecurityPageTemplate`, `ViewerLimitPageTemplate`), `KestrelConfigurator`, `LocalCertificateProvider` ([[Certificado SSL (HTTPS)]]), `NetworkAddressHelper`, `ScreenSecurityGate`, `RateLimiter`/`RateLimiterRegistry`.
- **Configuration**: `VirtualWebDisplaySettings`, `VirtualScreenSettingsStore`, `VirtualScreenConfig`, `TransmissionModeOptions`, `VirtualDisplayPlacementOptions`, `TouchInputConstants`, `VirtualDisplayResolutionStore`, `VirtualResolutionWatcher`.
- **Streaming**: `DxgiCaptureService`, `H264EncoderService`, `WebRtcStreamService`, `IFrameSource`/`IFrameCaptureService`, `JpegFallbackEncoder`.
- **Parsec**: `VirtualDisplayManager`, `ParsecVddDriverApi`, `VddCustomModesStore`.
- **Infrastructure**: ⭐ `ServiceStateManager`, `ApplicationLifecycleManager`, `ApplicationBootstrapper`, `ScreenRuntimeContext`, `RuntimeFactory`, `RuntimeStartupHelper`, `RuntimeCleanupHelper`, `PollingHelper`, `SingleInstanceManager`, `UpdateCheckService`, `IDriverVerifier`/`ParsecVddDriverVerifier`.

## Patrones utilizados

- **State Machine** — `ServiceStateManager`
- **Facade** — `ScreenRuntimeContext`
- **Adapter** — `VirtualDisplayManager` (driver externo)
- **Background Service** — captura/encoder/stream
- **Repository** — `VirtualScreenSettingsStore`
- **Template Method** — generadores HTML
- **DI** — handlers/servicios web inyectables
- **Singleton (mutex)** — `SingleInstanceManager`

## Enlaces

- [[Diagramas del Sistema]]
- [[ServiceStateManager]]
- [[ScreenRuntimeContext]]