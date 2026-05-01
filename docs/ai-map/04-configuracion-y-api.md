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
| `MaxViewers` | 1 | Máximo de viewers simultáneos (`0` = sin límite) |
| `TouchInputEnabled` | false | Habilita touch remoto para esa pantalla (editable en caliente, por pantalla, sin reinicio) |
| `TouchPreserveCursor` | false | Preserva posición del cursor al tocar (editable en caliente, true = cursor no se mueve) |
| `TouchZoomEnabled` | true | Habilita gesto de zoom/pellizco (editable en caliente) |
| `TouchZoomDelayMs` | 50 | Tiempo (ms) para activar zoom (editable en caliente) |
| `TouchHoldEnabled` | true | Habilita mantener toque para drag (editable en caliente) |
| `TouchHoldDelayMs` | 250 | Tiempo (ms) de presión para activar drag (editable en caliente) |
| `TouchScrollEnabled` | true | Habilita scroll con dos dedos (editable en caliente) |
| `TouchScrollDelayMs` | 250 | Tiempo (ms) de presión para activar scroll (editable en caliente) |
| `MonitorIndex` | -1 | -1=auto (VDD creado), 0=primario, 1+=otros |
| `VirtualDisplayPlacement` | "right" | right / left / top / bottom / **duplicate** |
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
- HTML con `div#screen` + polling a `/cap`
- simple y compatible con cualquier navegador
- intervalo configurable (`CaptureIntervalSeconds`)
- recomendado para Kindle / e-ink

Nota iPad/Safari:
- Se usa `div` con `background-image` para evitar drag-and-drop/long-press nativo sobre imágenes.

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

### `POST /input/touch`
Recibe eventos touch del cliente para emular clicks de mouse en la pantalla remota.

Reglas:
- 1 dedo: click izquierdo
- 2 dedos: click derecho
- 3+ dedos: click central
- Si `TouchInputEnabled=false`, el backend ignora los eventos (`204`).

### `GET /input/stats`
Devuelve métricas agregadas de touch (`eventsPerSecond`, `avgLatencyMs`, errores, rate limit, etc.).

## Límite de receptores por pantalla
- `VirtualScreenConfig.MaxViewers` define el máximo simultáneo por pantalla.
- `0` significa sin límite.
- si el cupo ya fue alcanzado, `GET /` devuelve una página informativa y no llega a mostrar login aunque `ScreenSecurityEnabled=true`.
- `GET /cap`, `GET /mjpeg` y `POST /webrtc/offer` responden `429` cuando ya no se puede aceptar otro viewer.
- `ViewerLimiter` contabiliza polling activo de `WebImage`, conexiones `MJPEG` abiertas y peers `WebRTC` activos.

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

## Resoluciones personalizadas del driver Parsec VDD

El driver Parsec VDD soporta hasta **5 slots** de resoluciones personalizadas almacenados en el registro de Windows.

### Registro
- Ruta: `HKLM\SOFTWARE\Parsec\vdd\{0..4}`
- Valores por slot: `width` (DWORD), `height` (DWORD), `hz` (DWORD)
- Requiere permisos de Administrador para escribir.

### Componentes involucrados
- `Parsec/VddCustomModesStore.cs` — lectura/escritura de los slots
- `UI/Forms/CustomModesDialog.cs` — diálogo con 5 slots editables (W×H@Hz)
- `Program.cs` — maneja argumento CLI `--set-custom-modes "<w>x<h>@<hz>;..."` para el flujo UAC

### Argumento CLI UAC
Cuando el usuario guarda desde `CustomModesDialog` sin permisos de administrador, se relanza el proceso con:
```
VirtualWebDisplay.exe --set-custom-modes "1920x1080@60;1280x720@60;..."
```
El proceso elevado escribe al registro y sale. El proceso original detecta el éxito y cierra el diálogo.

### `VirtualDisplayPlacement = "duplicate"`
Cuando `VirtualDisplayPlacement = "duplicate"`, **no se crea ningún monitor virtual**. En su lugar se captura el monitor primario existente en su resolución actual. Útil para transmitir la pantalla principal sin crear hardware virtual.

## Helpers compartidos relevantes
- `VirtualDisplayPlacementOptions`: normalización (acepta español e inglés), etiqueta visible y cálculo de posición Win32 del monitor virtual.
- `NetworkAddressHelper`: detección de IP local y construcción de URLs HTTP de acceso.
- `TransmissionModeOptions`: constantes, normalización, validación de rangos (`CaptureIntervalSeconds`, `JpegQuality`).

## Localización (i18n)

### Sistema de localización
- Basado en archivos `.resx` en `Localization/`
- Idiomas soportados: **Inglés** (EN) y **Español** (ES)
- Cambio de idioma en vivo sin reiniciar la aplicación
- Clase principal: `AppText.cs`

### Métodos principales
```csharp
AppText.Get("Key")                    // Obtiene texto localizado
AppText.Format("Key", arg1, arg2)     // Texto con formato
AppText.ApplyCulture("es")            // Cambia idioma en runtime
AppText.NormalizeLanguage("en")       // Normaliza código de idioma
```

### Claves de localización para UI (relevantes a cambios recientes)

#### Indicadores de pantalla
- `Form_Config_ScreenIndicator_Tooltip`: "Access at: {0}" / "Ingrese a: {0}"
- `Form_Config_ScreenIndicator_UrlCopied`: "URL copied to clipboard" / "URL copiada al portapapeles"

#### Entrada táctil
- `Tab_Section_TouchInput`: "Touch Input" / "Entrada Táctil"
- `Tab_TouchPreserveCursor_Checkbox`: "Preserve cursor position on tap" / "Recordar posición del puntero"

#### Claves obsoletas eliminadas
Las siguientes claves fueron eliminadas de los archivos `.resx` por no tener uso:
- ~~`Tab_TouchInput_Enabled`~~ - Ya no se usa (checkbox ahora tiene texto fijo)
- ~~`Tab_TouchInput_Disabled`~~ - Ya no se usa
- ~~`Tab_AccessUrlPrefix`~~ - Eliminado (URL ahora en indicadores)
- ~~`Tab_Help_AccessUrl`~~ - Eliminado (tooltip ahora en indicadores)

### Convención de nombres
- `Form_Config_*`: Elementos del formulario de configuración principal
- `Tab_*`: Elementos dentro de las tabs de pantalla
- `Tab_Help_*`: Tooltips de ayuda
- `*_Title`, `*_Message`: Títulos y mensajes de diálogos

- `ViewerLimiter`: cupo de viewers y expiración de viewers por polling.

## Convenciones útiles para futuras IAs

### Si el cambio afecta...
- **creación del monitor virtual** -> `VirtualDisplayManager.cs`
- **captura o cursor** -> `CaptureService.cs`
- **negociación WebRTC** -> `WebRtcStreamService.cs` y `UI/HtmlTemplates/RtcPageTemplate.cs`
- **HTML servido al navegador / ajuste visual** -> `UI/HtmlTemplates/WebImagePageTemplate.cs` y `UI/HtmlTemplates/RtcPageTemplate.cs`
- **touch remoto y gestos** -> `Controllers/Handlers/InputHandler.cs` y `UI/HtmlTemplates/TouchInputScriptHelper.cs`
- **seguridad por clave o cupo de viewers** -> `Controllers/Handlers/*`, `ScreenSecurityGate.cs`, `ViewerLimiter.cs`
- **UI/configuración del tray** -> `VirtualDisplayTrayController.cs`
- **defaults, JSON o migración** -> `VirtualScreenSettingsStore.cs` y `VirtualWebDisplaySettings.cs`
- **perfiles de dispositivos o resoluciones** -> `VirtualDisplayProfiles.cs`
- **placement/posición del monitor** -> `VirtualDisplayPlacementOptions.cs`
- **modos WebImage/Rtc, intervalos, calidad JPEG** -> `TransmissionModeOptions.cs`

### Suposición operativa correcta
El programa no busca "streaming multimedia genérico", sino **usar navegadores de dispositivos secundarios como extensión de pantallas virtuales Windows**. Ese objetivo debe guiar cualquier cambio futuro.
