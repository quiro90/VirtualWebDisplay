---
tags: [arquitectura, capas]
aliases: [Capas, Layered Architecture]
type: arquitectura
updated: 2026-07-26
---

# Arquitectura por Capas

VirtualWebDisplay sigue un **diseño por capas** con separación clara de responsabilidades. Cada pantalla virtual activa se representa con un [[ScreenRuntimeContext]].

## Capas

| Capa | Carpeta | Responsabilidad |
|---|---|---|
| **UI** | `UI/` | Tray icon, formularios WinForms, helpers, theming |
| **Web** | `Web/` (raíz + `Program.cs`) | Kestrel, endpoints HTTP, handlers, servicios, templates HTML, seguridad |
| **Configuration** | `Configuration/` | Modelos + persistencia JSON + apariencia |
| **Streaming** | `Streaming/` | Captura DXGI/GDI, H.264, WebRTC |
| **Parsec** | `Parsec/` | Interfaz P/Invoke con driver VDD + watcher de resolución |
| **Infrastructure** | `Infrastructure/` | Estado, ciclo de vida, runtime, hosting, input, interop, updates |
| **Localization** | `Localization/` | Strings EN/ES (`AppText`, resx) |
| **wwwroot** | `wwwroot/` | Assets estáticos web (JS cliente, touch-input.js) |

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
├── VirtualWebDisplay.Parsec.Display
├── VirtualWebDisplay.Streaming
├── VirtualWebDisplay.Streaming.Models
├── VirtualWebDisplay.Infrastructure.Drivers
├── VirtualWebDisplay.Infrastructure.Hosting
├── VirtualWebDisplay.Infrastructure.Input
├── VirtualWebDisplay.Infrastructure.Interop
├── VirtualWebDisplay.Infrastructure.Runtime
├── VirtualWebDisplay.Infrastructure.Tasks
├── VirtualWebDisplay.Infrastructure.Updates
└── VirtualWebDisplay.Localization
```

## Componentes por capa

- **UI**: `VirtualDisplayTrayController`, `ResolutionConfigurationForm`, `ScreenTabControls`, `CustomModesDialog`, `FormThemeApplicator`, helpers (`ShellHelper`, `UiDispatcherHelper`, `WindowDragHelper`).
- **Web**: `Program.cs` (host), `WebApiEndpoints`/`WebEndpointOrchestrator`, handlers en `Web/Handlers/` (incl. `InputHandler`, `RateLimiterRegistry`, `TouchInputActions`), servicios en `Web/Services/` (interfaces `IXxxService` en `WebEndpointServices.cs`), templates HTML en `Web/HtmlTemplates/` (`WebImagePageTemplate`, `RtcPageTemplate`, `SecurityPageTemplate`, `ViewerLimitPageTemplate`), `KestrelConfigurator`, `LocalCertificateProvider` ([[Certificado SSL (HTTPS)]]), `NetworkAddressHelper`, `ScreenSecurityGate`, `RateLimiter`, `ViewerLimiter`.
- **Configuration**: `VirtualWebDisplaySettings`, `VirtualScreenSettingsStore`, `VirtualScreenConfig`, `TransmissionModeOptions`, `VirtualDisplayPlacementOptions`, `TouchInputConstants`, `VirtualDisplayResolutionStore`, `AppearanceSettingsStore`.
- **Streaming**: `DxgiCaptureService`, `H264EncoderService`, `WebRtcStreamService`, `IFrameSource`/`IFrameCaptureService`, `JpegFallbackEncoder`.
- **Parsec**: `VirtualDisplayManager`, `ParsecVddDriverApi`, `VddCustomModesStore`, `VirtualResolutionWatcher` (en `Parsec/Display/`).
- **Infrastructure**: ⭐ `ServiceStateManager`, `ApplicationLifecycleManager`, `ApplicationBootstrapper` (en `Hosting/`), `ScreenRuntimeContext`, `ScreenRuntimeServices`, `RuntimeFactory`, `RuntimeStartupHelper`, `RuntimeCleanupHelper`, `RuntimeAccessHelper`/`RuntimeAccessService`, `PollingHelper`, `SingleInstanceManager`/`SingleInstanceActivator`, `MouseInputHelper` (en `Input/`), `CursorNativeMethods` (en `Interop/`), `UpdateCheckService`, `IDriverVerifier`/`ParsecVddDriverVerifier`.

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

## Continuar con
- [[Diagramas del Sistema]]
- [[Program (Entry Point)]]
