---
tags: [web, streaming, modos]
aliases: [Modos de Transmisión, Transmission Mode, WebImage vs RTC]
type: referencia
updated: 2026-07-08
---

# Modos de Transmisión

Dos mecanismos de entrega al navegador, ambos sobre la **misma captura base** ([[DxgiCaptureService]]). La diferencia es solo el mecanismo de entrega.

## Comparación

| Feature | Web Image (JPEG) | WebRTC |
|---|---|---|
| Latencia | ~0–200ms | ~0–50ms |
| Compatibilidad | Todos los navegadores | Modernos (Chrome, Edge, Safari) |
| CPU | Bajo | Medio (gestión de peers) |
| Touch | ✅ | ✅ |
| Múltiples clientes | ✅ (polling independiente) | ✅ (peers concurrentes) |
| Requiere HTTPS | ❌ (HTTP) | ⚠️ Experimental (requisito WebRTC) |

## Configuración

`TransmissionMethod`: `"WebImage"` | `"Rtc"` (enum `TransmissionModeOptions`).

Ambos modos comparten `CaptureIntervalSeconds` y `JpegQuality`. RTC además usa `H264Framerate` y `H264BitrateKbps`.

## Selección

- [[WebImage (JPEG Polling)]] → e-readers, dispositivos lentos, dashboards, escenarios donde importa la simplicidad.
- [[WebRTC (H.264)]] → tablets, gaming, presentaciones, baja latencia.

> [!note] iPad/Safari (WebImage)
> Se renderiza con `div#screen` + `background-image` (no `<img>`) para evitar drag/long-press nativo de Safari. Ver [[HTML Templates]].

## Enlaces

- [[WebImage (JPEG Polling)]]
- [[WebRTC (H.264)]]
- [[DxgiCaptureService]]
- [[VirtualScreenConfig (Campos)]]