# ⚙️ Configuración - VirtualWebDisplay

> Estado actual (2026-04): este proyecto usa `virtualscreen.user.json` y los campos `Port`, `TransmissionMethod`, `CaptureIntervalSeconds`, `StreamRotationDegrees`, `VirtualDisplayPlacement`, `BrowserImageFit` y `ScreenSecurityEnabled`.
>
> Este documento conserva secciones históricas para referencia. Para cambios nuevos, tomar como fuente de verdad:
> - `docs/ai-map/04-configuracion-y-api.md`
> - `AGENT.md`

## Tabla de Contenidos

1. [Esquema Vigente (importante)](#esquema-vigente-importante)
2. [Ubicación del Archivo](#ubicación-del-archivo)
3. [Estructura JSON](#estructura-json)
4. [Referencia de Campos](#referencia-de-campos)
5. [Ejemplos de Configuración](#ejemplos-de-configuración)
6. [Validación y Valores por Defecto](#validación-y-valores-por-defecto)
7. [Edición Manual](#edición-manual)

---

## Esquema Vigente (importante)

Ruta real de persistencia:
- `%USERPROFILE%\.virtualwebdisplay\virtualscreen.user.json`

Ejemplo actualizado:

```json
{
  "UiLanguage": "es",
  "Screen1": {
    "Enabled": true,
    "Port": 8000,
    "Width": 1080,
    "Height": 1920,
    "TransmissionMethod": "Rtc",
    "CaptureIntervalSeconds": 0.25,
    "JpegQuality": 40,
    "ScreenSecurityEnabled": true,
    "StreamRotationDegrees": 0,
    "VirtualDisplayPlacement": "right",
    "BrowserImageFit": "contain"
  },
  "Screen2": {
    "Enabled": false,
    "Port": 8002,
    "TransmissionMethod": "WebImage",
    "CaptureIntervalSeconds": 0.2,
    "JpegQuality": 45,
    "ScreenSecurityEnabled": false,
    "VirtualDisplayPlacement": "left"
  }
}
```

Notas:
- `ScreenSecurityEnabled=true` activa login por clave al abrir el host de esa pantalla.
- La clave es alfanumérica de 6 caracteres y se genera al iniciar los runtimes.
- Límite anti-force brute: 5 intentos por cliente/IP, ventana de 45 segundos.
- Si ves referencias a `HttpPort`, `TransmissionMode`, `CaptureIntervalMs`, `Rotation`, son nombres históricos.

---

## Ubicación del Archivo

**Ruta**: `C:\Users\<Usuario>\.virtualwebdisplay\virtualscreen.user.json`

**Ejemplos**:
```
C:\Users\Juan Quiroga\.virtualwebdisplay\settings.json
C:\Users\Admin\.virtualwebdisplay\settings.json
```

**Creación Automática**:
- Si el archivo no existe, la aplicación crea configuración por defecto
- La carpeta `.virtualwebdisplay` se crea automáticamente

---

## Estructura JSON

```json
{
  "Screen1": {
    "Enabled": true,
    "Width": 1920,
    "Height": 1080,
    "HttpPort": 5000,
    "TransmissionMode": "RTC",
    "CaptureIntervalMs": 50,
    "JpegQuality": 75,
    "Placement": "Right",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  },
  "Screen2": {
    "Enabled": false,
    "Width": 1280,
    "Height": 720,
    "HttpPort": 6000,
    "TransmissionMode": "WebImage",
    "CaptureIntervalMs": 100,
    "JpegQuality": 70,
    "Placement": "Above",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  }
}
```

---

## Referencia de Campos

### Configuración de Pantalla (`Screen1`, `Screen2`)

#### `Enabled` (booleano)

**Descripción**: Habilita o deshabilita la pantalla virtual.

**Valores**:
- `true`: Pantalla virtual creada al iniciar aplicación
- `false`: Pantalla virtual **no** se crea

**Default**: 
- `Screen1`: `true`
- `Screen2`: `false`

**Ejemplo**:
```json
{
  "Enabled": true
}
```

---

#### `Width` (entero)

**Descripción**: Ancho de la pantalla virtual en píxeles.

**Valores**:
- Mínimo: `640`
- Máximo: `7680` (limitado por Parsec VDD)
- **Recomendado**: `1920` (Full HD)

**Perfiles Comunes**:
- 1280 (HD)
- 1920 (Full HD)
- 2560 (2K)
- 3840 (4K)

**Ejemplo**:
```json
{
  "Width": 1920
}
```

---

#### `Height` (entero)

**Descripción**: Alto de la pantalla virtual en píxeles.

**Valores**:
- Mínimo: `480`
- Máximo: `4320` (limitado por Parsec VDD)
- **Recomendado**: `1080` (Full HD)

**Perfiles Comunes**:
- 720 (HD)
- 1080 (Full HD)
- 1440 (2K)
- 2160 (4K)

**Ejemplo**:
```json
{
  "Height": 1080
}
```

---

#### `HttpPort` (entero)

**Descripción**: Puerto para servidor HTTP/HTTPS.

**Comportamiento**:
- **HTTP**: `HttpPort` (ejemplo: 5000)
- **HTTPS**: `HttpPort + 1` (ejemplo: 5001)

**Valores**:
- Mínimo: `1024`
- Máximo: `65535`
- **Default Screen1**: `5000`
- **Default Screen2**: `6000`

**Advertencias**:
- ⚠️ No usar puertos reservados (< 1024)
- ⚠️ Evitar puertos comunes en uso (80, 443, 3000, 8080, etc.)
- ⚠️ `Screen1` y `Screen2` **deben usar puertos diferentes**

**Validación Automática**:
- Si `Screen1.HttpPort == Screen2.HttpPort`, se ajusta automáticamente:
  ```csharp
  Screen2.HttpPort = Screen1.HttpPort + 10;
  ```

**Ejemplo**:
```json
{
  "Screen1": {
    "HttpPort": 5000  // HTTP: 5000, HTTPS: 5001
  },
  "Screen2": {
    "HttpPort": 6000  // HTTP: 6000, HTTPS: 6001
  }
}
```

---

#### `TransmissionMode` (cadena)

**Descripción**: Método de transmisión de video.

**Valores Permitidos**:
- `"WebImage"`: JPEG polling (compatible con todos los navegadores)
- `"RTC"`: WebRTC streaming (baja latencia, requiere HTTPS)

**Default**: `"RTC"`

**Comparación**:

| Característica | `"WebImage"` | `"RTC"` |
|----------------|--------------|---------|
| Latencia | ~100-200ms | ~30-50ms |
| Navegadores | Todos (IE, antiguos) | Modernos (Chrome, Edge, Firefox) |
| Requiere HTTPS | ❌ No | ✅ Sí |
| CPU Usage | Bajo | Medio |

**Ejemplo**:
```json
{
  "TransmissionMode": "RTC"
}
```

---

#### `CaptureIntervalMs` (entero)

**Descripción**: Milisegundos entre capturas de pantalla.

**Valores**:
- Mínimo: `16` (~60 FPS)
- Máximo: `500` (~2 FPS)
- **Default**: `50` (~20 FPS)

**FPS Equivalente**:
```
FPS = 1000 / CaptureIntervalMs
```

**Ejemplos**:

| CaptureIntervalMs | FPS | Uso Recomendado |
|-------------------|-----|-----------------|
| 16 | 60 | Gaming, máxima fluidez |
| 33 | 30 | Presentaciones, videos |
| 50 | 20 | **Default** - uso general |
| 100 | 10 | Dashboards, monitoreo |
| 200 | 5 | Contenido estático |

**Consideraciones**:
- ⬇️ Menor intervalo = ⬆️ FPS = ⬆️ CPU usage
- ⬆️ Mayor intervalo = ⬇️ FPS = ⬇️ CPU usage

**Ejemplo**:
```json
{
  "CaptureIntervalMs": 50  // 20 FPS
}
```

---

#### `JpegQuality` (entero)

**Descripción**: Calidad de compresión JPEG (1-100).

**Valores**:
- Mínimo: `1` (peor calidad, menor tamaño)
- Máximo: `100` (mejor calidad, mayor tamaño)
- **Default**: `75`

**Guía de Calidad**:

| JpegQuality | Tamaño Frame (1080p) | Calidad Visual | Uso |
|-------------|----------------------|----------------|-----|
| 95-100 | ~500-800 KB | Excelente | Fotografía, diseño |
| 85-94 | ~200-400 KB | Muy buena | Uso general de alta calidad |
| 75-84 | ~100-200 KB | Buena | **Recomendado** - balance óptimo |
| 60-74 | ~50-100 KB | Aceptable | Bajo ancho de banda |
| 1-59 | ~20-50 KB | Pobre | Solo testing |

**Trade-off**:
- ⬆️ Mayor calidad = ⬆️ Tamaño = ⬆️ Ancho de banda = ⬆️ Tiempo codificación
- ⬇️ Menor calidad = ⬇️ Tamaño = ⬇️ Ancho de banda = ⬇️ Tiempo codificación

**Ejemplo**:
```json
{
  "JpegQuality": 75  // Balance óptimo
}
```

---

#### `Placement` (cadena)

**Descripción**: Posición de la pantalla virtual relativa al monitor primario.

**Valores Permitidos**:
- `"Right"`: A la derecha del monitor primario
- `"Left"`: A la izquierda del monitor primario
- `"Above"`: Encima del monitor primario
- `"Below"`: Debajo del monitor primario

**Default**: `"Right"`

**Visualización**:

```
┌─────────────┐
│   "Above"   │
└─────────────┘
┌──────┬─────────────┬──────┐
│"Left"│   Primary   │"Right│
└──────┴─────────────┴──────┘
       ┌─────────────┐
       │   "Below"   │
       └─────────────┘
```

**Ejemplo**:
```json
{
  "Placement": "Right"
}
```

**Uso Común**:
- `"Right"`: Monitor extra a la derecha (más común)
- `"Left"`: Monitor extra a la izquierda
- `"Above"`: Dashboard/info panel superior
- `"Below"`: Consola/logs inferior

---

#### `OffsetX` (entero)

**Descripción**: Desplazamiento horizontal adicional en píxeles (relativo a posición calculada por `Placement`).

**Valores**:
- Positivo: Desplaza a la derecha
- Negativo: Desplaza a la izquierda
- `0`: Sin desplazamiento adicional
- **Default**: `0`

**Ejemplo**:

```json
{
  "Placement": "Right",
  "OffsetX": 100  // 100px adicionales a la derecha
}
```

**Caso de Uso**:
- Ajustar posición exacta entre monitores
- Compensar bezels/marcos de pantalla

---

#### `OffsetY` (entero)

**Descripción**: Desplazamiento vertical adicional en píxeles (relativo a posición calculada por `Placement`).

**Valores**:
- Positivo: Desplaza hacia abajo
- Negativo: Desplaza hacia arriba
- `0`: Sin desplazamiento adicional
- **Default**: `0`

**Ejemplo**:

```json
{
  "Placement": "Above",
  "OffsetY": -50  // 50px adicionales hacia arriba
}
```

**Caso de Uso**:
- Alinear verticalmente con otros monitores
- Compensar altura de taskbar

---

#### `Rotation` (entero)

**Descripción**: Rotación de la imagen capturada en grados.

**Valores Permitidos**:
- `0`: Sin rotación (default)
- `90`: Rotación horaria (landscape → portrait)
- `180`: Rotación 180° (invertido)
- `270`: Rotación antihoraria

**Default**: `0`

**Ejemplo**:

```json
{
  "Rotation": 90  // Rotar 90° (ideal para tablet vertical)
}
```

**Casos de Uso**:
- Tablets/teléfonos en orientación vertical
- Monitores montados verticalmente
- Corrección de displays rotados físicamente

---

## Ejemplos de Configuración

### Ejemplo 1: Configuración Básica (Single Screen)

**Escenario**: Monitor Full HD extra, WebRTC, 30 FPS.

```json
{
  "Screen1": {
    "Enabled": true,
    "Width": 1920,
    "Height": 1080,
    "HttpPort": 5000,
    "TransmissionMode": "RTC",
    "CaptureIntervalMs": 33,
    "JpegQuality": 75,
    "Placement": "Right",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  },
  "Screen2": {
    "Enabled": false,
    "Width": 1280,
    "Height": 720,
    "HttpPort": 6000,
    "TransmissionMode": "WebImage",
    "CaptureIntervalMs": 100,
    "JpegQuality": 70,
    "Placement": "Left",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  }
}
```

**Acceso**: `https://localhost:5001`

---

### Ejemplo 2: Dual Screen (Diferentes Modos)

**Escenario**: 
- Screen1: 1080p WebRTC para gaming (30 FPS, alta calidad)
- Screen2: 720p Web Image para dashboard (10 FPS, calidad media)

```json
{
  "Screen1": {
    "Enabled": true,
    "Width": 1920,
    "Height": 1080,
    "HttpPort": 5000,
    "TransmissionMode": "RTC",
    "CaptureIntervalMs": 33,
    "JpegQuality": 85,
    "Placement": "Right",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  },
  "Screen2": {
    "Enabled": true,
    "Width": 1280,
    "Height": 720,
    "HttpPort": 6000,
    "TransmissionMode": "WebImage",
    "CaptureIntervalMs": 100,
    "JpegQuality": 70,
    "Placement": "Above",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  }
}
```

**Acceso**:
- Screen1: `https://localhost:5001` (gaming, baja latencia)
- Screen2: `http://localhost:6000` (dashboard, HTTP simple)

---

### Ejemplo 3: Tablet Vertical (Rotación 90°)

**Escenario**: Usar tablet en orientación vertical (portrait).

```json
{
  "Screen1": {
    "Enabled": true,
    "Width": 1080,
    "Height": 1920,
    "HttpPort": 5000,
    "TransmissionMode": "RTC",
    "CaptureIntervalMs": 50,
    "JpegQuality": 75,
    "Placement": "Right",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 90
  },
  "Screen2": {
    "Enabled": false,
    "Width": 1280,
    "Height": 720,
    "HttpPort": 6000,
    "TransmissionMode": "WebImage",
    "CaptureIntervalMs": 100,
    "JpegQuality": 70,
    "Placement": "Left",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  }
}
```

**Nota**: Resolución es 1080×1920 (portrait), rotación 90° compensa.

---

### Ejemplo 4: Bajo Ancho de Banda (Optimizado)

**Escenario**: Red lenta, priorizar bajo consumo de ancho de banda.

```json
{
  "Screen1": {
    "Enabled": true,
    "Width": 1280,
    "Height": 720,
    "HttpPort": 5000,
    "TransmissionMode": "WebImage",
    "CaptureIntervalMs": 200,
    "JpegQuality": 60,
    "Placement": "Right",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  },
  "Screen2": {
    "Enabled": false,
    "Width": 1920,
    "Height": 1080,
    "HttpPort": 6000,
    "TransmissionMode": "RTC",
    "CaptureIntervalMs": 50,
    "JpegQuality": 75,
    "Placement": "Left",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  }
}
```

**Optimizaciones**:
- ⬇️ Resolución: 720p (vs. 1080p)
- ⬇️ FPS: 5 (vs. 20-30)
- ⬇️ Calidad JPEG: 60 (vs. 75-85)
- Modo: Web Image (menor overhead que WebRTC)

**Estimación Ancho de Banda**: ~1-2 Mbps (vs. 10-20 Mbps con config alta)

---

### Ejemplo 5: Gaming/High Performance

**Escenario**: Gaming o aplicación interactiva, máxima calidad y fluidez.

```json
{
  "Screen1": {
    "Enabled": true,
    "Width": 1920,
    "Height": 1080,
    "HttpPort": 5000,
    "TransmissionMode": "RTC",
    "CaptureIntervalMs": 16,
    "JpegQuality": 95,
    "Placement": "Right",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  },
  "Screen2": {
    "Enabled": false,
    "Width": 1280,
    "Height": 720,
    "HttpPort": 6000,
    "TransmissionMode": "WebImage",
    "CaptureIntervalMs": 100,
    "JpegQuality": 70,
    "Placement": "Left",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  }
}
```

**Configuración**:
- ⬆️ FPS: 60 (16ms intervalo)
- ⬆️ Calidad: 95 (máxima calidad visual)
- Modo: WebRTC (latencia ~30ms)

**Requisitos**:
- CPU potente (i5/Ryzen 5 o superior)
- Red local rápida (Gigabit Ethernet o WiFi 6)

---

## Validación y Valores por Defecto

### Validación Automática

La aplicación valida configuración al cargar (`EnsureValid()`):

```csharp
public void EnsureValid()
{
    // Normalizar resolución
    if (Width < 640) Width = 640;
    if (Height < 480) Height = 480;
    if (Width > 7680) Width = 7680;
    if (Height > 4320) Height = 4320;

    // Normalizar puerto
    if (HttpPort < 1024) HttpPort = 5000;
    if (HttpPort > 65535) HttpPort = 5000;

    // Normalizar intervalo
    if (CaptureIntervalMs < 16) CaptureIntervalMs = 16;
    if (CaptureIntervalMs > 500) CaptureIntervalMs = 500;

    // Normalizar calidad JPEG
    if (JpegQuality < 1) JpegQuality = 1;
    if (JpegQuality > 100) JpegQuality = 100;

    // Detectar conflicto de puertos
    if (Screen1.HttpPort == Screen2.HttpPort && Screen2.Enabled)
    {
        Screen2.HttpPort = Screen1.HttpPort + 10;
    }
}
```

### Configuración por Defecto

Si `settings.json` no existe o está corrupto:

```json
{
  "Screen1": {
    "Enabled": true,
    "Width": 1920,
    "Height": 1080,
    "HttpPort": 5000,
    "TransmissionMode": "RTC",
    "CaptureIntervalMs": 50,
    "JpegQuality": 75,
    "Placement": "Right",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  },
  "Screen2": {
    "Enabled": false,
    "Width": 1280,
    "Height": 720,
    "HttpPort": 6000,
    "TransmissionMode": "WebImage",
    "CaptureIntervalMs": 100,
    "JpegQuality": 70,
    "Placement": "Left",
    "OffsetX": 0,
    "OffsetY": 0,
    "Rotation": 0
  }
}
```

---

## Edición Manual

### Paso a Paso

1. **Cerrar la Aplicación**:
   ```
   Click derecho en tray icon → Exit
   ```

2. **Abrir Archivo de Configuración**:
   ```
   C:\Users\<Usuario>\.virtualwebdisplay\settings.json
   ```

   Usar cualquier editor de texto (Notepad++, VS Code, etc.)

3. **Modificar JSON**:
   - Asegurar sintaxis JSON válida
   - Usar comillas dobles para cadenas
   - No usar comas finales

4. **Guardar y Cerrar**

5. **Reiniciar Aplicación**:
   - La configuración se valida automáticamente
   - Si hay errores, se usan valores por defecto

### Validación de JSON

Usar herramienta online para verificar sintaxis:
- https://jsonlint.com/
- https://jsonformatter.org/

### Ejemplo de Error Común

❌ **Incorrecto** (coma final):
```json
{
  "Screen1": {
    "Width": 1920,
    "Height": 1080,  ← Coma final invalida JSON
  }
}
```

✅ **Correcto**:
```json
{
  "Screen1": {
    "Width": 1920,
    "Height": 1080
  }
}
```

---

## Exportar/Importar Configuración

### Exportar

1. **Método 1: Descargar desde Web**:
   ```
   https://localhost:5001/config
   ```
   Guarda: `settings.json`

2. **Método 2: Copiar Manualmente**:
   ```powershell
   copy C:\Users\<Usuario>\.virtualwebdisplay\settings.json C:\Backup\
   ```

### Importar

1. **Sobrescribir Archivo**:
   ```powershell
   copy C:\Backup\settings.json C:\Users\<Usuario>\.virtualwebdisplay\
   ```

2. **Reiniciar Aplicación**

---

## Troubleshooting de Configuración

### Problema: Configuración No Se Aplica

**Solución**:
- Verificar que archivo esté en ubicación correcta
- Verificar sintaxis JSON válida
- Reiniciar aplicación (no basta con "Apply" en UI si se editó manualmente)

### Problema: Puerto en Uso

**Error**: "Address already in use: 5000"

**Solución**:
- Cambiar `HttpPort` a puerto diferente (ej: 7000)
- O detener aplicación usando ese puerto:
  ```powershell
  netstat -ano | findstr :5000
  taskkill /PID <PID> /F
  ```

### Problema: Pantalla Virtual No Aparece

**Solución**:
- Verificar `"Enabled": true`
- Verificar Parsec VDD instalado
- Revisar resolución dentro de límites (640-7680 x 480-4320)

---

Para más ayuda, ver **docs/TROUBLESHOOTING.md**.
