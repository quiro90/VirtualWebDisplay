# Copilot Instructions

## Directrices del proyecto
- En este proyecto, el usuario prefiere que el modo WebRTC use los mismos controles configurables que Web image (intervalo y calidad JPEG) y que la configuración de usuario se persista en una carpeta oculta `.virtualwebdisplay` dentro del perfil del usuario.
- El proyecto VirtualWebDisplay usa top-level statements en `Program.cs` y tiene una carpeta `/refactoring/PLAN.md` en la raíz del repo para tracking del refactoring. El usuario prefiere llevar un fichero de tracking de refactoring en `/refactoring/PLAN.md` en la raíz del repo, con el estado de cada paso, lo que ya fue hecho y lo que falta, para poder retomar sin repetir procesos.
- Los handlers se ubican en `Controllers/Handlers/`.

## Arquitectura de Entrada Táctil
- **Modos mutuamente exclusivos**: Usar ComboBox para estados que se excluyen entre sí (ejemplo: "Tap only" vs "Gestures"), NO usar checkboxes independientes.
- **Hot-reload**: Los cambios en configuración táctil (TouchInputEnabled, TouchGesturesEnabled, TouchPreserveCursor, TouchGestureHoldDelayMs) se aplican en vivo sin reiniciar servicio. Flujo: UI → Form → Presenter → Settings → Runtime.
- **Localización**: Todo texto visible al usuario debe usar `AppText.Get("Key")`, nunca hardcodear strings. Soportar EN/ES con cambio de idioma en vivo.
- **Master/slave controls**: Cuando un control depende de otro, implementar lógica de activación/desactivación automática (ejemplo: NumericUpDown de ms solo habilitado en modo Gestures).
- **Consolidación de eventos**: Preferir eventos consolidados con tuplas (ejemplo: `TouchModeChanged(bool preserveCursor, bool gesturesEnabled)`) en vez de múltiples eventos separados.
- **Helpers genéricos**: Usar helpers como `ApplyScreenPropertyChange(screenId, Action)` para eliminar duplicación de código en presenters.
- **TouchModeItem**: Record con `(PreserveCursor: bool, GesturesEnabled: bool, DisplayName: string)` para representar modos táctiles.

## Componentes clave de entrada táctil
- `UI/Forms/ScreenTabControls.cs`: UI de configuración por pantalla, ComboBox de modos, eventos hot-reload, método `GetAccessUrl()`
- `Controllers/Handlers/InputHandler.cs`: Procesamiento de eventos táctiles, gates por config, helpers consolidados
- `wwwroot/js/touch/touch-input.js`: Script estático cliente compartido para WebImage/WebRTC
- `Configuration/Models/VirtualScreenConfig.cs`: Propiedades táctiles persistidas
- `UI/TrayIcon/ConfigurationFormPresenter.cs`: Orquestador de cambios, hot-reload sin reinicio
- `UI/Forms/ResolutionConfigurationForm.cs`: Formulario principal, gestión de indicadores de pantalla

## Arquitectura de UI y Configuración
- **Indicadores de pantalla**: URLs mostradas mediante `1↗: 📺` en parte inferior del formulario, NO en tabs
  - Solo visibles cuando servicio iniciado (`_wasStarted == true`)
  - Factory method `CreateScreenIndicator()` evita duplicación
  - Handler genérico usa `Tag` property para referencia a `ScreenTabControls`
  - Click número/flecha → abre navegador, click 📺 → copia URL
- **Visibilidad centralizada**: `UpdateScreenIndicatorsVisibility()` gestiona estado
- **Ciclo de vida**: `NotifyServiceStarted()` / `NotifyServiceStopped()` controlan visibilidad
- **Acceso a URL**: `ScreenTabControls.GetAccessUrl()` retorna URL actual

## Principios de Código Limpio
- **DRY**: Factory methods, helpers centralizados, métodos genéricos
- **Single Responsibility**: Una responsabilidad por método
- **Pattern Matching**: Early returns en vez de anidamiento
- **Uso de Tag**: Para metadatos en controles (patrón existente en `FormThemeApplicator`)
- **Helpers UI**: Usar `UI/Helpers/` (`UiDispatcherHelper`, `WindowDragHelper`, `ShellHelper`) para extraer lógica WinForms repetitiva.
- **Localización**: Eliminar claves `.resx` obsoletas al eliminar código
