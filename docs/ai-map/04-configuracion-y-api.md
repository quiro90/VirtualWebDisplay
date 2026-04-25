# Configuración, endpoints y contratos

## Persistencia

### Ubicación
- directorio: `%USERPROFILE%\.virtualwebdisplay`
- archivo: `virtualscreen.user.json`

### Objeto raíz
`VirtualWebDisplaySettings`

```text
Screen1: VirtualScreenConfig
Screen2: VirtualScreenConfig
```

### Compatibilidad hacia atrás
`VirtualScreenSettingsStore` aún sabe leer un formato legado con sección `VirtualScreen` y migrarlo a:
- `Screen1 = legacyConfig`
- `Screen2 = defaults`

## `VirtualScreenConfig`

### Campos funcionales
- `Enabled`: si se crea o no esa pantalla.
- `Profile`: perfil lógico del dispositivo destino.
- `Landscape`: rota la lógica de resolución del perfil.
- `CustomWidth` / `CustomHeight`: tamaño cuando el perfil es `Custom`.
- `Width` / `Height`: tamaño efectivo final.
- `Port`: puerto HTTP propio de la pantalla.
- `TransmissionMethod`: `WebImage` o `Rtc`.
- `CaptureIntervalSeconds`: ritmo de generación/emisión.
- `JpegQuality`: calidad de compresión.
- `RotateForPortrait`: rota el bitmap capturado.
- `MonitorIndex`: monitor Windows a capturar.
- `VirtualDisplayPlacement`: `right`, `left`, `top`, `bottom`.
- `BrowserImageFit`: `contain`, `cover`, `fill`.

## Perfiles conocidos
Definidos en `VirtualDisplayProfiles`:
- `Kindle`
- `KindlePaperWhite12`
- `IPadMini`
- `IPad`
- `Custom`

## Modos de transmisión

### `WebImage`
- HTML con `<img>` + polling a `/cap`
- simple y compatible
- recomendado para Kindle / e-ink

### `Rtc`
- HTML con JS WebRTC
- usa `RTCDataChannel`
- mejor para tablets

## Endpoints expuestos

### `GET /`
Devuelve la página HTML cliente para la pantalla correspondiente al puerto local donde entró la solicitud.

### `GET /cap`
Devuelve el último frame JPEG.

### `GET /mjpeg`
Stream multipart MJPEG continuo. Existe como salida adicional basada en el mismo frame capturado.

### `POST /webrtc/offer`
Recibe una oferta SDP y devuelve la respuesta SDP.

#### Request esperado
```json
{ "sdp": "...", "type": "offer" }
```

#### Response esperado
```json
{ "sdp": "...", "type": "answer", "peerId": "..." }
```

### `GET /config`
Devuelve metadata de runtime:
- `DisplayName`
- `Config`
- `HostUrl`
- `IpUrl`

## Resolución del runtime por puerto
La app puede escuchar en varios puertos a la vez. `Program.cs` decide qué `ScreenRuntimeContext` usar comparando el `LocalPort` de la conexión entrante con `runtime.Config.Port`.

## Helpers compartidos relevantes
- `VirtualDisplayPlacementOptions`: centraliza normalización, etiqueta visible y cálculo de posición del monitor virtual.
- `NetworkAddressHelper`: centraliza IP local y construcción de URLs HTTP de acceso.

## Convenciones útiles para futuras IAs

### Si el cambio afecta...
- **creación del monitor virtual** -> revisar `VirtualDisplayManager.cs`
- **captura o cursor** -> revisar `CaptureService.cs`
- **negociación WebRTC** -> revisar `WebRtcStreamService.cs` y `BuildRtcPage(...)`
- **UI/configuración** -> revisar `VirtualDisplayTrayController.cs`
- **defaults, JSON o migración** -> revisar `VirtualScreenSettingsStore.cs` y `VirtualWebDisplaySettings.cs`
- **perfiles de dispositivos** -> revisar `VirtualDisplayProfiles.cs`
- **HTML servido al navegador** -> revisar `Program.cs`

### Suposición operativa correcta
El programa no busca “streaming multimedia genérico”, sino **usar navegadores de dispositivos secundarios como extensión de pantallas virtuales Windows**. Ese objetivo debe guiar cualquier cambio futuro.