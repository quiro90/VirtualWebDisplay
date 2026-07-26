---
tags: [flujo, captura, streaming]
aliases: [Flujo de Captura y Streaming, Capture Pipeline, Streaming Pipeline]
type: flujo
updated: 2026-07-26
---

# Flujo de Captura y Streaming

Pipeline de frame → encode → stream según modo.

## WebImage (JPEG Polling)

```mermaid
sequenceDiagram
    Browser->>+Endpoint: GET /cap/{token}
    Endpoint->>DxgiCaptureService: NotifyJpegDemand() (marca timestamp)
    Endpoint->>+DxgiCaptureService: GetCurrentJpegFrame()
    DxgiCaptureService-->>-Endpoint: _currentJpeg (cache)
    Endpoint-->>-Browser: 200 OK (binary)
```

- El loop de captura corre **continuamente** (cada `CaptureIntervalSeconds`); publica `RawFrameAvailable` siempre.
- La codificación JPEG **solo ocurre si hay demanda** (`/cap` en los últimos 2 s o consumidores `/mjpeg`), guardando el resultado en `_currentJpeg` (cache en memoria).
- `/cap/{token}` devuelve el JPEG cacheado y renueva la demanda — **no** captura sincrónica por request.
- Ver [[WebImage (JPEG Polling)]] y [[DxgiCaptureService]].

## WebRTC (H.264)

```mermaid
sequenceDiagram
    Browser->>+Endpoint: POST /webrtc/offer (WebRtcSessionOffer: sdp+type)
    Endpoint->>+WebRtcStreamService: CreateAnswerAsync(offer)
    WebRtcStreamService->>H264EncoderService: RequestKeyframe()
    H264EncoderService->>DxgiCaptureService: CaptureFrame()
    H264EncoderService->>H264EncoderService: FFmpeg H.264 encode
    H264EncoderService->>WebRtcStreamService: H.264 NALUs
    WebRtcStreamService->>WebRtcStreamService: RTP packetize
    WebRtcStreamService-->>-Browser: SDP answer + RTP stream
```

- **Keyframe on-demand** al inicio del stream.
- Encode continuo mientras hay viewers activos.
- Ver [[WebRTC (H.264)]], [[H264EncoderService]], [[WebRtcStreamService]].

## Renderizado cliente

- WebImage: `div#screen` + `background-image` ([[HTML Templates]]).
- WebRTC: `<video>` + `VideoTrack` nativo.

## Enlaces

- [[Modos de Transmisión]]
- [[DxgiCaptureService]]
- [[H264EncoderService]]
- [[WebRtcStreamService]]
- [[WebImage (JPEG Polling)]]
- [[WebRTC (H.264)]]

## Continuar con
- [[DxgiCaptureService]]
- [[H264EncoderService]]
- [[WebRtcStreamService]]
