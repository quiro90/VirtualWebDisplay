---
tags: [troubleshooting, webrtc, ssl, h264]
aliases: [Problemas de WebRTC y SSL, WebRTC Issues, SSL Issues, H.264 Issues]
type: guia
updated: 2026-07-26
---

# Problemas de WebRTC y SSL

## Síntomas

- WebRTC no inicia (SDP offer/answer falla).
- Pantalla negra en modo RTC.
- `ERR_CERT_AUTHORITY_INVALID` en navegador.
- H.264 artifacts / stutter.

## WebRTC

### SDP negotiation falla

- Verificar [[WebRtcStreamService]] logs.
- `POST /webrtc/offer` → revisar `IWebRtcOfferService` (DI).
- SIPSorcery 10.0.5 (ver [[02 - Stack Tecnológico]]).

### Pantalla negra

- `H264EncoderService` no recibe frames → verificar [[DxgiCaptureService]].
- Keyframe no generado → revisar `RequestKeyframe()` (ver [[Flujo de Captura y Streaming]]).
- FFmpeg/Sdcb.FFmpeg 7.0.0 codecs → instalar dependencias.

### Stutter / reconexión

- `webrtc-client.js` detecta stalls via `getStats()` y reconecta (ver [[Módulos JavaScript]]).
- Red inestable → bajar resolución ([[Perfiles de Resolución]]).

## SSL / HTTPS

### Certificado self-signed

- `localca.pfx` generado en `%USERPROFILE%\.virtualwebdisplay\` (ver [[Certificado SSL (HTTPS)]]).
- Navegador requiere aceptar excepción de seguridad.
- En iPad Safari: instalar perfil de certificado.

### HTTPS = Port+1

- Si `HttpPort=8080` → HTTPS en `8443`. Ver [[Certificado SSL (HTTPS)]].

## Enlaces

- [[WebRTC (H.264)]]
- [[WebRtcStreamService]]
- [[H264EncoderService]]
- [[Certificado SSL (HTTPS)]]
- [[Guía de Troubleshooting]]

## Continuar con
- [[WebRTC (H.264)]]
- [[Certificado SSL (HTTPS)]]
