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
├── VirtualWebDisplay.UI.TrayIcon
├── VirtualWebDisplay.UI.Forms
├── VirtualWebDisplay.UI.HtmlTemplates
├── VirtualWebDisplay.UI.Theme
├── VirtualWebDisplay.Web.Api
├── VirtualWebDisplay.Web.Handlers
├── VirtualWebDisplay.Web.Services
├── VirtualWebDisplay.Web.Security
├── VirtualWebDisplay.Web.Hosting
├── VirtualWebDisplay.Configuration
├── VirtualWebDisplay.Configuration.Models
├── VirtualWebDisplay.Parsec
├── VirtualWebDisplay.Streaming
├── VirtualWebDisplay.Streaming.Models
└── VirtualWebDisplay.Infrastructure.*
```

## Componentes por capa

- **UI**: `VirtualDisplayTrayController`, `ResolutionConfigurationForm`, `ScreenTabControls`, `CustomModesDialog`, `FormThemeApplicator`, templates HTML (`WebImagePageTemplate`, `RtcPageTemplate`).
- **Web**: `Program.cs`, `WebApiEndpoints`, handlers en `Web/Handlers/`, servicios en `Web/Services/`.
- **Configuration**: `VirtualWebDisplaySettings`, `VirtualScreenSettingsStore`, `VirtualScreenConfig`, `TransmissionModeOptions`, `VirtualDisplayPlacementOptions`, `TouchInputConstants`.
- **Streaming**: `DxgiCaptureService`, `H264EncoderService`, `WebRtcStreamService`, `IFrameSource`, `JpegFallbackEncoder`.
- **Parsec**: `VirtualDisplayManager`, `ParsecVddDriverApi`, `VddCustomModesStore`, `VirtualResolutionWatcher`.
- **Infrastructure**: ⭐ `ServiceStateManager`, `ApplicationLifecycleManager`, `ScreenRuntimeContext`, `RuntimeFactory`, `NetworkAddressHelper`, `LocalCertificateProvider`, `SingleInstanceManager`, `UpdateCheckService`.

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