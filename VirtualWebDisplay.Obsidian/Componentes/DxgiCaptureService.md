---
tags: [componente, streaming, captura, dxgi]
aliases: [DxgiCaptureService, Captura, Capture Service]
type: componente
updated: 2026-07-26
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
- `GET /cap/{token}` → `NotifyJpegDemand()` (anota timestamp de demanda).
- `GET /mjpeg` open → `EnterMjpegDemand()` (incrementa `_activeMjpegConsumers`).
- `GET /mjpeg` close → `ExitMjpegDemand()` (decrementa consumidores).

`ShouldEncodeJpeg()` retorna `true` si hay consumidores MJPEG activos **o** si la última demanda `/cap` cayó dentro de una ventana de **2 s** (`JpegDemandWindowSeconds = 2.0`). Fuera de eso, no se codifica JPEG. **No hay comparación de contenido ni hash** entre frames: la decisión es por demanda temporal, no por diff de píxeles.

## Detección de frames negros (DXGI → GDI)

`IsLikelyBlackFrame(bgra)` muestrea hasta **~2048 píxeles** (ignorando alfa) con `sampleStep = max(1, pixelCount/2048)`. Si **todos** los samples son `(0,0,0)` → frame negro. Esto se usa para decidir el **fallback DXGI → GDI** (adaptadores indirectos/virtuales como Parsec VDD devuelven frames negros por Desktop Duplication), **no** para saltar la codificación JPEG.

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

## Continuar con
- [[H264EncoderService]]
- [[Flujo de Captura y Streaming]]
