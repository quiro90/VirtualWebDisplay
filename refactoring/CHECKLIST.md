# Checklist de mejoras técnicas — VirtualWebDisplay

Archivo de tracking para avanzar sobre la deuda técnica identificada.
Referencia completa: `docs/ai-map/05-deuda-tecnica-y-residuos.md`

Formato de estado: `[ ]` pendiente · `[~]` en progreso · `[x]` completado

---

## 🔴 Prioridad alta

### E. Expiración de sesiones en `ScreenSecurityGate`
**Archivo:** `VirtualWebDisplay_Parsec/Infrastructure/ScreenSecurityGate.cs`

- [x] Agregar constante `SessionTtl` (ej. `TimeSpan.FromHours(8)`)
- [x] En `IsAuthorized`: verificar que `_authorizedSessions[sessionId]` + TTL > `UtcNow`; si expiró, remover y retornar `false`
- [x] En `TryAuthorize`: al crear sesión, guardar timestamp de creación (ya existe) — confirmar que la lectura en `IsAuthorized` lo usa
- [x] Agregar método privado `PurgeExpiredSessions()` que recorre y remueve sesiones vencidas
- [x] Llamar `PurgeExpiredSessions()` desde `IsAuthorized` (lazy cleanup) o desde un timer periódico
- [x] Verificar que el cookie de sesión en el navegador tiene el mismo tiempo de vida (o se invalida server-side correctamente)

---

### F. `LogDebug` → `LogWarning` en WebRTC dispatch
**Archivo:** `VirtualWebDisplay_Parsec/Streaming/WebRtcStreamService.cs`

- [x] En `ExecuteAsync`, cambiar `_logger.LogDebug(ex, "No se pudo enviar un frame WebRTC.")` a `_logger.LogWarning`
- [x] Revisar si hay otros `LogDebug` en paths de error (ej. `RemovePeer`) — cambiado a `LogInformation`
- [x] Verificar que `appsettings.json` no tiene nivel mínimo que tape estos logs en producción (`Default: Warning` — `LogWarning` es visible ✓)

---

## 🟡 Prioridad media

### G. Allocations en `TrySendFrame` — usar `ArrayPool`
**Archivo:** `VirtualWebDisplay_Parsec/Streaming/WebRtcStreamService.cs`

- [x] Agregar `using System.Buffers;` al archivo
- [x] En `TrySendFrame`: reemplazar `new byte[4 + chunkLength]` por `ArrayPool<byte>.Shared.Rent(4 + chunkLength)`
- [x] Devolver el buffer con `ArrayPool<byte>.Shared.Return(chunk)` en un `try/finally` después del `channel.send(chunk)`
- [x] Confirmar que `channel.send(byte[])` consume el buffer inmediatamente — SIPSorcery serializa el payload sobre SCTP/DTLS en la misma llamada, no retiene la referencia ✓
- [x] `channel.send(chunk[..chunkSize])` — slice exacto para no enviar bytes no inicializados del buffer rentado

---

### A. `Clone()` y `CopyTo()` frágiles en `VirtualScreenConfig`
**Archivo:** `VirtualWebDisplay_Parsec/Configuration/Models/VirtualScreenConfig.cs`

Opción preferida: **test de cobertura** (menor riesgo que convertir a record)

- [x] Crear proyecto de tests `VirtualWebDisplay.Tests` (xUnit, `net10.0-windows`)
- [x] Agregar `ProjectReference` al proyecto principal y agregar a la solución
- [x] Agregar test `Clone_CopiesAllPublicSettableProperties` — reflexión sobre todas las propiedades públicas con setter
- [x] Agregar test `Clone_ReturnsNewInstance` — verifica que no se devuelve la misma referencia
- [x] Agregar test `CopyTo_CopiesAllPublicSettableProperties` — mismo mecanismo de reflexión
- [x] Agregar test `Clone_And_CopyTo_CoverSameProperties` — meta-test: verifica que `BuildNonDefaultConfig()` asigna valores no-default a **todas** las propiedades; si se agrega una propiedad nueva sin actualizar el test, falla con mensaje exacto del nombre de la propiedad faltante
- [x] Verificar detección: propiedad temporal agregada y no cubierta → test falla con `"BuildNonDefaultConfig does not override these properties: TestUncopiedProperty"` ✓
- [ ] Evaluar si conviene convertir `VirtualScreenConfig` a `record` (cambio más grande — ver ítem B antes)

---

### B. Snapshot de runtime vs. configuración editable
**Archivos:** `WebRtcStreamService.cs`, `ScreenRuntimeContext.cs`, `RuntimeStartupHelper.cs`

- [x] Analizar qué propiedades de `VirtualScreenConfig` leen `CaptureService` y `WebRtcStreamService`
  - `CaptureService`: lee `CaptureIntervalSeconds`, `JpegQuality`, `StreamRotationDegrees`, `MonitorIndex`, `Width`/`Height` — en cada iteración del loop
  - `WebRtcStreamService`: **no leía ninguna** — recibía `VirtualScreenConfig` en el constructor pero nunca lo accedía ✓ eliminado
- [x] `WebRtcStreamService._config` eliminado — dependencia inyectada pero nunca usada; constructor simplificado
- [x] `ScreenRuntimeContext` actualizado — ya no pasa `config` al constructor de `WebRtcStreamService`
- [x] Mutaciones de `MonitorIndex` en `RuntimeStartupHelper` documentadas con comentario — son intencionales y ocurren **antes** de `StartAsync`, no durante la ejecución
- [x] Confirmar que el riesgo real del ítem B es bajo: `VirtualScreenConfig` es efectivamente inmutable durante la ejecución porque cualquier cambio de usuario dispara un restart completo de los servicios
- [ ] (Opcional/futuro) Si se quiere formalizar: separar `MonitorIndex` del config persistido en un `RuntimeScreenConfig` para que la mutación de startup no toque el objeto guardado en disco

---

### C. Convención de idioma en el código fuente
**Afecta:** todo el proyecto

Convención acordada:
- **Identificadores** (clases, métodos, variables, propiedades): **inglés**
- **Comentarios de implementación** (por qué, contexto): **español** (preferencia del equipo)
- **Comentarios XML doc de API pública**: **inglés**
- **Strings al usuario**: siempre via `AppText.resx`, nunca hardcodeados

Pasos:
- [x] Identificar comentarios XML doc en español en clases públicas — traducidos al inglés en 8 archivos: `IHtmlTemplate`, `RtcPageTemplate`, `WebImagePageTemplate`, `ThemePalette`, `FormThemeApplicator` (4 docs), `TrayMenuBuilder`, `VirtualDisplayTrayController`, `ConfigurationFormPresenter` (4 docs incluyendo eventos)
- [x] Identificar strings hardcodeados en español fuera de recursos — encontrados 6 strings en `VirtualDisplayManager.cs` (mensajes de diagnóstico del driver VDD)
- [x] Mover strings de `VirtualDisplayManager.cs` a recursos — agregadas 7 keys (`VDD_Status_*`) a `AppText.resx` (inglés) y `AppText.es.resx` (español), reemplazadas con `AppText.Get`/`AppText.Format`
- [x] Audit pre-H: corregido `DateTimeOffset.UtcNow` doble en `ScreenSecurityGate.TryAuthorize` (capturado una vez en `var now`)
- [x] Audit pre-H: traducida string de excepción en español en `WebRtcStreamService.CreateAnswerAsync` a inglés
- [x] Audit pre-H: 6 strings hardcodeadas en español en `VirtualDisplayManager` (`TryCreate`, `TryReconfigure`, `ArrangeVirtualDisplay`) migradas a 7 nuevas keys `VDD_Status_*` en ambos resx
- [x] Revisar docstrings bilingues en `VirtualScreenConfig.cs` — los comentarios XML son en inglés (los que tenían español son comentarios `//` de implementación, que se mantienen en español por convención)
- [x] Revisar comentarios `// Fila N:` en `ScreenTabControls.cs` — son comentarios de implementación, se mantienen en español ✓

---

## 🔵 Baja prioridad

### H. Revisión UI — posibles abstracciones menores
**Archivos:** `ResolutionConfigurationForm.cs`, `ScreenTabControls.cs`, `FormThemeApplicator.cs`, `CaptureService.cs`

- [x] Evaluar reemplazar coordenadas absolutas en `ResolutionConfigurationForm` por `TableLayoutPanel` para las filas de botones inferiores — resuelto con `Anchor = Bottom | Right` en ambos botones (Accept/Cancel), sin necesidad de TableLayoutPanel
- [x] Evaluar si el patrón `currentTop += N` en `ScreenTabControls` puede encapsularse — patrón válido, filas ya comentadas, sin riesgo real de desalineación; no se toca
- [x] Documentar el `catch { }` en `FormThemeApplicator.ResolveDarkMode` — comentario añadido explicando que falla en entornos restringidos y el fallback es modo claro
- [x] Verificar que los controles custom (`ThemedComboBox`, `ThemedNumericUpDown`, `ThemedTrackBar`) tienen `Dispose` correcto — todos los recursos GDI (Pen, SolidBrush, Font) usan `using`; ningún recurso queda sin liberar ✓
- [x] `CaptureService.ExecuteAsync`: `catch { }` silencioso → `catch (Exception ex) { _logger.LogWarning(...) }` — inyectado `ILogger<CaptureService>` via constructor; `ILoggerFactory` propagado desde `app.Services` a través de `RuntimeFactory.GetEnabledPorts` → `RuntimeFactory.TryCreate` → `ScreenRuntimeContext`

---

### D. Sleeps y polling en integración con driver Parsec
- [ ] Identificar todos los `Thread.Sleep` y `Task.Delay` en el flujo de detección del monitor virtual
- [ ] Documentar cada uno con el motivo técnico (por qué no se puede usar evento/callback)
- [ ] Evaluar si alguno puede reemplazarse con `WaitHandle` o polling con backoff exponencial

---

## Notas de implementación

- Al trabajar en **E** (sesiones), asegurarse de que el test de sesión válida sigue funcionando tras el cambio.
- Al trabajar en **G** (ArrayPool), el riesgo de regresión es bajo si `channel.send(byte[])` hace copia interna — confirmar en la documentación de SIPSorcery.
- Los ítems **A** y **B** están relacionados: si se convierte `VirtualScreenConfig` a `record` para resolver A, también facilita B. Conviene resolverlos juntos.
- El ítem **C** (idiomas) es incremental — se puede resolver archivo por archivo sin riesgo.
