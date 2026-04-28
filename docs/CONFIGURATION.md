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
- Gestos:
  - 1 dedo: click izquierdo (preservando cursor original)
  - 2 dedos: click derecho (preservando cursor original)
  - 3+ dedos: click central (preservando cursor original)
- El flag `TouchInputEnabled` se puede cambiar en caliente desde la UI por pantalla.

## WebImage en iPad/Safari

Para evitar drag-and-drop/long-press nativo de Safari sobre imagenes, WebImage renderiza la vista como una capa `div` con `background-image` en lugar de `<img>`.

## Fuente de verdad para IA

- `docs/ai-map/01-overview.md`
- `docs/ai-map/02-componentes.md`
- `docs/ai-map/03-flujos.md`
- `docs/ai-map/04-configuracion-y-api.md`
