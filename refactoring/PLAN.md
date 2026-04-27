# Plan de refactoring — VirtualWebDisplay

## Estado general
Refactoring completado en todas las áreas planificadas hasta la fecha. No hay tareas pendientes activas.

---

## Historial de cambios completados

### ? Centralización de theming (`FormThemeApplicator`)
- `TryCreateUiFont()` movida a `FormThemeApplicator` (antes duplicada en `ResolutionConfigurationForm` y `CustomModesDialog`)
- `ApplyThemeRecursive` soporta `Tag = "preserve-color"` en paneles para no sobrescribir `BackColor` intencional (p.ej. panel de advertencia amarillo)

### ? Resoluciones personalizadas Parsec VDD
- `Parsec/VddCustomModesStore.cs` — lee/escribe 5 slots en `HKLM\SOFTWARE\Parsec\vdd\{0..4}`
- `UI/Forms/CustomModesDialog.cs` — diálogo con 5 slots W×H@Hz, botones Reset/Save, panel de advertencia amarillo
- `Program.cs` — manejo de argumento `--set-custom-modes` para flujo UAC
- Menú ? ? "Resoluciones personalizadas..." en `ResolutionConfigurationForm`
- Localizaciones EN + ES añadidas (`CustomModes_*`)

### ? Bloqueo UI mientras el servicio corre
- `ScreenTabControls.SetServiceRunning(bool)` — deshabilita `_managedControls` excepto `_windowsDisplayButton`
- `ResolutionConfigurationForm.SetConfigurationControlsLocked(bool)` — deshabilita `_enableScreen2Check` y llama `SetServiceRunning` en ambas tabs
- Se llama desde `NotifyServiceStarted`, `NotifyServiceStopped` y el constructor (si `_wasStarted = true`)

### ? Limpieza de código duplicado (`ScreenTabControls`)
- `_serviceRunning` movido al bloque de campos al inicio de la clase
- `UpdateSecurityCodePreview` refactorizado: extraído `DisableSecurityCodePreview(string text)` — eliminadas 3 ramas de 4 líneas repetidas

### ? Unificación path de éxito en `CustomModesDialog`
- `ShowSavedAndClose()` helper unifica `MessageBox(Saved) + Close()` usado antes por duplicado en `CommitModes` y la rama UAC

### ? URL del driver Parsec VDD actualizada
- `VirtualDisplayManager.InstallUrl` apunta a `https://builds.parsec.app/vdd/parsec-vdd-0.45.0.0.exe` (instalador directo)

---

## Tareas pendientes

_Ninguna actualmente._
