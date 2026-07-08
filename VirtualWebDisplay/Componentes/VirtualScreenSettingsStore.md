---
tags: [componente, config, persistencia, json]
aliases: [VirtualScreenSettingsStore, Settings Store, Persistencia]
type: componente
updated: 2026-07-08
---

# VirtualScreenSettingsStore

**Namespace**: `VirtualWebDisplay.Configuration`
**Archivo**: `Configuration/VirtualScreenSettingsStore.cs`

Persiste/carga [[Configuración de Usuario|VirtualWebDisplaySettings]] en JSON.

## Ubicación

`%USERPROFILE%\.virtualwebdisplay\virtualscreen.user.json`

> [!info]
> Archivos ocultos en Windows. Los cambios se guardan al aplicar config en el formulario.

## Compatibilidad hacia atrás

Sabe leer un formato **legado** con sección `VirtualScreen` y migrarlo a:
- `Screen1 = legacyConfig`
- `Screen2 = CreateScreen2Defaults()`

## Otros archivos de estado

| Archivo | Contenido |
|---|---|
| `virtualscreen.user.json` | Preferencias de usuario (config) |
| `virtualscreen.display.json` | Estado dinámico del hardware (resolución + posición X/Y en Windows), vigilado por `VirtualResolutionWatcher` |
| `ui-preferences.user.json` | UI e idioma |
| `localhost.pfx` | Cert SSL — ver [[Certificado SSL (HTTPS)]] |

> [!important] Separación de estado
> Preferencias de usuario → `virtualscreen.user.json`. Estado del hardware → `virtualscreen.display.json`. No mezclar.

## Nombres legacy (no usar)

`HttpPort`, `TransmissionMode`, `CaptureIntervalMs`, `Rotation` → versiones anteriores. El formato actual usa `Port`, `TransmissionMethod`, `CaptureIntervalSeconds`, `BrowserImageFit`. La **rotación de stream fue removida** del flujo activo.

## Enlaces

- [[Configuración de Usuario]]
- [[VirtualScreenConfig (Campos)]]
- [[Cambio de Configuración en Runtime]]