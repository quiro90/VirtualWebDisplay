---
tags: [componente, streaming, webrtc, sipsorcery]
aliases: [WebRtcStreamService, WebRTC Service]
type: componente
updated: 2026-07-08
---

# WebRtcStreamService

**Namespace**: `VirtualWebDisplay.Streaming`
**Archivo**: `Streaming/WebRtcStreamService.cs`

Gestiona conexiones WebRTC y transmite H.264 por `VideoTrack` RTP.

> [!warning] Delicado
> WebRTC es sensible. Usa **SIPSorcery**. Modificar con cuidado.

## Responsabilidades

- `CreateAnswerAsync(offer)` → crea `RTCPeerConnection`, agrega `VideoTrack` H.264 (send-only), `setRemoteDescription`, `createAnswer`, almacena el peer.
- Diccionario **concurrente de peers**. Cada NAL unit se transmite a todos los peers activos.
- Limpieza automática de peers desconectados.
- Conversión de timestamps de captura a reloj RTP (90kHz).

## Request/Response (ver [[WebRTC (H.264)]])

```json
// POST /webrtc/offer
{ "sdp": "...", "type": "offer" }
// →
{ "sdp": "...", "type": "answer", "peerId": "..." }
```

> [!info]
> Devuelve `400` si se invoca en modo WebImage. Requiere auth si [[Seguridad por Pantalla]] activa.

## Modelos

`WebRtcSessionOffer`, `WebRtcSessionAnswer` (`Streaming/Models/`).

## Enlaces

- [[H264EncoderService]]
- [[DxgiCaptureService]]
- [[WebRTC (H.264)]]
- [[Endpoints HTTP]]