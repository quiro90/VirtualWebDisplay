# Flujos de ejecución

## 1. Arranque completo
1. `Program.cs` crea `SingleInstanceManager`.
2. Si ya existe otra instancia, intenta cerrarla y relanzar limpio.
3. Carga settings con `VirtualScreenSettingsStore.Load()`.
4. Crea `VirtualDisplayTrayController`.
5. Verifica `Parsec VDD` con `VirtualDisplayManager.VerifyDriverAvailability()`.
6. Muestra formulario inicial.
7. Construye uno o dos `ScreenRuntimeContext` según `Screen2.Enabled`.
8. Configura el host web para escuchar en todos los puertos activos.
9. Para cada runtime:
   - crea monitor virtual,
   - detecta el índice de monitor Windows,
   - arranca `CaptureService`,
   - arranca `WebRtcStreamService`.
10. Publica endpoints HTTP.
11. Actualiza tray con las URLs disponibles.
12. Ejecuta el servidor hasta salida.

## 2. Creación de una pantalla virtual
1. `ScreenRuntimeContext` ya contiene un `VirtualScreenConfig`.
2. `VirtualDisplayManager.TryCreate(config)`:
   - abre el adaptador `Parsec VDD`,
   - manda IOCTL `Add`,
   - inicia keep-alive,
   - detecta qué pantalla apareció en `Screen.AllScreens`,
   - ajusta resolución y posición,
   - actualiza `MonitorIndex`, `Width` y `Height` reales.

## 3. Captura de frame
1. `CaptureService.ExecuteAsync()` corre en loop.
2. Resuelve región con `GetCaptureRegion()`.
3. Copia pantalla a `Bitmap`.
4. Si corresponde, dibuja cursor.
5. Si corresponde, rota a portrait.
6. Codifica JPEG con la calidad configurada.
7. Guarda bytes en `_currentFrame`.

## 4. Modo `WebImage`
1. El navegador abre `/`.
2. `Program.cs` devuelve HTML generado por `BuildWebImagePage(...)`.
3. El JS hace polling periódico a `/cap?s=N`.
4. `/cap` devuelve el último JPEG disponible.
5. El cliente reemplaza el `src` del `<img>`.

### Perfil de uso ideal
- e-readers,
- dispositivos lentos,
- escenarios donde importa más simplicidad que latencia.

## 5. Modo `Rtc`
1. El navegador abre `/`.
2. `Program.cs` devuelve HTML generado por `BuildRtcPage(...)`.
3. El JS crea `RTCPeerConnection` y `DataChannel` `frames`.
4. El cliente publica oferta en `/webrtc/offer`.
5. `WebRtcStreamService.CreateAnswerAsync(...)` devuelve la respuesta SDP.
6. Cuando hay nuevos frames, el servicio los envía a peers conectados.
7. El cliente rearma chunks y muestra el JPEG recibido.

### Perfil de uso ideal
- tablets,
- pantallas secundarias con mejor refresco,
- menor sensación de polling.

## 6. Cambio de configuración
1. El usuario abre `Configuración...` desde el tray.
2. `ResolutionConfigurationForm` edita una copia de settings.
3. Si acepta, `ApplySelection(...)` copia valores al objeto real.
4. `VirtualScreenSettingsStore.Save(...)` persiste JSON.
5. La UI avisa que hace falta reiniciar para recrear pantallas y puertos.

## 7. Cierre de aplicación
1. Tray o instancia nueva solicitan salida.
2. `app.Lifetime.StopApplication()` termina el servidor.
3. En `finally`, `DisposeRuntimesAsync(...)` recorre runtimes en reversa.
4. Cada runtime:
   - detiene WebRTC,
   - detiene captura,
   - destruye monitor virtual.
5. Se libera mutex/evento de instancia única.

## Decisiones de arquitectura visibles en los flujos
- El frame base siempre sale de `CaptureService`; no hay captura separada por protocolo.
- `WebImage` y `Rtc` comparten controles de intervalo y calidad JPEG.
- Cada pantalla tiene puerto propio y runtime propio.
- El tray es la interfaz de operación; el servidor web no expone panel administrativo.