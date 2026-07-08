---
tags: [arquitectura, diagramas, mermaid]
aliases: [Diagramas, Diagramas del Sistema]
type: arquitectura
updated: 2026-07-08
---

# Diagramas del Sistema

> [!note] Mermaid
> Obsidian renderiza estos diagramas nativamente.

## Arquitectura conceptual

```mermaid
graph TB
    Program[Program.cs]
    Program --> Single[SingleInstanceManager]
    Program --> Store[VirtualScreenSettingsStore]
    Program --> Tray[VirtualDisplayTrayController]
    Program --> Lifecycle[ApplicationLifecycleManager]

    Lifecycle --> Factory[RuntimeFactory]
    Lifecycle --> Kestrel[KestrelConfigurator]
    Lifecycle --> Startup[RuntimeStartupHelper]
    Lifecycle --> Endpoints[WebApiEndpoints]

    Factory --> Runtime[ScreenRuntimeContext]
    Runtime --> VDM[VirtualDisplayManager]
    Runtime --> Dxgi[DxgiCaptureService]
    Runtime --> H264[H264EncoderService]
    Runtime --> WebRTC[WebRtcStreamService]
    Runtime --> Sec[ScreenSecurityGate]
    Runtime --> View[ViewerLimiter]

    Endpoints --> Handlers[Handlers/Services]
    Handlers --> Runtime
```

## Arranque de la aplicación

```mermaid
sequenceDiagram
    participant User
    participant Program
    participant SingleInstanceManager
    participant Store
    participant Tray
    participant Runtime
    participant Kestrel

    User->>Program: Ejecuta app
    Program->>SingleInstanceManager: EnsureSingleInstance()
    alt Ya hay instancia
        SingleInstanceManager-->>Program: false → sale
    else Primera instancia
        Program->>Store: LoadSettings()
        Program->>Tray: new(settings)
        Program->>Runtime: TryCreate(Screen1)
        opt Screen2 habilitada
            Program->>Runtime: TryCreate(Screen2)
        end
        Program->>Kestrel: Configure + MapEndpoints
        Program->>Kestrel: RunAsync()
        Kestrel-->>User: App lista (tray visible)
    end
```

## Captura y streaming (WebRTC)

```mermaid
sequenceDiagram
    participant Browser
    participant Kestrel
    participant WebRTC
    participant Dxgi
    participant H264

    Browser->>Kestrel: POST /webrtc/offer (SDP)
    Kestrel->>WebRTC: CreateAnswerAsync
    WebRTC-->>Kestrel: SDP answer
    Kestrel-->>Browser: answer
    Browser->>Browser: setRemoteDescription + ICE
    loop Cada frame
        Dxgi->>H264: RawFrameAvailable
        H264->>WebRTC: NAL units
        WebRTC->>Browser: RTP H.264
    end
    Browser->>Browser: reproduce en <video>
```

Ver el detalle completo en [[Arranque del Sistema]], [[Flujo de Captura y Streaming]] y [[Endpoints HTTP]].

## Enlaces

- [[Arquitectura por Capas]]
- [[Resolución de Runtime por Puerto]]