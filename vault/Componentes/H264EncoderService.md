---
tags: [componente, streaming, h264, ffmpeg]
aliases: [H264EncoderService, Encoder H.264]
type: componente
updated: 2026-07-08
---

# H264EncoderService

**Namespace**: `VirtualWebDisplay.Streaming`
**Archivo**: `Streaming/H264EncoderService.cs`

Consume `RawFrameAvailable` de [[DxgiCaptureService]] y codifica H.264, publicando NAL units.

## Encoder

- Selección automática: **NVENC → AMF → libx264** (primero disponible).
- Frames BGRA → H.264.
- Emite `NalUnitReady` consumido por [[WebRtcStreamService]].

## Configuración (modo RTC)

- `H264Framerate` (FPS)
- `H264BitrateKbps` (ancho de banda)

| Config | Latencia | Uso |
|---|---|---|
| 20fps / 1200kbps | ~60–90ms | Bajo ancho de banda |
| **30fps / 2000kbps** | **~30–60ms** | ✅ Recomendado |
| 60fps / 4000kbps | ~20–50ms | Equipos potentes |

## Enlaces

- [[WebRTC (H.264)]]
- [[WebRtcStreamService]]
- [[VirtualScreenConfig (Campos)]]