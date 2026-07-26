---
tags: [web, streaming, webrtc, h264, sdp]
aliases: [WebRTC, RTC, WebRTC H.264]
type: referencia
updated: 2026-07-08
---

# WebRTC (H.264)

Streaming en tiempo real con `RTCPeerConnection` + `VideoTrack` H.264 (RTP). Latencia ~30–50ms.

## Flujo

1. Navegador abre `GET /` → `RtcPageTemplate` ([[HTML Templates]]).
2. JS crea `RTCPeerConnection`, agrega transceiver `video` (recvonly), genera SDP offer.
3. `POST /webrtc/offer` con la offer.
4. [[WebRtcStreamService]].`CreateAnswerAsync` devuelve SDP answer.
5. [[H264EncoderService]] codifica frames raw → NAL units.
6. `WebRtcStreamService` envía H.264 por RTP a todos los peers.
7. Cliente reproduce en `<video>` (sin reensamblado manual en JS).
8. `object-fit` según `BrowserImageFit`.

## Negociación (cliente)

El backend exige el record `WebRtcSessionOffer { Sdp, Type }` — **ambos campos obligatorios**. Envía `Content-Type: application/json`.

```javascript
const pc = new RTCPeerConnection();
pc.addTransceiver('video', { direction: 'recvonly' });
const offer = await pc.createOffer();
await pc.setLocalDescription(offer);
const res = await fetch('/webrtc/offer', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sdp: pc.localDescription.sdp, type: pc.localDescription.type })
});
const answer = await res.json();
await pc.setRemoteDescription({ type: 'answer', sdp: answer.sdp });
```

## Configuración

- `H264Framerate` (FPS) · `H264BitrateKbps`.
- Recomendado: 30fps / 2000kbps (~30–60ms).

## Requisitos

- **HTTPS** (requisito de seguridad de WebRTC) — ver [[Certificado SSL (HTTPS)]].
- Solo navegadores modernos.
- Devuelve `400` si se invoca en modo WebImage.

## Enlaces

- [[Modos de Transmisión]]
- [[WebRtcStreamService]]
- [[H264EncoderService]]
- [[Módulos JavaScript]] (`webrtc-client.js`)
- [[Endpoints HTTP]]

## Continuar con
- [[WebRtcStreamService]]
- [[Flujo de Captura y Streaming]]
