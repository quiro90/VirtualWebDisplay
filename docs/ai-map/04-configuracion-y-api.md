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
`VirtualScreenSettingsStore` sabe leer un formato legado con sección `VirtualScreen` y migrarlo a:
- `Screen1 = legacyConfig`
- `Screen2 = CreateScreen2Defaults()`

## `VirtualScreenConfig`

### Campos funcionales
| Campo | Default | Descripción |
|---|---|---|
| `Enabled` | true | Si se crea esta pantalla |
| `Profile` | "" | Id del perfil (ver VirtualDisplayProfiles) |
| `Landscape` | false | Rota el perfil a landscape |
| `CustomWidth` / `CustomHeight` | 800/1280 | Tamaño cuando Profile = Custom |
| `Width` / `Height` | 800/1280 | Tamaño efectivo final (calculado) |
| `Port` | 8000 | Puerto HTTP propio de la pantalla |
| `TransmissionMethod` | "Rtc" | `WebImage` o `Rtc` |
| `CaptureIntervalSeconds` | 0.25 | Ritmo de generación/emisión (compartido por ambos modos) |
| `JpegQuality` | 40 | Calidad de compresión 10-100 (compartido por ambos modos) |
| `StreamRotationDegrees` | 0 | Rotación del frame capturado: 0, 90, 180 o 270 grados |
| `RotateForPortrait` | false | **Legacy** — migrado a `StreamRotationDegrees`. Se mantiene solo para leer configs antiguas |
| `MonitorIndex` | -1 | -1=auto (VDD creado), 0=primario, 1+=otros |
| `VirtualDisplayPlacement` | "right" | right/left/top/bottom |
| `BrowserImageFit` | "contain" | fill/cover/contain (CSS object-fit en el navegador) |
| `ScreenSecurityEnabled` | false | Activa protección por clave de 6 caracteres para esa pantalla |

### `BrowserImageFit` — valores y efecto visual
- `fill`: estira la imagen para llenar toda la pantalla del cliente (sin barras, puede deformar si hay diferencia de proporción)
- `cover`: llena toda el área recortando los bordes sobrantes
- `contain`: muestra toda la imagen preservando proporción, puede mostrar franjas negras

### Defaults por pantalla
- **Screen1**: `Enabled=true`, `Port=8000`, `VirtualDisplayPlacement="right"`
- **Screen2**: `Enabled=false`, `Port=8002`, `VirtualDisplayPlacement="left"`, `TransmissionMethod=Rtc`

## Perfiles conocidos
Definidos en `VirtualDisplayProfiles.All`. Todos en portrait; se rotan si `Landscape=true`.

| Id | Resolución |
|---|---|
| `1200x1920` | 1200 × 1920 |
| `1200x1800` | 1200 × 1800 |
| `1200x1600` | 1200 × 1600 |
| `1152x2048` | 1152 × 2048 |
| `1080x3840` | 1080 × 3840 |
| `1080x2560` | 1080 × 2560 |
| `1080x1920` | 1080 × 1920 **(recomendada)** |
| `1050x1680` | 1050 × 1680 |
| `900x1600` | 900 × 1600 |
| `900x1440` | 900 × 1440 |
| `800x1280` | 800 × 1280 |
| `768x1366` | 768 × 1366 |
| `720x1280` | 720 × 1280 |
| `Custom` | Personalizado (usa CustomWidth/CustomHeight) |

## Modos de transmisión

### `WebImage`
- HTML con `<img>` + polling a `/cap`
- simple y compatible con cualquier navegador
- intervalo configurable (`CaptureIntervalSeconds`)
- recomendado para Kindle / e-ink

### `Rtc`
- HTML con JS WebRTC + `RTCDataChannel`
- menor latencia percibida
- mismo `CaptureIntervalSeconds` y `JpegQuality` que WebImage
- recomendado para tablets

### Nota sobre el modo Rtc y WebRTC
Ambos modos usan los mismos parámetros de captura (`CaptureIntervalSeconds`, `JpegQuality`). No hay captura separada por protocolo. La diferencia es solo en el mecanismo de entrega al navegador.

## Endpoints expuestos

### `GET /`
Devuelve la página HTML cliente para la pantalla correspondiente al puerto local donde entró la solicitud.
- Si `ScreenSecurityEnabled=true` y el cliente no está autenticado: responde una página de login por clave.
- Si el runtime usa `WebImage`: responde con `WebImagePageTemplate`.
- Si el runtime usa `Rtc`: responde con `RtcPageTemplate`.
- Ambas páginas aplican `BrowserImageFit` vía CSS `object-fit`.

### `GET /cap`
Devuelve el último frame JPEG capturado. `Cache-Control: no-store, no-cache`.

Requiere autenticación previa cuando `ScreenSecurityEnabled=true`.

### `GET /mjpeg`
Stream multipart MJPEG continuo. Comparte el frame de `CaptureService`.

Requiere autenticación previa cuando `ScreenSecurityEnabled=true`.

### `POST /webrtc/offer`
Recibe una oferta SDP y devuelve la respuesta SDP.

Requiere autenticación previa cuando `ScreenSecurityEnabled=true`.

#### Request esperado
```json
{ "sdp": "...", "type": "offer" }
```

#### Response esperado
```json
{ "sdp": "...", "type": "answer", "peerId": "..." }
```

Solo disponible si `TransmissionMethod = Rtc`. Devuelve 400 si se invoca en modo WebImage.

### `POST /auth/login`
Valida la clave de acceso para la pantalla (si seguridad activa) y crea cookie de sesión HTTP-only.

Reglas:
- clave correcta: autoriza y devuelve `200`.
- clave incorrecta: `401`.
- límite: 5 intentos por cliente/IP, ventana de 45 segundos.
- al superar límite: `429` con tiempo de espera.

### `GET /config`
Devuelve metadata de runtime (requiere auth si seguridad activa):
```json
{
  "displayName": "Pantalla 1",
  "config": { ... },
  "hostUrl": "http://hostname:8000",
  "ipUrl": "http://192.168.x.x:8000"
}
```

## Resolución del runtime por puerto
La app escucha en varios puertos a la vez. `ResolveRuntime(HttpContext)` decide qué `ScreenRuntimeContext` usar comparando el `LocalPort` de la conexión entrante con `runtime.Config.Port`. Si ninguno coincide, usa el primero.

## Helpers compartidos relevantes
- `VirtualDisplayPlacementOptions`: normalización (acepta español e inglés), etiqueta visible y cálculo de posición Win32 del monitor virtual.
- `NetworkAddressHelper`: detección de IP local y construcción de URLs HTTP de acceso.
- `TransmissionModeOptions`: constantes, normalización, validación de rangos (`CaptureIntervalSeconds`, `JpegQuality`).

## Convenciones útiles para futuras IAs

### Si el cambio afecta...
- **creación del monitor virtual** -> `VirtualDisplayManager.cs`
- **captura, cursor o rotación** -> `CaptureService.cs`
- **negociación WebRTC** -> `WebRtcStreamService.cs` y `BuildRtcPage(...)` en `Program.cs`
- **HTML servido al navegador / ajuste visual** -> `BuildWebImagePage` / `BuildRtcPage` en `Program.cs`
- **UI/configuración del tray** -> `VirtualDisplayTrayController.cs`
- **defaults, JSON o migración** -> `VirtualScreenSettingsStore.cs` y `VirtualWebDisplaySettings.cs`
- **perfiles de dispositivos o resoluciones** -> `VirtualDisplayProfiles.cs`
- **placement/posición del monitor** -> `VirtualDisplayPlacementOptions.cs`
- **modos WebImage/Rtc, intervalos, calidad JPEG** -> `TransmissionModeOptions.cs`

### Suposición operativa correcta
El programa no busca "streaming multimedia genérico", sino **usar navegadores de dispositivos secundarios como extensión de pantallas virtuales Windows**. Ese objetivo debe guiar cualquier cambio futuro.
