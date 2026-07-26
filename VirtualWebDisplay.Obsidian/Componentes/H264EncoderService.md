---
tags: [componente, streaming, h264, ffmpeg]
aliases: [H264EncoderService, Encoder H.264]
type: componente
updated: 2026-07-26
---

# H264EncoderService

**Namespace**: `VirtualWebDisplay.Streaming`
**Archivo**: `Streaming/H264EncoderService.cs`

Consume `RawFrameAvailable` de [[DxgiCaptureService]] y codifica H.264, publicando NAL units.

## Encoder

- Selección automática: **NVENC → AMD AMF → libx264** (CPU fallback). Usa `Sdcb.FFmpeg`.
- Frames BGRA → H.264.
- Emite `NalUnitReady` consumido por [[WebRtcStreamService]].

> [!warning] QSV excluido
> `h264_qsv` **no se intenta** en runtime. En sistemas donde la DLL del runtime Intel MFX está presente pero la inicialización QSV falla, `enc.Open()` lanza un **SEH crash nativo (0xC0000005)** no capturable desde C# que mata el proceso antes de arrancar la captura GDI. Por eso `FindBestH264Encoder` solo prueba `h264_nvenc`, `h264_amf`, `libx264`. Pre-check `HasRequiredRuntime()` valida que la DLL del SDK esté cargable antes de invocar FFmpeg (NVENC/AMF/QSV crashean con AV si falta su DLL).

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

## Continuar con
- [[WebRtcStreamService]]
- [[Flujo de Captura y Streaming]]
