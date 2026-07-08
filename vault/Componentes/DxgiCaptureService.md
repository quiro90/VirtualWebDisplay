---
tags: [componente, streaming, captura, dxgi]
aliases: [DxgiCaptureService, Captura, Capture Service]
type: componente
updated: 2026-07-08
---

# DxgiCaptureService

**Namespace**: `VirtualWebDisplay.Streaming`
**Archivo**: `Streaming/DxgiCaptureService.cs`

`BackgroundService` que captura el monitor objetivo y expone frames raw + JPEG.

## Captura

- **DXGI Desktop Duplication** con **fallback automático a GDI** para adaptadores virtuales/indirectos.
- Resuelve región con `GetCaptureRegion()` según `MonitorIndex`.
- Dibuja cursor si está visible.
- Publica `RawFrameAvailable` (BGRA + timestamp) para el pipeline H.264 ([[H264EncoderService]]).

## JPEG bajo demanda

> [!important] Optimización
> La codificación JPEG **solo ocurre si hay demanda** (`/cap` reciente o consumidores `/mjpeg` activos). En modo solo-WebRTC sin consumidores JPEG, se evita la codificación continua. Ahorra CPU.

Señales:
- `GET /cap/{token}` → `NotifyJpegDemand()`
- `GET /mjpeg` open → `EnterMjpegDemand()`
- `GET /mjpeg` close → `ExitMjpegDemand()`

## Detección de cambios (FNV-1a)

Hash FNV-1a sobre ~1% de píxeles (muestreo distribuido). Solo codifica JPEG si el hash difiere del frame anterior. **Ahorra ~80–90% CPU con pantalla estática**, overhead ~2–3ms.

## Loop

```
cada CaptureIntervalSeconds:
  1. copiar pantalla → Bitmap
  2. dibujar cursor
  3. publicar RawFrameAvailable
  4. si hay demanda JPEG → codificar y guardar en _currentJpeg
```

> [!warning] Frame base compartido
> **No hay captura separada por protocolo.** WebImage y WebRTC comparten la misma captura base. La diferencia es solo el mecanismo de entrega. Ver [[Modos de Transmisión]].

## Enlaces

- [[H264EncoderService]]
- [[WebImage (JPEG Polling)]]
- [[Flujo de Captura y Streaming]]
- [[VirtualScreenConfig (Campos)]] (`CaptureIntervalSeconds`, `JpegQuality`)