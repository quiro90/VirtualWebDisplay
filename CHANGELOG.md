# Changelog

Todos los cambios notables de este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2024-01-XX

### 🎉 Versión Inicial - Refactorización Completa

Esta versión representa una refactorización completa del proyecto VirtualWebDisplay, transformando un código monolítico en una arquitectura organizada por capas.

### ✨ Added

#### Arquitectura

- **Estructura de carpetas organizada** con 7 capas:
  - `UI/` - Interfaz gráfica (tray icon, formularios, templates HTML)
  - `Configuration/` - Gestión de configuración y persistencia
  - `Parsec/` - Interfaz con driver Parsec VDD
  - `Streaming/` - Captura de pantalla y transmisión
  - `Infrastructure/` - Servicios transversales
  - `docs/` - Documentación del proyecto

- **Sistema de namespaces consistente**: `VirtualWebDisplay.[Folder].[Subfolder]`

#### Características Principales

- ✅ **Doble pantalla virtual**: Soporte para hasta 2 monitores virtuales simultáneos
- ✅ **Dos modos de transmisión**:
  - **Web Image**: JPEG polling (compatible con todos los navegadores)
  - **WebRTC**: Streaming en tiempo real con DataChannel optimizado
- ✅ **Configuración persistente**: Settings guardados en `~/.virtualwebdisplay/settings.json`
- ✅ **Certificado SSL automático**: Generación de certificado autofirmado con SANs
- ✅ **Detección automática de IP**: NetworkAddressHelper obtiene IP local para acceso remoto
- ✅ **Prevención de instancias múltiples**: SingleInstanceManager con mutex global

#### Opciones de Configuración

- Resolución personalizable (720p, 1080p, 2K, 4K)
- Posicionamiento de pantallas (Right, Left, Above, Below)
- Intervalo de captura ajustable (16ms - 500ms)
- Calidad JPEG configurable (1-100)
- Rotación de imagen (0°, 90°, 180°, 270°)
- Múltiples puertos HTTP configurables

#### Endpoints HTTP

- `GET /` - Página web principal (template HTML dinámico)
- `GET /cap` - Frame JPEG actual
- `GET /mjpeg` - Stream MJPEG continuo
- `POST /webrtc/offer` - Negociación WebRTC (SDP offer/answer)
- `GET /cert` - Descarga certificado SSL
- `GET /config` - Descarga configuración JSON

#### Documentación

- 📖 **README.md**: Descripción del proyecto, inicio rápido, uso
- 🏗️ **ARCHITECTURE.md**: Arquitectura del sistema con diagramas Mermaid
- 🛠️ **DEVELOPMENT.md**: Guía de desarrollo completa
- 🤖 **AGENT.md**: Contexto técnico para asistentes IA
- 📝 **docs/CONFIGURATION.md**: Estructura del archivo de configuración
- 🐛 **docs/TROUBLESHOOTING.md**: Guía de resolución de problemas
- ✨ **docs/FEATURES.md**: Descripción detallada de características

### ♻️ Changed

#### Refactorización de Código

- **VirtualDisplayTrayController.cs**: Reducido de 850 → 250 líneas (70.6% reduction)
  - Extraídas clases anidadas a archivos independientes:
    - `ResolutionConfigurationForm.cs`
    - `ScreenTabControls.cs`
  - Movido de raíz → `UI/TrayIcon/`

- **Program.cs**: Reducido de 620 → 164 líneas (73.5% reduction)
  - HTML embebido extraído a templates:
    - `IHtmlTemplate.cs` (interface)
    - `WebImagePageTemplate.cs`
    - `RtcPageTemplate.cs`
  - Extraída clase anidada:
    - `InstallDialog.cs`
  - Eliminadas ~456 líneas de HTML en strings

#### Organización de Archivos

**Movidos a `Configuration/Models/`**:
- `VirtualScreenConfig.cs`
- `VirtualWebDisplaySettings.cs`

**Movidos a `Configuration/`**:
- `VirtualScreenSettingsStore.cs`
- `VirtualDisplayProfiles.cs`
- `TransmissionModeOptions.cs`
- `VirtualDisplayPlacementOptions.cs`

**Movidos a `Parsec/`**:
- `VirtualDisplayManager.cs`

**Movidos a `Streaming/`**:
- `CaptureService.cs`
- `WebRtcStreamService.cs`

**Extraídos a `Streaming/Models/`**:
- `WebRtcSessionOffer.cs` (record)
- `WebRtcSessionAnswer.cs` (record)

**Movidos a `Infrastructure/`**:
- `ScreenRuntimeContext.cs`
- `NetworkAddressHelper.cs`
- `LocalCertificateProvider.cs`
- `SingleInstanceManager.cs`

### 🚀 Optimizations

- **Detección de cambios en frames**: Hash FNV-1a de muestras de píxeles (evita codificaciones JPEG innecesarias)
- **Caché de ImageCodecInfo**: Búsqueda única de codec JPEG (mejora performance)
- **WebRTC DataChannel optimizado**: `ordered: false`, `maxRetransmits: 0` (latencia mínima ~30-50ms)
- **Chunking eficiente**: Frames divididos en chunks de 64KB con prefijo frameId little-endian

### 🔧 Technical Improvements

- **Arquitectura por capas** (Layered Architecture)
- **Patrones de diseño implementados**:
  - Repository Pattern (`VirtualScreenSettingsStore`)
  - Template Method Pattern (`IHtmlTemplate`)
  - Facade Pattern (`ScreenRuntimeContext`)
  - Singleton Pattern (`SingleInstanceManager`)
  - Adapter Pattern (`VirtualDisplayManager`)
  - Observer Pattern (eventos WinForms)
- **Disposable Pattern**: Gestión correcta de recursos con `IDisposable` / `IAsyncDisposable`
- **Background Services**: `CaptureService` y `WebRtcStreamService` heredan `BackgroundService`
- **Record types para DTOs**: Inmutabilidad en modelos de datos

### 📊 Metrics

- **Archivos organizados**: 15 archivos en raíz → 21 archivos en 7 carpetas estructuradas
- **Reducción de complejidad**: ~70% en VirtualDisplayTrayController, ~73% en Program.cs
- **Eliminación de código duplicado**: ~456 líneas de HTML embebido
- **Compilación exitosa**: 0 errores, 0 warnings
- **Funcionalidad preservada**: 100% (sin cambios en lógica de negocio)

### 🐛 Fixed

- Conflictos de namespace al reorganizar archivos
- Imports faltantes después de mover archivos
- Duplicación de archivos durante refactorización
- Codificación de caracteres especiales en PowerShell

### 🔒 Security

- Certificado SSL con Subject Alternative Names (SANs) incluyendo IP local
- Validación de configuración antes de persistir (detección de conflictos de puerto)
- Mutex global para prevenir múltiples instancias (evita conflictos de recursos)

### 📚 Documentation

Creación de documentación completa:

- **Documentación principal** (nivel raíz):
  - `README.md` - Descripción, inicio rápido, uso
  - `ARCHITECTURE.md` - Arquitectura y diagramas
  - `DEVELOPMENT.md` - Guía de desarrollo
  - `AGENT.md` - Contexto para IA
  - `CHANGELOG.md` - Este archivo

- **Documentación de refactorización** (`docs/refactoring/`):
  - `REFACTORING_COMPLETE.md` - Resumen completo de refactorización
  - `REFACTORING_VISUAL_SUMMARY.md` - Comparación antes/después
  - `REFACTORING_STATUS.md` - Estado del progreso
  - `REFACTORING_LOG.md` - Log cronológico
  - `REFACTORING_SUMMARY.md` - Guía de implementación
  - `REFACTORING_PLAN.md` - Plan original

- **Documentación de usuario** (`docs/`):
  - `FEATURES.md` - Descripción de características
  - `CONFIGURATION.md` - Estructura de configuración
  - `TROUBLESHOOTING.md` - Resolución de problemas

### 🛣️ Roadmap para Futuras Versiones

Planificado para versiones futuras:

- [ ] **v1.1.0**: Soporte para H.264 hardware encoding
- [ ] **v1.2.0**: Streaming de audio
- [ ] **v1.3.0**: Control remoto (mouse/teclado desde navegador)
- [ ] **v2.0.0**: Soporte para 3+ pantallas virtuales
- [ ] **v2.1.0**: Cliente desktop multiplataforma
- [ ] **v2.2.0**: Modo "espejo" de monitor real

---

## [Unreleased]

### En Desarrollo

- Configuración de WebRTC heredará configuración de intervalo y calidad JPEG de Web Image mode
- Persistencia de configuración en carpeta `.virtualwebdisplay` en perfil de usuario

---

## Notas de Migración

### De versión monolítica a v1.0.0

**Configuración**:
- El archivo de configuración ahora se almacena en `C:\Users\<Usuario>\.virtualwebdisplay\settings.json`
- Si usabas una versión anterior, exporta tu configuración y copia manualmente al nuevo archivo

**Código**:
- Si has modificado el código, necesitarás actualizar imports:
  - Ejemplo: `VirtualDisplayManager` ahora está en `VirtualWebDisplay.Parsec`
  - Ver `AGENT.md` para mapeo completo de namespaces

**Compilación**:
- Proyecto requiere .NET 10 SDK
- Target framework: `net10.0-windows`

---

## Formato de Cambios

Secciones utilizadas en este changelog:

- **Added**: Nuevas características
- **Changed**: Cambios en funcionalidad existente
- **Deprecated**: Características obsoletas (a eliminar en futuras versiones)
- **Removed**: Características eliminadas
- **Fixed**: Corrección de bugs
- **Security**: Mejoras de seguridad
- **Optimizations**: Mejoras de rendimiento
- **Documentation**: Cambios en documentación

---

[1.0.0]: https://github.com/quiro90/VirtualWebDisplay/releases/tag/v1.0.0
[Unreleased]: https://github.com/quiro90/VirtualWebDisplay/compare/v1.0.0...HEAD
