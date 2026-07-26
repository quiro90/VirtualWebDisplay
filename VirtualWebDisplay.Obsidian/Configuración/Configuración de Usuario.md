---
tags: [config, persistencia, json]
aliases: [Configuración de Usuario, User Config, virtualscreen.user.json]
type: referencia
updated: 2026-07-08
---

# Configuración de Usuario

## Ubicación

`%USERPROFILE%\.virtualwebdisplay\` (archivos ocultos en Windows).

## Archivos

| Archivo | Contenido | Store |
|---|---|---|
| `virtualscreen.user.json` | Config raíz (Screen1, Screen2, UI) | [[VirtualScreenSettingsStore]] |
| `virtualscreen.display.json` | Estado hardware (resolución + posición X/Y) | `VirtualDisplayResolutionStore` + `VirtualResolutionWatcher` |
| `ui-preferences.user.json` | UI e idioma | `AppearanceSettingsStore` |
| `localca.pfx` | Cert SSL (PKCS#12 con clave) | [[Certificado SSL (HTTPS)]] |
| `localca.crt` | Cert SSL público (descargable vía `GET /cert`) | [[Certificado SSL (HTTPS)]] |

## Objeto raíz

`VirtualWebDisplaySettings` (`Configuration/Models/`):
```text
Screen1: VirtualScreenConfig
Screen2: VirtualScreenConfig
UiLanguage: "es" | "en"
WindowTheme: "system" | ...
```

## Defaults por pantalla

- **Screen1**: `Enabled=true`, `Port=8000`, `VirtualDisplayPlacement="right"`, `TransmissionMethod="Rtc"`.
- **Screen2**: `Enabled=false`, `Port=8002`, `VirtualDisplayPlacement="left"`, `TransmissionMethod="Rtc"`.

HTTPS = `Port + 1`.

## Qué cambia en caliente vs qué requiere reinicio

> [!warning]
> **En caliente** (sin reiniciar servicio): `TouchInputEnabled` y todos los gestos/delays táctiles por pantalla.
> **Requiere reinicio**: puertos, creación/destrucción de pantallas virtuales, cambios estructurales.

## Compatibilidad legacy

`VirtualScreenSettingsStore` migra el formato antiguo (sección `VirtualScreen` suelta) → `Screen1` + `Screen2` (con `Screen2 = CreateScreen2Defaults()`). También migra el `TouchGestureHoldDelay` global legacy al `TouchHoldDelayMs` de cada pantalla. La **rotación de stream** fue removida del flujo activo (no existe `StreamRotationDegrees`).

## Enlaces

- [[VirtualScreenConfig (Campos)]]
- [[VirtualScreenSettingsStore]]
- [[Cambio de Configuración en Runtime]]

## Continuar con
- [[VirtualScreenConfig (Campos)]]
- [[VirtualScreenSettingsStore]]
