---
tags: [web, api, endpoints, http]
aliases: [Endpoints, HTTP Endpoints, Rutas, API]
type: referencia
updated: 2026-07-08
---

# Endpoints HTTP

**Registro**: `Web/Api/WebApiEndpoints.cs` (`Map()` recibe el `WebApplication`). Handlers en `Web/Handlers/`, servicios DI en `Web/Services/`.

## Tabla de endpoints

| Endpoint | Método | Descripción | Auth |
|---|---|---|---|
| `/` | GET | Página principal (template según modo) | si seguridad activa |
| `/auth/login` | POST | Login por clave de 6 caracteres | — |
| `/cap/{token}` | GET | Frame JPEG actual | si seguridad activa |
| `/mjpeg` | GET | Stream MJPEG continuo | si seguridad activa |
| `/keepalive` | GET | Mantener viva la sesión/cookie de auth | si seguridad activa |
| `/webrtc/offer` | POST | Negociación SDP (offer → answer) | si seguridad activa |
| `/input/touch` | POST | Entrada táctil remota | — (gate backend) |
| `/input/stats` | GET | Métricas de touch/rate-limit | — |
| `/config` | GET | Metadata de runtime en JSON | si seguridad activa |
| `/cert` | GET | Descarga certificado SSL (`localca.crt`) | — |

## Detalle de respuestas

### `GET /`
Devuelve la página HTML cliente según el puerto local. Si `ScreenSecurityEnabled=true` y no autenticado → página de login. Si límite de viewers alcanzado → página informativa (no llega a login). Según modo: [[WebImage (JPEG Polling)|WebImagePageTemplate]] o [[WebRTC (H.264)|RtcPageTemplate]].

### `GET /cap/{token}`
> [!warning] Token
> `{token}` = [[ScreenRuntimeContext]].`CapToken` (16 chars hex, cambia cada reinicio). Comparación `StringComparison.Ordinal`. Si no coincide → `404`. `Cache-Control: no-store, no-cache`. Acceder sin token correcto **no revela el frame**.

### `POST /webrtc/offer`
Solo disponible si `TransmissionMethod = Rtc`. Devuelve `400` en modo WebImage. Ver [[WebRTC (H.264)]].

### `POST /auth/login`
- Clave correcta → `200` + cookie HTTP-only.
- Incorrecta → `401`.
- Límite: 5 intentos/cliente/IP, ventana 45s → al superar: `429` con tiempo de espera. Ver [[Rate Limiting y Brute Force]].

### `POST /input/touch`
Si `TouchInputEnabled=false` → `204` (ignorado). Ver [[InputHandler (Touch)]].

### `GET /config`
```json
{
  "displayName": "Pantalla 1",
  "config": { ... },
  "hostUrl": "http://hostname:8000",
  "ipUrl": "http://192.168.x.x:8000"
}
```

## Viewer limit (429)

`MaxViewers` alcanzado → `GET /` devuelve página informativa; `/cap`, `/mjpeg` y `/webrtc/offer` responden `429`. Ver [[Límite de Viewers]].

## Resolución del runtime

Cada request resuelve el [[ScreenRuntimeContext]] por puerto — ver [[Resolución de Runtime por Puerto]].

## Enlaces

- [[Modos de Transmisión]]
- [[Seguridad por Pantalla]]
- [[HTML Templates]]