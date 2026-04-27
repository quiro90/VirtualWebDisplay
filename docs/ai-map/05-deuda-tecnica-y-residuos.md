# Deuda técnica y residuos

## Objetivo de este análisis
Evaluar qué partes del workspace agregan ruido o coste de mantenimiento sin aportar al objetivo principal del proyecto:

**extender pantallas virtuales de Windows hacia dispositivos secundarios mediante acceso web**.

## Limpiezas aplicadas (historial)

### 1. Residuos de plantilla eliminados
Se eliminaron archivos que no formaban parte del dominio real:
- `VirtualWebDisplay/WeatherForecast.cs`
- `VirtualWebDisplay/Controllers/WeatherForecastController.cs`
- `VirtualWebDisplay/VirtualWebDisplay.http`

### 2. Centralización de defaults
`VirtualScreenSettingsStore` usa `CreateDefaults()` para centralizar valores por defecto.

### 3. Centralización de placement
Normalización y etiquetado de posición centralizado en `VirtualDisplayPlacementOptions.cs`.

### 4. Centralización de red
Construcción de URLs y detección de IP centralizada en `NetworkAddressHelper.cs`.

### 5. Corrección de `--vh: 85vh`
La página `BuildWebImagePage` tenía `--vh: 85vh` hardcodeado. Corregido a `100vh` para que la imagen ocupe el 100% de la pantalla del cliente.

### 6. Control de `BrowserImageFit` en UI
El campo `BrowserImageFit` ya existía en `VirtualScreenConfig` pero no tenía control en el formulario. Se agregó un combo en `ScreenTabControls` con tres opciones: Estirar (fill) / Recortar (cover) / Contener (contain). Se inicializa y guarda igual que los demás campos.

### 7. Eliminación de wrappers redundantes en `VirtualDisplayTrayController`
- `CopyConfig(source, target)` (wrapper de una línea sobre `source.CopyTo(target)`) inlineado en `ApplySelection`.
- `CloneSettings(settings)` (definido en clase externa, usado solo en `ResolutionConfigurationForm`) inlineado directamente en el constructor del form.
- `_portInput.Enabled` como estado implícito reemplazado por campo explícito `_portEditable` en `ScreenTabControls`.
- Fallbacks muertos en `Initialize` eliminados: `config.Width > 0 ? config.Width : config.CustomWidth` y `w > 0 ? w : 1080` — `EnsureValid` ya garantiza `Width > 0` antes de construir el form.

### 9. Refactoring SOLID de Program.cs y clases de infraestructura
`Program.cs` pasó de ~685 líneas (monolítico) a ~50 líneas (composition root puro).
Se extrajeron 16 clases con responsabilidad única:
- `Infrastructure/RuntimeFactory.cs`, `KestrelConfigurator.cs`, `ApplicationLifecycleManager.cs`
- `Infrastructure/Interop/CursorNativeMethods.cs`
- `Controllers/Handlers/` — 4 handlers (Auth, Index, Capture, WebRtc)
- `UI/Theme/` — ThemePalette, ThemedMenuRenderer, FormThemeApplicator
- `UI/Forms/SettingsFormValidator.cs`
- `UI/TrayIcon/TrayMenuBuilder.cs`, `ConfigurationFormPresenter.cs`

---

## Deuda técnica vigente, ordenada por prioridad

## Prioridad alta

### E. `_authorizedSessions` sin expiración — leak en uso prolongado
`ScreenSecurityGate` almacena sesiones autorizadas en un `ConcurrentDictionary<string, DateTimeOffset>` (valor = timestamp de creación) pero **nunca purga entradas antiguas**. En uso prolongado el diccionario crece indefinidamente. Además una sesión autorizada dura para siempre hasta que el proceso se reinicia, lo que es un riesgo de seguridad si el acceso físico al dispositivo cambia.

#### Riesgo
- Leak de memoria en aplicaciones que corren días/semanas sin reinicio.
- Una sesión comprometida nunca expira.

#### Limpieza sugerida
Agregar TTL de sesión configurable (ej. 8–24 hs). En `IsAuthorized`, verificar `DateTimeOffset` almacenado y remover si expiró. Opcionalmente ejecutar purga periódica en background.

---

### F. `LogDebug` en el loop principal de WebRTC — errores invisibles en producción
En `WebRtcStreamService.ExecuteAsync`, el catch genérico (línea 124) usa `_logger.LogDebug`. Con nivel de log por defecto (`Information` o `Warning`), cualquier excepción en el dispatch de frames WebRTC desaparece silenciosamente.

#### Riesgo
- Problemas de transmisión difíciles de diagnosticar en producción.

#### Limpieza sugerida
Cambiar a `LogWarning` para que sea visible con configuración estándar de logging.

---

## Prioridad media

### A. `VirtualScreenConfig.Clone()` y `CopyTo()` manuales campo a campo
Ambos métodos enumeran todas las propiedades a mano. Si se agrega un campo nuevo a `VirtualScreenConfig`, es fácil olvidar actualizarlos — especialmente `Clone()`, que no genera error de compilación si falta un campo.

#### Limpieza futura sugerida
Reemplazar con un mecanismo de copia automática (record, AutoMapper, reflexión supervisada por test) o al menos agregar un test que valide que `Clone()` y `CopyTo()` cubren todos los campos públicos.

---

### B. ~~Servicios acoplados al modelo mutable~~ — Analizado y parcialmente resuelto ✅

#### Resultado del análisis
- `WebRtcStreamService` recibía `VirtualScreenConfig` en el constructor pero **nunca lo leía**. Eliminado — constructor simplificado.
- `CaptureService` lee `CaptureIntervalSeconds`, `JpegQuality`, `StreamRotationDegrees`, `MonitorIndex`, `Width`/`Height` en cada iteración, pero el config es **efectivamente inmutable durante la ejecución** porque cualquier cambio de usuario dispara un restart completo.
- Las mutaciones de `MonitorIndex` en `RuntimeStartupHelper` ocurren **antes** de `StartAsync`, no durante la ejecución. Documentadas con comentarios.

#### Deuda residual (baja prioridad)
Si se quiere eliminar la mutación de `MonitorIndex` sobre el objeto persistido: separar en un `RuntimeScreenState` que contenga valores resueltos en startup (índice de monitor asignado por Windows) sin mezclarlos con el config guardado en disco.

---

### C. Mezcla de idioma técnico y de negocio
El código combina nombres y mensajes en inglés y español sin una convención clara. Ejemplos: comentarios XML en español en clases con nombres en inglés, strings hardcodeadas en español dentro de archivos con identifiers en inglés, docstrings bilingues en `VirtualScreenConfig`.

#### Convención recomendada
- **Código fuente** (identifiers, nombres de clases/métodos/variables): inglés.
- **Comentarios de implementación** (por qué, no qué): español (preferencia del equipo).
- **Comentarios de API pública / XML doc**: inglés.
- **Mensajes al usuario** (`AppText.resx`): siempre vía recursos, nunca hardcodeados.

#### Impacto
No rompe funcionalidad, pero aumenta fricción documental y consistencia interna.

---

### G. ~~Allocations por frame en `WebRtcStreamService.TrySendFrame`~~ ✅ Implementado
`ArrayPool<byte>.Shared` aplicado. En el caso de uso real (Kindle, calidad 40, resolución ~800×1280) los frames son ≤64KB = 1 chunk → 0 allocations por frame. `send(byte[])` serializa inmediatamente, es seguro devolver el buffer al pool tras la llamada.

---

## Baja prioridad

### D. Uso intensivo de sleeps y sondeo
Hay varios `Thread.Sleep` y polling ligero para detectar el monitor virtual o esperar frames.

#### Nota
No necesariamente está mal para este tipo de integración con Windows/driver, pero es una zona sensible si aparecen problemas de timing.

---

### H. Revisión de UI y catches silenciosos menores
La UI con modo oscuro/claro funciona correctamente. Áreas de evaluación opcional:
- `ResolutionConfigurationForm`: construcción de controles con coordenadas absolutas (`Left`, `Top`) — funciona bien pero es frágil ante cambios de tamaño. Evaluar `TableLayoutPanel` o `FlowLayoutPanel` para filas de controles.
- `ScreenTabControls`: el patrón `currentTop += N` funciona pero requiere ajuste manual si se insertan filas.
- `FormThemeApplicator.ResolveDarkMode`: el `catch { }` en lectura de registry es aceptable (falla no crítica) — agregar comentario explicando por qué se silencia.
- `CaptureService.ExecuteAsync`: el `catch { }` en el loop de captura es completamente silencioso. Errores transitorios (pantalla bloqueada, monitor desconectado) son esperados, pero errores persistentes no dejan diagnóstico. Cambiar a `catch (Exception ex) { _logger.LogDebug(ex, "Transient capture error."); }`.

---

## Criterio para limpiar sin romper el producto
En este proyecto conviene priorizar limpiezas que:
1. reduzcan ruido de plantilla,
2. centralicen reglas repetidas,
3. no alteren el flujo de creación/captura/transmisión de la pantalla virtual.

Conviene evitar refactors grandes que puedan afectar:
- detección del monitor virtual,
- negociación WebRTC,
- persistencia de settings,
- compatibilidad con perfiles tipo Kindle/iPad.

## Próximas limpiezas de mejor relación beneficio/riesgo
1. expiración de sesiones en `ScreenSecurityGate` (ítem E) — alta prioridad, bajo riesgo,
2. `LogWarning` en WebRTC dispatch (ítem F) — una línea, impacto inmediato en diagnóstico,
3. `ArrayPool` en `TrySendFrame` (ítem G) — mejora de rendimiento sin cambio de comportamiento,
4. centralizar copia/clonado de `VirtualScreenConfig` (ítem A),
5. evaluar separación entre settings editables y snapshot de runtime (ítem B).
