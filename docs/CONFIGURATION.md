# Configuracion - VirtualWebDisplay

## Resumen rapido

- Archivo persistido: `%USERPROFILE%\\.virtualwebdisplay\\virtualscreen.user.json`
- Objeto raiz: `VirtualWebDisplaySettings`
- Pantallas: `Screen1` y `Screen2` (`VirtualScreenConfig`)
- Cada pantalla tiene su propio puerto HTTP (`Port`) y HTTPS en `Port + 1`

## Ejemplo actual

```json
{
  "UiLanguage": "es",
  "WindowTheme": "system",
  "Screen1": {
    "Enabled": true,
    "Width": 1080,
    "Height": 1920,
    "Port": 8000,
    "TransmissionMethod": "Rtc",
    "CaptureIntervalSeconds": 0.25,
    "JpegQuality": 40,
    "MaxViewers": 1,
    "TouchInputEnabled": false,
    "TouchGesturesEnabled": true,
    "TouchPreserveCursor": false,
    "TouchGestureHoldDelayMs": 300,
    "ScreenSecurityEnabled": true,
    "MonitorIndex": -1,
    "VirtualDisplayPlacement": "right",
    "BrowserImageFit": "contain"
  },
  "Screen2": {
    "Enabled": false,
    "Width": 1080,
    "Height": 1920,
    "Port": 8002,
    "TransmissionMethod": "WebImage",
    "CaptureIntervalSeconds": 0.2,
    "JpegQuality": 45,
    "MaxViewers": 0,
    "TouchInputEnabled": true,
    "TouchGesturesEnabled": true,
    "TouchPreserveCursor": false,
    "TouchGestureHoldDelayMs": 300,
    "ScreenSecurityEnabled": false,
    "MonitorIndex": -1,
    "VirtualDisplayPlacement": "left",
    "BrowserImageFit": "cover"
  }
}
```

## Campos importantes por pantalla

| Campo | Descripcion |
|---|---|
| `Enabled` | Activa o desactiva la pantalla virtual |
| `Port` | Puerto HTTP de esa pantalla (HTTPS usa `Port + 1`) |
| `TransmissionMethod` | `WebImage` o `Rtc` |
| `CaptureIntervalSeconds` | Intervalo de captura para ambos modos |
| `JpegQuality` | Calidad JPEG (10..100, validada) |
| `MaxViewers` | Limite de viewers (`0` = sin limite) |
| `TouchInputEnabled` | Habilita/deshabilita touch para esa pantalla (en caliente) |
| `ScreenSecurityEnabled` | Requiere login por codigo de acceso |
| `VirtualDisplayPlacement` | `right`, `left`, `top`, `bottom`, `duplicate` |
| `BrowserImageFit` | `fill`, `cover`, `contain` |

## Defaults relevantes

- `Screen1`: habilitada, `Port=8000`, `VirtualDisplayPlacement=right`
- `Screen2`: deshabilitada, `Port=8002`, `VirtualDisplayPlacement=left`, `TransmissionMethod=Rtc`

## Notas de compatibilidad

- Nombres legacy como `HttpPort`, `TransmissionMode`, `CaptureIntervalMs`, `Rotation` pertenecen a versiones anteriores.
- El archivo actual usa `Port`, `TransmissionMethod`, `CaptureIntervalSeconds` y `BrowserImageFit`.
- La rotacion de stream fue removida del flujo activo.

## Touch input

- Endpoint de entrada: `POST /input/touch`
- Estadisticas: `GET /input/stats`
- **Modos de Entrada Táctil** (configurables por pantalla):
  - **Tap only (cursor not affected)**: Solo taps/clicks sin gestos. El cursor NO se mueve al tocar.
    - 1 dedo tap: click izquierdo
    - 2 dedos tap: click derecho
    - 3+ dedos tap: click central
  - **Gestures (cursor affected)**: Gestos completos con drag, scroll y clicks. El cursor SE MUEVE al tocar.
    - 1 dedo tap: click izquierdo
    - 1 dedo hold + drag: arrastrar (drag)
    - 2 dedos hold + drag: scroll vertical y horizontal (ambos sentidos, inversión natural)
    - 2 dedos tap: click derecho
    - 3+ dedos tap: click central
- **Configuración en Caliente** (sin reiniciar servicio):
  - `TouchInputEnabled`: activa/desactiva entrada táctil por pantalla (Táctil/Normal toggle)
  - `TouchGesturesEnabled`: habilita gestos de arrastre y scroll (cuando modo Gestures seleccionado)
  - `TouchPreserveCursor`: preserva posición del cursor (cuando modo Tap only seleccionado)
  - `TouchGestureHoldDelayMs`: tiempo de hold en ms para activar drag/scroll (300ms por defecto, solo en modo Gestures)

## WebImage en iPad/Safari

Para evitar drag-and-drop/long-press nativo de Safari sobre imagenes, WebImage renderiza la vista como una capa `div` con `background-image` en lugar de `<img>`.

## Fuente de verdad para IA

- `docs/ai-map/01-overview.md`
- `docs/ai-map/02-componentes.md`
- `docs/ai-map/03-flujos.md`
- `docs/ai-map/04-configuracion-y-api.md`
