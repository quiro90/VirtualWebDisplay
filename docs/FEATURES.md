# 📋 Características - VirtualWebDisplay

## Tabla de Contenidos

1. [Visión General](#visión-general)
2. [Pantallas Virtuales](#pantallas-virtuales)
3. [Modos de Transmisión](#modos-de-transmisión)
4. [Configuración](#configuración)
5. [Acceso Remoto](#acceso-remoto)
6. [Optimizaciones de Rendimiento](#optimizaciones-de-rendimiento)
7. [Seguridad](#seguridad)
8. [Entrada Táctil](#entrada-táctil)

---

## Visión General

VirtualWebDisplay permite crear hasta **2 monitores virtuales** en Windows y transmitir su contenido a través de un navegador web usando dos tecnologías:

- **JPEG Polling (Web Image)**: Compatible con todos los navegadores, ideal para dashboards y monitoreo
- **WebRTC**: Latencia ultra-baja (~30-50ms), ideal para gaming, presentaciones y aplicaciones interactivas

---

## Pantallas Virtuales

### Características de Pantallas Virtuales

#### 1. **Resoluciones Soportadas**

Perfiles predefinidos:

| Resolución | Nombre Común | Aspecto | Uso Recomendado |
|------------|--------------|---------|-----------------|
| 1280×720 | 720p / HD | 16:9 | Dispositivos móviles, bajo ancho de banda |
| 1920×1080 | 1080p / Full HD | 16:9 | **Recomendado** - balance entre calidad y rendimiento |
| 2560×1440 | 2K / QHD | 16:9 | Monitores de alta resolución, trabajo profesional |
| 3840×2160 | 4K / UHD | 16:9 | Máxima calidad (requiere hardware potente) |

**Resoluciones Personalizadas en el Driver**:

El driver Parsec VDD soporta hasta **5 slots de resolución personalizada** que se configuran desde el menú ⚙️ → *Resoluciones personalizadas...*:
- Formato por slot: `Ancho × Alto @ Hz`
- Slot vacío (todos en 0) = ignorado
- Requiere permisos de Administrador para escribir (la app eleva automáticamente vía UAC)
- Los cambios se aplican al reiniciar el driver Parsec VDD

#### 2. **Posicionamiento**

Las pantallas virtuales pueden posicionarse relativamente al monitor primario:

```
┌─────────────┐
│   Above     │
│             │
└─────────────┘
┌──────┬─────────────┬───────┐
│ Left │   Primary   │ Right │
│      │             │       │
└──────┴─────────────┴───────┘
       ┌─────────────┐
       │    Below    │
       │             │
       └─────────────┘
```

**Opciones**:
- **Right**: A la derecha del monitor primario (default)
- **Left**: A la izquierda del monitor primario
- **Above**: Encima del monitor primario
- **Below**: Debajo del monitor primario
- **Duplicate**: Captura el monitor primario existente sin crear hardware virtual (no genera monitor nuevo)

**Offsets Personalizados**:
- `OffsetX`: Desplazamiento horizontal en píxeles (puede ser negativo)
- `OffsetY`: Desplazamiento vertical en píxeles (puede ser negativo)

Ejemplo:
```json
{
  "Placement": "Right",
  "OffsetX": 100,   // 100px a la derecha de posición calculada
  "OffsetY": -50    // 50px arriba de posición calculada
}
```

#### 3. **Rotación de Stream (removida)**

La rotación de stream fue eliminada del flujo activo para simplificar el mapeo de coordenadas y evitar desalineaciones entre visualización y entrada táctil.

La orientación actual depende del monitor/resolución configurada y del ajuste visual del navegador (`BrowserImageFit`).

#### 4. **Múltiples Pantallas**

Hasta **2 pantallas virtuales simultáneas** con configuración independiente:

**Screen 1**:
```
Resolution: 1920×1080
Port: 5000
Mode: WebRTC
Position: Right
```

**Screen 2**:
```
Resolution: 1280×720
Port: 6000
Mode: Web Image
Position: Above
```

Cada pantalla tiene:
- ✅ Puerto HTTP independiente
- ✅ Configuración de calidad/intervalo independiente
- ✅ Modo de transmisión independiente
- ✅ Posicionamiento independiente
- ✅ Control táctil independiente (`TouchInputEnabled`)

---

## Modos de Transmisión

### 1. Web Image (JPEG Polling)

**Descripción**: El navegador solicita frames JPEG periódicamente mediante HTTP polling.

**Ventajas**:
- ✅ Compatible con **todos los navegadores** (incluso antiguos)
- ✅ No requiere HTTPS (funciona con HTTP simple)
- ✅ Implementación simple
- ✅ Bajo uso de CPU

**Desventajas**:
- ❌ Mayor latencia (~100-200ms)
- ❌ Overhead HTTP en cada request
- ❌ No ideal para contenido de movimiento rápido

**Configuración Recomendada**:

```json
{
  "TransmissionMode": "WebImage",
  "CaptureIntervalSeconds": 0.10, // 10 FPS (adecuado para dashboards)
  "JpegQuality": 85                // Alta calidad
}
```

**Casos de Uso**:
- 📊 Dashboards y monitoring
- 📈 Gráficas estáticas/semi-estáticas
- 🖥️ Escritorios remotos de baja frecuencia
- 📱 Dispositivos con navegadores antiguos

**Implementación Técnica**:

```javascript
// Cliente (navegador) — el token se embebe en el HTML generado por el servidor
function updateImage() {
    const img = document.getElementById('screen');
    img.src = `/cap/${capToken}?s=${++seq}`;  // token de instancia + seq previene caché
    setTimeout(updateImage, 100);              // Polling cada 100ms
}
```

**Endpoints**:
- `GET /cap/{token}`: Frame JPEG actual — `{token}` es el `CapToken` de instancia
- `GET /mjpeg`: Stream MJPEG continuo (multipart/x-mixed-replace)

---

### 2. WebRTC (Real-Time Communication)

**Descripción**: Streaming en tiempo real usando RTCPeerConnection con `VideoTrack` H.264 (RTP).

**Ventajas**:
- ✅ **Latencia ultra-baja** (~30-50ms)
- ✅ Protocolo optimizado para streaming
- ✅ Múltiples peers eficientes
- ✅ Ideal para contenido dinámico

**Desventajas**:
- ❌ Solo navegadores modernos (Chrome, Edge, Firefox, Safari)
- ❌ **Requiere HTTPS** (requisito de seguridad de WebRTC)
- ❌ Mayor complejidad de implementación
- ❌ Ligeramente mayor uso de CPU (gestión de peers)

**Configuración Recomendada**:

```json
{
  "TransmissionMode": "RTC",
  "H264Framerate": 30,
  "H264BitrateKbps": 2000
}
```

**Casos de Uso**:
- 🎮 Gaming / Cloud gaming
- 🎥 Presentaciones en vivo
- 🖱️ Aplicaciones interactivas
- 🎬 Edición de video en tiempo real

**Implementación Técnica**:

**Negociación SDP**:
```javascript
// 1. Cliente crea offer
const pc = new RTCPeerConnection();
pc.addTransceiver('video', { direction: 'recvonly' });
const offer = await pc.createOffer();
await pc.setLocalDescription(offer);

// 2. Envía offer al servidor
const response = await fetch('/webrtc/offer', {
    method: 'POST',
    body: JSON.stringify({ sdp: offer.sdp })
});
const answer = await response.json();

// 3. Aplica answer
await pc.setRemoteDescription({ type: 'answer', sdp: answer.sdp });
```

**Recepción de Video**:
```javascript
pc.ontrack = (event) => {
   if (event.track.kind === 'video') {
      const video = document.getElementById('screen');
      video.srcObject = event.streams[0] ?? new MediaStream([event.track]);
      video.play().catch(() => {});
    }
};
```

**Optimizaciones**:

1. **Codificación H.264**:
  - Encoder automático: NVENC → AMF → libx264.
  - Menor ancho de banda para misma calidad visual.

2. **Peers Concurrentes**:
   - Servidor gestiona diccionario de peers conectados
  - Cada NAL unit se transmite a todos los peers activos
   - Limpieza automática de peers desconectados

---

## Configuración

### Opciones de Captura

#### 1. **Intervalo de Captura** (`CaptureIntervalSeconds`)

Segundos entre capturas de pantalla.

| Intervalo | FPS Equivalente | Uso Recomendado | CPU Usage |
|-----------|-----------------|-----------------|-----------|
| 0.016s | ~60 FPS | Gaming, máxima fluidez | Alto |
| 0.033s | ~30 FPS | **Recomendado** - presentaciones, videos | Medio |
| 0.050s | 20 FPS | Uso general, aplicaciones | Bajo |
| 0.100s | 10 FPS | Dashboards, monitoreo | Muy bajo |
| 0.200s | 5 FPS | Contenido estático | Mínimo |

**Valores Permitidos**: > 0 (recomendado 0.016 a 0.5)

**Fórmula**:
```
FPS = 1 / CaptureIntervalSeconds
```

**Ejemplo**:
```json
{
  "CaptureIntervalSeconds": 0.033  // 30 FPS
}
```

#### 2. **Calidad JPEG** (`JpegQuality`)

Nivel de compresión JPEG (1-100).

| Calidad | Tamaño de Frame (1080p) | Calidad Visual | Uso Recomendado |
|---------|-------------------------|----------------|-----------------|
| 95-100 | ~500-800 KB | Excelente | Fotografía, diseño gráfico |
| 85-94 | ~200-400 KB | Muy buena | **Recomendado** - uso general |
| 75-84 | ~100-200 KB | Buena | Default - balance óptimo |
| 60-74 | ~50-100 KB | Aceptable | Bajo ancho de banda |
| 1-59 | ~20-50 KB | Pobre | Solo para testing |

**Valores Permitidos**: 1-100

**Trade-off**:
- ⬆️ Mayor calidad = ⬆️ Tamaño frame = ⬆️ Ancho de banda = ⬆️ Tiempo de codificación
- ⬇️ Menor calidad = ⬇️ Tamaño frame = ⬇️ Ancho de banda = ⬇️ Tiempo de codificación

**Ejemplo**:
```json
{
  "JpegQuality": 75  // Default
}
```

#### 3. **Detección de Cambios**

**Automático** - No configurable manualmente.

**Funcionamiento**:
- Calcula hash FNV-1a de muestras de píxeles (~1% del frame)
- Solo codifica JPEG si hash difiere del frame anterior
- **Beneficio**: Ahorra ~80-90% CPU cuando pantalla está estática

**Nota**:
```csharp
// La codificación JPEG ahora es bajo demanda en DxgiCaptureService.
// Se activa cuando hay polling /cap reciente o consumidores /mjpeg.
```

---

### Opciones de Red

#### 1. **Puerto HTTP** (`HttpPort`)

Puerto para servidor web HTTP/HTTPS.

- **HTTP**: `HttpPort` (ejemplo: 5000)
- **HTTPS**: `HttpPort + 1` (ejemplo: 5001)

**Valores Permitidos**: 1024-65535

**Detección de Conflictos**:
- Si puerto está en uso, la aplicación mostrará error
- Cambiar a puerto diferente en configuración

**Ejemplo**:
```json
{
  "HttpPort": 5000  // HTTP: 5000, HTTPS: 5001
}
```

#### 2. **Certificado SSL**

**Generación Automática**:
- Ubicación: `C:\Users\<Usuario>\.virtualwebdisplay\localhost.pfx`
- Algoritmo: RSA 2048 bits
- Validez: 10 años
- **Subject Alternative Names (SANs)**:
  - `localhost`
  - IP local (ejemplo: `192.168.1.100`)
  - `127.0.0.1`

**Instalación Manual**:
1. Descargar: `https://localhost:5001/cert`
2. Guardar `localhost.cer`
3. Doble click → Instalar certificado
4. Store: **Trusted Root Certification Authorities**

**Regeneración**:
- Eliminar `localhost.pfx`
- Reiniciar aplicación (generará nuevo certificado)

---

## Acceso Remoto

### Desde Dispositivo Local

```
https://localhost:5001
```

### Desde Otro Dispositivo en Red Local

**Paso 1**: Obtener IP local

La aplicación muestra IP automáticamente en la página web:

```
Available at:
- https://localhost:5001
- https://192.168.1.100:5001  ← Usar esta desde otro dispositivo
```

**Paso 2**: Configurar Firewall

Windows Firewall puede bloquear acceso externo. Permitir puertos:

```powershell
# Permitir puerto HTTPS (5001)
New-NetFirewallRule -DisplayName "VirtualWebDisplay HTTPS" `
                    -Direction Inbound `
                    -LocalPort 5001 `
                    -Protocol TCP `
                    -Action Allow
```

**Paso 3**: Instalar Certificado en Dispositivo Remoto

1. En dispositivo remoto, navegar a: `https://<IP>:5001/cert`
2. Descargar e instalar certificado
3. Reiniciar navegador

---

### Acceso desde Internet (Avanzado)

**Advertencia**: Exponer servicio a Internet requiere precauciones de seguridad.

**Opción 1: Port Forwarding en Router**

1. Configurar router para forwardear puerto externo → 5001 interno
2. Obtener IP pública: https://www.whatismyip.com/
3. Acceder: `https://<IP_PUBLICA>:<PUERTO_EXTERNO>`

**Opción 2: Túnel Reverse (Más Seguro)**

Usar servicio de túnel como **ngrok**:

```powershell
ngrok http https://localhost:5001
```

Ngrok proporcionará URL pública temporal:
```
https://abc123.ngrok.io → https://localhost:5001
```

---

## Optimizaciones de Rendimiento

### 1. Detección de Cambios en Frames

**Algoritmo Hash FNV-1a**:

```csharp
// Muestrea ~1% de píxeles distribuidos uniformemente
const int sampleInterval = 100;
ulong hash = 14695981039346656037UL;  // FNV offset basis

for (int y = 0; y < height; y += sampleInterval)
{
    for (int x = 0; x < width; x += sampleInterval)
    {
        Color pixel = bitmap.GetPixel(x, y);
        hash ^= (ulong)pixel.ToArgb();
        hash *= 1099511628211UL;  // FNV prime
    }
}

bool changed = (hash != _previousHash);
```

**Beneficio**:
- ✅ ~100x más rápido que codificación JPEG completa
- ✅ Ahorra 80-90% CPU cuando pantalla estática
- ✅ Overhead insignificante (~2-3ms)

---

### 2. Caché de Codec JPEG

```csharp
// Búsqueda única (en constructor)
private static readonly ImageCodecInfo _jpegCodec = ImageCodecInfo
    .GetImageEncoders()
    .First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

// Reutilización en cada frame (sin búsqueda repetida)
bitmap.Save(memoryStream, _jpegCodec, encoderParameters);
```

**Beneficio**:
- ✅ Elimina búsqueda de codec en cada frame (~5-10ms ahorrados por frame)

---

### 3. WebRTC H.264 Optimizado

**Configuración**:
```csharp
new MediaStreamTrack(h264Format, MediaStreamStatusEnum.SendOnly)
```

**Comportamiento**:

| Configuración | Latencia | Calidad | Uso |
|---------------|----------|---------|-----|
| `H264Framerate=20, H264BitrateKbps=1200` | ~60-90ms | Buena | Bajo ancho de banda |
| `H264Framerate=30, H264BitrateKbps=2000` | **~30-60ms** | Muy buena | ✅ Recomendado |
| `H264Framerate=60, H264BitrateKbps=4000` | ~20-50ms | Alta | Equipos potentes |

**Trade-off**:
- ⬇️ Latencia mínima
- ⬇️ CPU en cliente al usar `<video>` nativo

---

### 4. JPEG Bajo Demanda

**Comportamiento**:
- `/cap/{token}` marca demanda reciente de JPEG.
- `/mjpeg` mantiene demanda activa mientras el stream esté abierto.
- En modo solo WebRTC sin consumidores JPEG, se evita la codificación JPEG continua.

**Razones**:
- ✅ Menor que límite de DataChannel (~256 KB)
- ✅ Tamaño óptimo para redes con alta latencia
- ✅ Balance entre overhead de chunks y eficiencia de transmisión

**Estructura**:
```
[4 bytes: frameId][64 KB: datos JPEG]
[4 bytes: frameId][64 KB: datos JPEG]
...
[4 bytes: frameId][restantes: datos JPEG]
```

---

## Seguridad

### 1. Certificado SSL con SANs

**Generación**:
```csharp
var sanBuilder = new SubjectAlternativeNameBuilder();
sanBuilder.AddDnsName("localhost");
sanBuilder.AddIpAddress(IPAddress.Parse("127.0.0.1"));
sanBuilder.AddIpAddress(localIpAddress);  // IP local detectada

certificate.Extensions.Add(sanBuilder.Build());
```

**Beneficio**:
- ✅ Navegadores modernos requieren SAN (Subject Alternative Name)
- ✅ Certificado válido para `localhost` **y** IP local
- ✅ Elimina warning "NET::ERR_CERT_COMMON_NAME_INVALID"

---

### 2. Prevención de Instancias Múltiples

**Mutex Global**:
```csharp
_mutex = new Mutex(true, "Global\\VirtualWebDisplay_SingleInstance", out bool createdNew);

if (!createdNew)
{
    MessageBox.Show("Another instance is already running.");
    return false;
}
```

**Beneficio**:
- ✅ Previene conflictos de puerto
- ✅ Previene conflictos de driver Parsec VDD
- ✅ Evita confusión con múltiples tray icons

---

### 3. Seguridad por pantalla (clave dinámica)

**Comportamiento actual**:
- Cada pantalla (`Screen1`/`Screen2`) tiene check `ScreenSecurityEnabled`.
- Si está activo, al iniciar runtime se genera una clave alfanumérica aleatoria de 6 caracteres.
- El host muestra login en `/` hasta que el cliente se autentica.
- Endpoints protegidos por auth cuando seguridad está activa: `/cap/{token}`, `/mjpeg`, `/webrtc/offer`, `/config`.

**Límite de intentos**:
- 5 intentos por cliente/IP en ventana de 45 segundos.
- Al superar el límite, responde `429` con tiempo de espera.

**Beneficio**:
- ✅ Evita acceso casual desde otros dispositivos de la red local
- ✅ Seguridad independiente por pantalla
- ✅ No expone contenido sin clave válida

---

## Características Avanzadas (Futuro)

### Planificado para v1.1+

- [x] **H.264 Hardware/Software Encoding**: NVENC/AMF/libx264 con fallback automático
- [ ] **Audio Streaming**: Captura y transmisión de audio de pantalla virtual
- [ ] **Input remoto avanzado**: Arrastre, teclado y gestos extendidos
- [ ] **3+ Pantallas**: Soporte para más de 2 pantallas simultáneas
- [ ] **Modo Espejo**: Duplicar monitor real (no solo virtuales)
- [ ] **Grabación de Sesión**: Guardar stream a archivo MP4
- [x] **Autenticación por clave local**: Protección por pantalla con clave dinámica y rate limiting
- [x] **Entrada táctil remota**: Clicks táctiles con control por pantalla

---

## Limitaciones Conocidas

1. **Solo Windows**: Requiere Windows 10/11 para Parsec VDD driver
2. **Máximo 2 Pantallas**: Limitación actual de implementación (no del driver)
3. **Input remoto limitado**: Hoy soporta clicks táctiles (1/2/3+ dedos), no teclado ni drag completo
4. **El modo WebImage sigue usando JPEG**: para e-ink y clientes simples
5. **WebRTC Requiere HTTPS**: Requisito de seguridad de navegadores

---

## Entrada Táctil

### Descripción General

VirtualWebDisplay soporta entrada táctil remota desde dispositivos móviles/tablets hacia la pantalla virtual de Windows. Los eventos táctiles se traducen a eventos de mouse nativos de Windows.

### Modos de Entrada Táctil

**Dos modos mutuamente exclusivos** configurables por pantalla desde la UI:

#### 1. **Tap only (cursor not affected)**
- Solo taps/clicks, sin gestos de drag ni scroll
- **Cursor NO se mueve** al tocar (preserva posición original)
- Ideal para: Dashboards estáticos, botones, interfaces sin scroll

**Gestos soportados**:
- 1 dedo tap: click izquierdo
- 2 dedos tap: click derecho
- 3+ dedos tap: click central

#### 2. **Gestures (cursor affected)**
- Taps, drag y scroll completos
- **Cursor SE MUEVE** a la posición tocada
- Ideal para: Interfaces interactivas, navegación completa, gaming casual

**Gestos soportados**:
- 1 dedo tap: click izquierdo
- 1 dedo hold + drag: arrastrar (drag)
- 2 dedos tap: click derecho
- 2 dedos hold + drag: scroll vertical y horizontal (ambos sentidos, inversión natural)
- 3+ dedos tap: click central
- Umbral de activación: `TouchGestureHoldDelayMs` (300ms por defecto)

### Configuración por Pantalla

**Controles en UI**:
- **Toggle Táctil/Normal**: Activa/desactiva entrada táctil globalmente para esa pantalla
- **ComboBox de Modo**: Selecciona entre "Tap only" o "Gestures"
- **Tiempo de Hold (ms)**: Configurable solo en modo Gestures (300ms por defecto)

**Hot-Reload**: Todos los cambios se aplican **al instante sin reiniciar** el servicio.

**Campos de Configuración**:
```json
{
  "TouchInputEnabled": true,           // Activa/desactiva entrada táctil
  "TouchGesturesEnabled": true,        // true=Gestures, false=Tap only
  "TouchPreserveCursor": false,        // true=Tap only, false=Gestures
  "TouchGestureHoldDelayMs": 300       // Umbral para activar drag/scroll
}
```

**Relación entre campos**:
| Modo UI | TouchGesturesEnabled | TouchPreserveCursor |
|---------|---------------------|---------------------|
| Tap only | `false` | `true` |
| Gestures | `true` | `false` |

### Implementación Técnica

**Endpoints**:
- `POST /input/touch`: Recibe eventos táctiles del cliente
- `GET /input/stats`: Métricas de entrada (eventos/segundo, latencia promedio, errores)

**Estructura de Request**:
```json
{
  "fingerCount": 1,
  "action": "tap",           // tap, dragStart, drag, dragEnd, scrollStart, scroll, scrollEnd
  "normalizedX": 0.5,        // 0.0-1.0
  "normalizedY": 0.5,        // 0.0-1.0
  "deltaX": 0,               // Para scroll
  "deltaY": 0                // Para scroll
}
```

**Procesamiento Backend**:
```csharp
// En InputHandler.cs
if (!config.TouchInputEnabled)
    return Results.NoContent();  // Gate global

if (!config.TouchGesturesEnabled && isGesture)
    return Results.NoContent();  // Solo permite taps

// Conversión de coordenadas normalizadas a píxeles
int pixelX = (int)(normalizedX * screenWidth);
int pixelY = (int)(normalizedY * screenHeight);

// Ejecución según modo
if (config.TouchPreserveCursor)
    MouseInputHelper.ClickPreservingCursor(type, pixelX, pixelY);
else
    MouseInputHelper.Click(type, pixelX, pixelY);
```

### Compatibilidad iPad/Safari (WebImage)

**Problema**: Safari en iOS tiene comportamiento nativo de drag-and-drop y long-press sobre imágenes.

**Solución**: WebImage renderiza la vista como `div#screen` con `background-image` en lugar de `<img>`.

**Prevención de eventos nativos**:
```javascript
// En wwwroot/js/touch/touch-input.js
canvas.addEventListener('touchstart', (e) => e.preventDefault(), { passive: false });
canvas.addEventListener('touchmove', (e) => e.preventDefault(), { passive: false });
canvas.addEventListener('contextmenu', (e) => e.preventDefault());
canvas.addEventListener('dragstart', (e) => e.preventDefault());
```

### Scroll Natural (Inversión)

**Comportamiento**: El scroll sigue la dirección natural del dedo (como en dispositivos móviles).

- Drag hacia **abajo** con 2 dedos → scroll hacia **abajo**
- Drag hacia **arriba** con 2 dedos → scroll hacia **arriba**
- Drag hacia **derecha** con 2 dedos → scroll hacia **derecha**
- Drag hacia **izquierda** con 2 dedos → scroll hacia **izquierda**

**Implementación**:
```javascript
// Inversión en el cliente (wwwroot/js/touch/touch-input.js)
const deltaX = -(currentX - lastX);  // Invertido
const deltaY = -(currentY - lastY);  // Invertido

// Backend traduce a scroll de Windows (Controllers/Handlers/InputHandler.cs)
MouseInputHelper.Scroll(deltaX, deltaY);
```

### Métricas y Estadísticas

**Endpoint**: `GET /input/stats`

**Respuesta**:
```json
{
  "eventsPerSecond": 15.3,
  "avgLatencyMs": 12.5,
  "totalEvents": 4523,
  "errorCount": 2,
  "rateLimitHits": 0,
  "lastEventTimestamp": "2025-01-15T14:30:45Z"
}
```

### Limitaciones Actuales

1. **No soporta teclado**: Solo entrada táctil/mouse
2. **Drag limitado a 1 dedo**: No soporta multi-touch drag simultáneo
3. **Zoom no soportado**: Pinch-to-zoom no traduce a zoom de Windows
4. **Rotación no soportada**: Gestos de rotación no implementados

### Futuro Planificado

- [ ] **Teclado virtual**: Entrada de texto desde dispositivos móviles
- [ ] **Zoom por pinch**: Traducción a zoom de Windows (Ctrl + Mouse Wheel)
- [ ] **Rotación**: Gestos de rotación de 2 dedos
- [ ] **Multi-touch avanzado**: Soporte para 4+ dedos simultáneos
- [x] **Hot-reload**: Cambios en vivo sin reiniciar ✅
- [x] **Modo Tap only vs Gestures**: Control fino del comportamiento del cursor ✅
- [x] **Scroll bidireccional**: Horizontal y vertical simultáneo ✅

---

Para más detalles técnicos, ver **ARCHITECTURE.md** y **DEVELOPMENT.md**.
