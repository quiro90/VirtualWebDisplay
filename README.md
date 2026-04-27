# 🖥️ VirtualWebDisplay

**Transmite pantallas virtuales de Windows a través de tu navegador web con latencia ultra-baja usando WebRTC o JPEG polling.**

[![.NET Version](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## 🌟 Características

- ✨ **Pantallas Virtuales**: Crea hasta 2 monitores virtuales usando Parsec VDD driver
- 🚀 **Transmisión de Baja Latencia**: WebRTC con DataChannel optimizado (< 50ms)
- 🎨 **Doble Modo de Streaming**:
  - **Web Image**: JPEG polling (compatible con cualquier navegador)
  - **WebRTC**: Streaming en tiempo real (navegadores modernos)
- ⚙️ **Altamente Configurable**:
  - Resolución personalizada (720p a 5K)
  - Intervalo de captura (16ms a 500ms)
  - Calidad JPEG ajustable (1-100)
  - Posicionamiento de pantallas (derecha, izquierda, arriba, abajo)
  - Rotación de imagen (0°, 90°, 180°, 270°)
- 🌐 **Acceso Remoto**: Accede desde cualquier dispositivo en tu red local
- 🔒 **HTTPS Automático**: Certificado SSL autofirmado generado automáticamente
- 🎯 **Sin Configuración de Red**: Detección automática de IP local
- 💾 **Persistencia de Configuración**: Settings guardados en `%USERPROFILE%\.virtualwebdisplay\virtualscreen.user.json`
- 🔐 **Seguridad por Pantalla**: Clave alfanumérica dinámica de 6 caracteres (opcional por Screen1/Screen2), con límite de intentos

---

## 📸 Screenshots

### Interfaz de Configuración

```
┌─────────────────────────────────────────┐
│  VirtualWebDisplay Configuration       │
├─────────────────────────────────────────┤
│  Screen 1  │  Screen 2                  │
│ ─────────────────────────────────────── │
│  ☑ Enable Screen 1                      │
│  Resolution: 1920x1080 ▼                │
│  Transmission Mode: WebRTC ▼            │
│  HTTP Port: 5000                        │
│  Capture Interval: 50 ms                │
│  JPEG Quality: 75                       │
│  Position: Right of primary ▼           │
│  Rotation: 0° ▼                         │
│                                         │
│  [Apply]  [Cancel]                      │
└─────────────────────────────────────────┘
```

### Cliente Web (Modo WebRTC)

```
┌────────────────────────────────────────────┐
│  VirtualWebDisplay - Screen 1              │
├────────────────────────────────────────────┤
│                                            │
│   🎥 [Pantalla virtual en tiempo real]    │
│                                            │
│   Status: Connected via WebRTC            │
│   Latency: ~30ms                          │
│   Resolution: 1920x1080                   │
│                                            │
└────────────────────────────────────────────┘
```

---

## 🚀 Inicio Rápido

### Requisitos Previos

1. **Windows 10/11** (64-bit)
2. **.NET 10 SDK** ([descargar](https://dotnet.microsoft.com/download/dotnet/10.0))
3. **Parsec Virtual Display Driver** ([descargar](https://github.com/nomi-san/parsec-vdd/releases))

### Instalación

#### Opción 1: Ejecutable Precompilado (Recomendado)

1. Descargar la última release desde [Releases](https://github.com/quiro90/VirtualWebDisplay/releases)
2. Extraer el archivo ZIP
3. Ejecutar `VirtualWebDisplay_Parsec.exe`

#### Opción 2: Compilar desde Código Fuente

```powershell
# Clonar repositorio
git clone https://github.com/quiro90/VirtualWebDisplay.git
cd VirtualWebDisplay

# Compilar
dotnet build VirtualWebDisplay_Parsec.csproj --configuration Release

# Ejecutar
.\bin\Release\net10.0-windows\VirtualWebDisplay_Parsec.exe
```

### Instalación del Driver Parsec VDD

Si es la primera vez que ejecutas la aplicación:

1. Descargar [Parsec VDD](https://github.com/nomi-san/parsec-vdd/releases/latest)
2. Extraer y ejecutar `installdriver.bat` **como Administrador**
3. Reiniciar la aplicación

O usar el diálogo de instalación integrado:

```
┌──────────────────────────────────────┐
│  Parsec VDD Driver Not Found         │
├──────────────────────────────────────┤
│  This application requires the       │
│  Parsec Virtual Display Driver.      │
│                                      │
│  [Download Driver]  [Cancel]         │
└──────────────────────────────────────┘
```

---

## 📖 Uso

### 1. Iniciar la Aplicación

Ejecutar `VirtualWebDisplay_Parsec.exe`. Aparecerá un icono en la bandeja del sistema:

```
🖥️ VirtualWebDisplay
├── Configuration...
└── Exit
```

### 2. Configurar Pantalla Virtual

Click derecho en el icono → **Configuration**

**Opciones Principales**:

| Opción | Descripción | Valores |
|--------|-------------|---------|
| **Resolution** | Tamaño de la pantalla virtual | 1280x720, 1920x1080, 2560x1440, 3840x2160 |
| **Transmission Mode** | Método de streaming | Web Image (JPEG), RTC (WebRTC) |
| **HTTP Port** | Puerto del servidor web | 5000 (default), cualquier puerto disponible |
| **Capture Interval** | Milisegundos entre capturas | 16-500ms (default: 50ms = 20 FPS) |
| **JPEG Quality** | Calidad de compresión | 1-100 (default: 75, mayor = mejor calidad) |
| **Position** | Posición relativa al monitor primario | Right, Left, Above, Below |
| **Rotation** | Rotación de imagen | 0°, 90°, 180°, 270° |
| **Screen Security** | Requiere clave de acceso al abrir el host | Activado/Desactivado por pantalla |

Click **Apply** para crear/actualizar la pantalla virtual.

### 3. Acceder desde Navegador

**Dispositivo Local**:
```
https://localhost:5001
```

**Desde otro dispositivo en la red**:
```
https://192.168.1.XXX:5001
```
*(La IP se muestra en la app y en systray)*

Si la seguridad de pantalla está activada para esa pantalla, primero aparecerá un formulario de acceso y solo mostrará contenido al ingresar la clave válida.

### 4. Instalar Certificado SSL (Primera Vez)

Para evitar advertencias de seguridad:

1. Navegar a `https://localhost:5001/cert`
2. Guardar `localhost.cer`
3. Doble click → **Instalar Certificado**
4. **Store Location**: Local Machine
5. **Certificate Store**: Trusted Root Certification Authorities
6. Reiniciar navegador

---

## 🎮 Casos de Uso

### 1. Monitor Extra para Laptop

Usa tu tablet/teléfono como segundo monitor inalámbrico:

```
Configuración Recomendada:
- Resolution: 1920x1080
- Transmission Mode: RTC
- Capture Interval: 50ms
- Position: Right
```

### 2. Streaming de Aplicación Específica

Arrastra una aplicación a la pantalla virtual para transmitirla:

```
1. Crear pantalla virtual (Ej: 1280x720)
2. Mover ventana de aplicación a pantalla virtual (Win+Shift+→)
3. Acceder desde navegador
```

### 3. Dashboard/Monitoring Remoto

Muestra métricas/dashboard en pantalla virtual, accede desde otro PC:

```
Configuración Recomendada:
- Resolution: 1920x1080
- Transmission Mode: Web Image
- Capture Interval: 200ms (baja CPU, suficiente para dashboards estáticos)
- JPEG Quality: 85
```

### 4. Presentaciones Remotas

Transmite presentación a múltiples dispositivos en red local:

```
Configuración Recomendada:
- Resolution: 1920x1080
- Transmission Mode: RTC (baja latencia)
- Capture Interval: 33ms (30 FPS para animaciones suaves)
```

---

## ⚙️ Configuración Avanzada

### Archivo de Configuración

Ubicación: `C:\Users\<Usuario>\.virtualwebdisplay\virtualscreen.user.json`

**Ejemplo**:

```json
{
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

**Edición Manual**:

1. Cerrar la aplicación
2. Editar `virtualscreen.user.json`
3. Reiniciar la aplicación (validará y aplicará configuración)

Ver **docs/CONFIGURATION.md** para detalles de cada campo.

---

## 🔧 Endpoints HTTP

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/` | GET | Página principal (HTML con cliente de streaming) |
| `/cap` | GET | Imagen JPEG actual (para modo Web Image) |
| `/mjpeg` | GET | Stream MJPEG continuo (solo modo Web Image) |
| `/webrtc/offer` | POST | Negociación WebRTC (SDP offer → answer) |
| `/auth/login` | POST | Login por clave cuando la seguridad de pantalla está activa |
| `/cert` | GET | Descargar certificado SSL autofirmado |
| `/config` | GET | Descargar configuración JSON actual (requiere auth si seguridad activa) |

### Ejemplo: Obtener Frame Actual

```powershell
# PowerShell
Invoke-WebRequest -Uri "https://localhost:5001/cap" -OutFile "screenshot.jpg"
```

```bash
# cURL
curl -k https://localhost:5001/cap -o screenshot.jpg
```

### Ejemplo: Integración con Python

```python
import requests
from PIL import Image
from io import BytesIO

# Obtener frame JPEG
response = requests.get("https://localhost:5001/cap", verify=False)
img = Image.open(BytesIO(response.content))
img.show()
```

---

## 📊 Comparación de Modos de Transmisión

| Característica | Web Image (JPEG) | WebRTC |
|----------------|------------------|--------|
| **Latencia** | ~100-200ms | ~30-50ms |
| **Compatibilidad** | Todos los navegadores | Navegadores modernos (Chrome, Edge, Firefox) |
| **Calidad** | JPEG comprimido | JPEG comprimido (mismo codec) |
| **CPU Usage** | Bajo | Medio (por gestión de peers) |
| **Ancho de Banda** | Medio | Medio (similar, pero más eficiente en cambios rápidos) |
| **Múltiples Clientes** | ✅ Sí (polling independiente) | ✅ Sí (peers concurrentes) |
| **Requiere HTTPS** | ❌ No (funciona HTTP) | ✅ Sí (requisito de navegadores) |

**Recomendación**:
- **WebRTC**: Para gaming, aplicaciones interactivas, presentaciones
- **Web Image**: Para dashboards, monitoreo, dispositivos antiguos

---

## 🛠️ Desarrollo

Ver documentación detallada:

- **[DEVELOPMENT.md](DEVELOPMENT.md)**: Guía de desarrollo completa
- **[ARCHITECTURE.md](ARCHITECTURE.md)**: Arquitectura del sistema y diagramas
- **[AGENT.md](AGENT.md)**: Contexto técnico para IA/asistentes

### Stack Tecnológico

- **.NET 10** (C# 13)
- **ASP.NET Core / Kestrel** (servidor web)
- **WinForms** (UI)
- **SIPSorcery** (WebRTC)
- **Parsec VDD** (driver de pantalla virtual)
- **System.Drawing** (captura de pantalla)

### Estructura del Proyecto

```
VirtualWebDisplay/
├── UI/                     # Interfaz gráfica (tray, formularios, templates HTML)
├── Configuration/          # Gestión de configuración y modelos
├── Parsec/                 # Interfaz con driver Parsec VDD
├── Streaming/              # Captura de pantalla y transmisión
├── Infrastructure/         # Servicios transversales (red, certificados, runtime)
├── Program.cs              # Punto de entrada
└── docs/                   # Documentación adicional
```

### Compilar

```powershell
dotnet build VirtualWebDisplay_Parsec.csproj --configuration Release
```

### Testing

```powershell
# Ejecutar tests (cuando estén disponibles)
dotnet test

# Testing manual
.\bin\Release\net10.0-windows\VirtualWebDisplay_Parsec.exe
```

### Contribuir

1. Fork del repositorio
2. Crear branch de feature (`git checkout -b feature/nueva-funcionalidad`)
3. Commit cambios (`git commit -am 'Agregar nueva funcionalidad'`)
4. Push al branch (`git push origin feature/nueva-funcionalidad`)
5. Crear Pull Request

Ver **DEVELOPMENT.md** para convenciones de código y guía de contribución.

---

## 🐛 Troubleshooting

### Problema: "Parsec VDD Driver Not Found"

**Solución**:
1. Descargar [Parsec VDD](https://github.com/nomi-san/parsec-vdd/releases/latest)
2. Ejecutar `installdriver.bat` como Administrador
3. Reiniciar aplicación

---

### Problema: "Another instance is already running"

**Solución**:
```powershell
# Terminar proceso previo
taskkill /F /IM VirtualWebDisplay_Parsec.exe
```

---

### Problema: "NET::ERR_CERT_AUTHORITY_INVALID" en Chrome

**Solución**:
1. Descargar certificado: `https://localhost:5001/cert`
2. Instalar en "Trusted Root Certification Authorities"
3. Reiniciar Chrome

---

### Problema: WebRTC no conecta

**Solución**:
- Verificar que navegador accede vía HTTPS (requerido para WebRTC)
- Instalar certificado SSL (ver arriba)
- Verificar firewall no bloquea puerto HTTPS (5001)

---

### Más Ayuda

Ver **[docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)** para guía completa de problemas comunes.

---

## 📄 Licencia

Este proyecto está licenciado bajo la **MIT License** - ver archivo [LICENSE](LICENSE) para detalles.

---

## 🙏 Agradecimientos

- [Parsec VDD](https://github.com/nomi-san/parsec-vdd) - Excelente driver de pantalla virtual
- [SIPSorcery](https://github.com/sipsorcery-org/sipsorcery) - Implementación WebRTC para .NET
- [.NET Team](https://github.com/dotnet) - Por la plataforma .NET

---

## 📧 Contacto

- **GitHub Issues**: [Reportar un problema](https://github.com/quiro90/VirtualWebDisplay/issues)
- **Discussions**: [Hacer una pregunta](https://github.com/quiro90/VirtualWebDisplay/discussions)

---

## 🚀 Roadmap

- [ ] Soporte para H.264 hardware encoding (menor latencia, menor CPU)
- [ ] Streaming de audio
- [ ] Control remoto (mouse/teclado desde navegador)
- [ ] Soporte para 3+ pantallas virtuales
- [ ] Cliente desktop multiplataforma (Linux, macOS)
- [ ] Modo "espejo" (duplicar monitor real)

---

**⭐ Si este proyecto te resulta útil, considera darle una estrella en GitHub!**
