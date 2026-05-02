﻿# Flujos de ejecución

## 1. Arranque completo
1. `Program.cs` crea `SingleInstanceActivator` (UI) basado en el hash del ejecutable. Si ya existe otra instancia de UI, envía una señal por `EventWaitHandle` para mostrar la ventana original y sale silenciosamente.
2. Si es la primera instancia UI, luego crea `SingleInstanceManager` (Servicio). Si un servicio background huérfano persiste, intenta cerrarlo por fuerza bruta y espera hasta 10 segundos.
3. Carga settings con `VirtualScreenSettingsStore.Load()`.
4. Crea `VirtualDisplayTrayController` (inicia hilo STA en background).
   - Internamente crea `ServiceStateManager` en estado `Stopped`
   - Se suscribe a eventos del `ServiceStateManager`
5. `RuntimeFactory.GetEnabledPorts(settings)` verifica el driver Parsec VDD y devuelve los puertos activos.
6. Muestra formulario inicial (`tray.ShowStartupConfiguration()`).
   - Usuario confirma inicio → dispara evento `StartupConfirmed`
   - Tray llama `ServiceStateManager.RequestStart()` (Stopped → Starting)
7. `WebApplication.CreateBuilder` + `Build()` — el DI container queda disponible (`ILoggerFactory`).
8. `RuntimeFactory.TryCreate(settings, hostName, localIp, loggerFactory)` construye uno o dos `ScreenRuntimeContext` con loggers reales.
9. `KestrelConfigurator.Configure(builder, ports, tlsCert)` asigna los puertos HTTP/HTTPS a Kestrel.
10. Para cada runtime:
   - crea monitor virtual (`DisplayManager.TryCreate`),
   - detecta el índice de monitor Windows (`WindowsMonitorIndex`),
   - arranca `CaptureService`,
   - arranca `WebRtcStreamService`.
11. `tray.ConfigureRuntimeActions(exitRequested, stopRequested, runtimes)`:
   - Llama `ServiceStateManager.CompleteStart(runtimes)` (Starting → Started)
   - Dispara evento `ServiceStarted` → actualiza UI/tray
12. Publica endpoints HTTP (`/`, `/cap`, `/mjpeg`, `/webrtc/offer`, `/auth/login`, `/input/touch`, `/input/stats`).
13. Actualiza tray con las URLs disponibles + balloon tip.
14. Ejecuta el servidor hasta salida (`app.RunAsync()`).
15. En `finally`: `DisposeRuntimesAsync(runtimes)` en orden inverso.
16. Si es detención (no salida): `tray.NotifyServiceStopped()` → `CompleteStop()` → evento `ServiceStopped` → actualiza UI

## 2. Creación de una pantalla virtual
1. `ScreenRuntimeContext` ya contiene un `VirtualScreenConfig`.
2. `VirtualDisplayManager.TryCreate(config)`:
   - abre el adaptador `Parsec VDD`,
   - manda IOCTL `Add`,
   - inicia keep-alive (loop `Update`),
   - detecta qué pantalla apareció en `Screen.AllScreens`,
   - aplica resolución y posición usando `VirtualDisplayPlacementOptions.GetPosition(...)`,
   - actualiza `MonitorIndex`, `Width` y `Height` reales en el config.

## 3. Captura de frame
1. `CaptureService.ExecuteAsync()` corre en loop.
2. Resuelve región con `GetCaptureRegion()` según `MonitorIndex`.
3. Copia pantalla a `Bitmap`.
4. Si corresponde, dibuja cursor.
5. Codifica JPEG con `JpegQuality` configurado.
6. Guarda bytes en `_currentFrame`.
7. Espera `CaptureIntervalSeconds` antes del próximo frame.

## 4. Modo `WebImage`
1. El navegador abre `/`.
2. `IndexHandler` devuelve HTML generado por `WebImagePageTemplate`.
3. El JS hace polling periódico a `/cap?s=N` (intervalo = `CaptureIntervalSeconds * 1000 ms`).
4. `/cap` devuelve el último JPEG disponible.
5. El cliente actualiza `background-image` de `div#screen`.
6. `object-fit` aplicado según `BrowserImageFit` (fill/cover/contain).

### iPad/Safari
En WebImage se bloquea drag/long-press nativo con:
- `div` en lugar de `<img>` para la capa de video,
- CSS `touch-action: none`, `-webkit-touch-callout: none`, `user-select: none`,
- `preventDefault()` en eventos táctiles/nativos relevantes.

### Perfil de uso ideal
- e-readers, dispositivos lentos,
- escenarios donde importa más simplicidad que latencia.

## 5. Modo `Rtc`
1. El navegador abre `/`.
2. `IndexHandler` devuelve HTML generado por `RtcPageTemplate`.
3. El JS crea `RTCPeerConnection` y `DataChannel` `frames`.
4. El cliente publica oferta SDP en `/webrtc/offer`.
5. `WebRtcStreamService.CreateAnswerAsync(...)` devuelve la respuesta SDP.
6. Cuando hay nuevos frames, el servicio los envía a peers conectados como metadata JSON + chunks binarios.
7. El cliente rearma chunks y muestra el JPEG recibido.
8. `object-fit` aplicado según `BrowserImageFit` (fill/cover/contain).

### Perfil de uso ideal
- tablets, pantallas secundarias con mejor refresco,
- menor sensación de polling.

## 6. Cambio de configuración en runtime
1. El usuario hace clic izquierdo (simple o doble) en el tray icon, o abre `Configuración...` desde el menú contextual. Si la ventana de configuración ya estaba abierta o minimizada, se restaura a su estado normal y se trae al frente (`BringToFront` / `Activate`).
2. `ResolutionConfigurationForm` trabaja sobre una copia clonada de settings (`Screen1.Clone()` + `Screen2.Clone()`).
3. Cambios en “Gestos ms” y “Táctil/Normal” se aplican y persisten en caliente, por pantalla, sin reinicio.
4. Si acepta, `ApplySelection(...)` copia valores al objeto real vía `VirtualScreenConfig.CopyTo(...)`.
5. `VirtualScreenSettingsStore.Save(...)` persiste JSON.
6. La UI avisa con balloon tip que hace falta reiniciar solo para cambios estructurales (pantallas, puertos, etc).

### Ciclo de vida de indicadores de pantalla
**Servicio detenido**:
- Botón muestra: "Iniciar" / "Start"
- Indicadores `1↗: 📺` y `2↗: 📺` están **ocultos**

**Al presionar "Iniciar"**:
- `StartupConfirmed?.Invoke()` → inicia el servicio
- `NotifyServiceStarted(screenRuntimes)` es llamado
- `_wasStarted = true`
- `UpdateScreenIndicatorsVisibility()` → muestra los indicadores
- Screen 1 siempre visible, Screen 2 solo si está habilitada
- Botón cambia a: "Detener" / "Stop"

**Durante ejecución**:
- Tooltip muestra URL actualizada al pasar el mouse: "Ingrese a: http://IP:PORT"
- Click en número/flecha (zona izquierda) → `OpenUrl()` abre navegador
- Click en 📺 (zona derecha) → `Clipboard.SetText()` + tooltip temporal "URL copiada"
- Si se habilita/deshabilita Screen 2 → solo afecta visibilidad si `_wasStarted == true`
- Al cambiar puerto → tooltip se actualiza automáticamente

**Al presionar "Detener"**:
- `StopRequested?.Invoke()` → detiene el servicio
- `NotifyServiceStopped()` es llamado
- `_wasStarted = false`
- `UpdateScreenIndicatorsVisibility()` → oculta ambos indicadores
- Botón vuelve a: "Iniciar" / "Start"

**Arquitectura**:
- `CreateScreenIndicator()`: factory method (DRY)
- `UpdateScreenIndicatorsVisibility()`: método centralizado de visibilidad
- Usa `Tag` property para almacenar referencia a `ScreenTabControls`
- Handler genérico `ScreenIndicator_Click()` para ambos indicadores

### Qué no se puede cambiar en caliente
- Puertos (solo editables en el arranque inicial).
- Creación/destrucción de pantallas virtuales (requiere reinicio).

### Qué sí cambia en caliente
- `TouchInputEnabled` por pantalla.
   - Se dispara desde `ScreenTabControls`.
   - Se propaga por `ResolutionConfigurationForm` y `ConfigurationFormPresenter`.
   - El backend (`InputHandler`) lo respeta en cada request de `/input/touch`.

## 7. Resolución de runtime por puerto
Todos los runtimes escuchan en el mismo proceso. Cada request HTTP:
1. `ResolveRuntime(HttpContext)` compara `context.Connection.LocalPort` con `runtime.Config.Port`.
2. Si ninguno coincide, usa `runtimes[0]` como fallback.

## 8. Cierre de aplicación
1. Tray invoca `ExitApplication()` o una nueva instancia solicita shutdown.
2. `app.Lifetime.StopApplication()` termina el servidor.
3. En `finally`, `DisposeRuntimesAsync(runtimes)` recorre runtimes en reversa.
4. Cada runtime:
   - detiene WebRTC (`StopAsync`),
   - detiene captura (`StopAsync`),
   - destruye monitor virtual (`Dispose`).
5. Se libera mutex/evento de instancia única.

## Decisiones de arquitectura visibles en los flujos
- El frame base siempre sale de `CaptureService`; no hay captura separada por protocolo.
- `WebImage` y `Rtc` comparten los mismos controles de intervalo (`CaptureIntervalSeconds`) y calidad JPEG (`JpegQuality`).
- Cada pantalla tiene puerto propio y runtime propio; el pattern se escala agregando más `ScreenRuntimeContext`.
- El tray es la única interfaz de operación; el servidor web no expone panel administrativo.
- `BrowserImageFit` se aplica en el CSS del HTML servido, no en el JPEG generado.
- El gate de touch está del lado backend para evitar desincronización de estado con el cliente web.
