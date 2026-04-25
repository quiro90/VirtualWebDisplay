# Flujos de ejecución

## 1. Arranque completo
1. `Program.cs` crea `SingleInstanceManager`.
2. Si ya existe otra instancia, intenta cerrarla y espera hasta 10 segundos.
3. Carga settings con `VirtualScreenSettingsStore.Load()`.
4. Crea `VirtualDisplayTrayController` (inicia hilo STA en background).
5. Verifica `Parsec VDD` con `VirtualDisplayManager.VerifyDriverAvailability()`.
6. Muestra formulario inicial (`tray.ShowStartupConfiguration()`).
7. Construye uno o dos `ScreenRuntimeContext` según `Screen2.Enabled`.
8. Configura el host web para escuchar en todos los puertos activos (`UseUrls`).
9. Para cada runtime:
   - crea monitor virtual (`DisplayManager.TryCreate`),
   - detecta el índice de monitor Windows (`WindowsMonitorIndex`),
   - arranca `CaptureService`,
   - arranca `WebRtcStreamService`.
10. Publica endpoints HTTP (`/`, `/cap`, `/mjpeg`, `/webrtc/offer`, `/config`).
11. Actualiza tray con las URLs disponibles + balloon tip.
12. Ejecuta el servidor hasta salida (`app.Run()`).
13. En `finally`: `DisposeRuntimesAsync(runtimes)` en orden inverso.

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
5. Si `RotateForPortrait` está activo, rota 90°.
6. Codifica JPEG con `JpegQuality` configurado.
7. Guarda bytes en `_currentFrame`.
8. Espera `CaptureIntervalSeconds` antes del próximo frame.

## 4. Modo `WebImage`
1. El navegador abre `/`.
2. `Program.cs` devuelve HTML generado por `BuildWebImagePage(...)`.
3. El JS hace polling periódico a `/cap?s=N` (intervalo = `CaptureIntervalSeconds * 1000 ms`).
4. `/cap` devuelve el último JPEG disponible.
5. El cliente reemplaza el `src` del `<img>`.
6. `object-fit` aplicado según `BrowserImageFit` (fill/cover/contain).

### Perfil de uso ideal
- e-readers, dispositivos lentos,
- escenarios donde importa más simplicidad que latencia.

## 5. Modo `Rtc`
1. El navegador abre `/`.
2. `Program.cs` devuelve HTML generado por `BuildRtcPage(...)`.
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
1. El usuario abre `Configuración...` desde el tray (doble clic o menú contextual).
2. `ResolutionConfigurationForm` edita una copia de settings (`CloneSettings`).
3. Si acepta, `ApplySelection(...)` copia valores al objeto real vía `CopyConfig`.
4. `VirtualScreenSettingsStore.Save(...)` persiste JSON.
5. La UI avisa con balloon tip que hace falta reiniciar para recrear pantallas y puertos.

### Qué no se puede cambiar en caliente
- Puertos (solo editables en el arranque inicial).
- Creación/destrucción de pantallas virtuales (requiere reinicio).

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
