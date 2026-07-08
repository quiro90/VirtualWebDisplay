---
tags: [componente, ui, tray, winforms]
aliases: [VirtualDisplayTrayController, Tray Controller, Tray Icon]
type: componente
updated: 2026-07-08
---

# VirtualDisplayTrayController

**Namespace**: `VirtualWebDisplay.UI.TrayIcon`
**Archivo**: `UI/TrayIcon/VirtualDisplayTrayController.cs`

Gestiona el icono de la bandeja del sistema y el menú contextual.

> [!warning] STA thread
> Corre en un **thread STA dedicado** (requisito de WinForms). `PostToUi` para operaciones thread-safe.

## Responsabilidades

- `NotifyIcon` + menú contextual dinámico (Configuración, Start/Stop, Salir).
- Gestión del ciclo de vida de `ResolutionConfigurationForm`.
- **Delegación de estado**: usa [[ServiceStateManager]] (no booleanos sueltos).
- Suscrito a `StateChanged`/`ServiceStarted`/`ServiceStopped` (reactivo).
- Coordinación vía `ConfigurationFormPresenter` ([[TrayIcon]]).

## Flujo de configuración

- Click en tray → abre `ResolutionConfigurationForm` (trabaja sobre **copia clonada** de settings).
- Cambios táctiles aplican y persisten **en caliente** (sin reinicio).
- `ApplySelection` copia valores vía `VirtualScreenConfig.CopyTo` y `VirtualScreenSettingsStore.Save`.
- Avisa con balloon tip si hace falta reiniciar (cambios estructurales).

Ver [[Cambio de Configuración en Runtime]].

## Enlaces

- [[ServiceStateManager]]
- [[ScreenRuntimeContext]]
- [[ApplicationLifecycleManager]]
- [[Cambio de Configuración en Runtime]]