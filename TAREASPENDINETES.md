# TAREAS PENDIENTES - Auditoria VirtualWebDisplay

Estado: borrador inicial basado en revision tecnica del codigo actual.

## Quick Wins (hacer primero)

- [x] Refactor IndexHandler para separar armado de parametros y seleccion de template.
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/IndexHandler.cs
  - Accion aplicada: extraidos BuildTemplateParameters y GenerateDisplayPage sin cambiar flujo.

- [x] Centralizar respuesta exitosa de autenticacion en helper comun.
  - Archivos: VirtualWebDisplay_Parsec/Infrastructure/Runtime/RuntimeAccessHelper.cs, VirtualWebDisplay_Parsec/Web/Handlers/AuthHandler.cs
  - Accion aplicada: agregado RuntimeAccessHelper.AuthorizedResult() y reemplazados Results.Ok(new { authorized = true }) duplicados.

- [x] Agregar cobertura de IndexHandler (security/viewer-limit/webimage/rtc).
  - Archivo: VirtualWebDisplay.Tests/Web/Handlers/IndexHandlerTests.cs
  - Accion aplicada: agregados tests para ramas de autorizacion, limite de viewers y render por modo de transmision.

- [x] Refactor transversal de autorizacion en handlers web usando RuntimeAccessHelper.TryResolveAuthorizedRuntime.
  - Archivos: VirtualWebDisplay_Parsec/Infrastructure/Runtime/RuntimeAccessHelper.cs, VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs, VirtualWebDisplay_Parsec/Web/Handlers/ConfigHandler.cs, VirtualWebDisplay_Parsec/Web/Handlers/KeepaliveHandler.cs, VirtualWebDisplay_Parsec/Web/Handlers/WebRtcHandler.cs, VirtualWebDisplay_Parsec/Web/Handlers/CaptureHandler.cs.
  - Accion aplicada: centralizada la logica de resolve+auth y eliminado codigo repetido en handlers.

- [x] Unificar respuesta 401 en ruta MJPEG usando IResult centralizado.
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/CaptureHandler.cs
  - Accion aplicada: eliminado 401 manual con WriteAsJsonAsync y reemplazado por ejecucion de RuntimeAccessHelper unauthorizedResult.

- [x] Unificar respuesta 429 de viewer limit en handlers web.
  - Archivos: VirtualWebDisplay_Parsec/Infrastructure/Runtime/RuntimeAccessHelper.cs, VirtualWebDisplay_Parsec/Web/Handlers/CaptureHandler.cs, VirtualWebDisplay_Parsec/Web/Handlers/WebRtcHandler.cs
  - Accion aplicada: creado RuntimeAccessHelper.ViewerLimitExceededResult/WriteViewerLimitExceededAsync y reemplazadas variantes manuales de 429.

- [x] Unificar respuesta 400 de validacion en handlers web.
  - Archivos: VirtualWebDisplay_Parsec/Infrastructure/Runtime/RuntimeAccessHelper.cs, VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs, VirtualWebDisplay_Parsec/Web/Handlers/WebRtcHandler.cs
  - Accion aplicada: creado RuntimeAccessHelper.BadRequestError(message) y reemplazadas respuestas 400 manuales duplicadas.

- [x] Centralizar estados HTTP adicionales (404, 429 generico, 500, 503).
  - Archivos: VirtualWebDisplay_Parsec/Infrastructure/Runtime/RuntimeAccessHelper.cs, VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs, VirtualWebDisplay_Parsec/Web/Handlers/CaptureHandler.cs
  - Accion aplicada: agregados helpers NotFoundResult, TooManyRequestsResult, InternalServerErrorResult, ServiceUnavailableResult y reemplazados usos directos.

- [x] Centralizar respuesta HTML en helper comun para handlers web.
  - Archivos: VirtualWebDisplay_Parsec/Infrastructure/Runtime/RuntimeAccessHelper.cs, VirtualWebDisplay_Parsec/Web/Handlers/IndexHandler.cs
  - Accion aplicada: agregado RuntimeAccessHelper.HtmlContent(html) y reemplazados Results.Content(..., "text/html") en IndexHandler.

- [x] Ampliar tests de estados/helpers centralizados en RuntimeAccessHelper.
  - Archivo: VirtualWebDisplay.Tests/Infrastructure/RuntimeAccessHelperTests.cs
  - Accion aplicada: agregados tests para NotFound(404), TooManyRequests(429), InternalServerError(500), ServiceUnavailable(503) y HtmlContent(text/html).

- [x] Ampliar cobertura de RuntimeAccessHelper con tests del nuevo helper de autorizacion.
  - Archivo: VirtualWebDisplay.Tests/Infrastructure/RuntimeAccessHelperTests.cs
  - Accion aplicada: agregados casos TryResolveAuthorizedRuntime en escenarios autorizado/no autorizado.

- [x] Refactor RuntimeAccessHelper (resolucion por puerto centralizada y sin magic duplication).
  - Archivo: VirtualWebDisplay_Parsec/Infrastructure/Runtime/RuntimeAccessHelper.cs
  - Accion aplicada: extraidos helpers TryResolveRuntimeByPort y MatchesRuntimePort, manteniendo fallback y comportamiento.

- [x] Ampliar tests de RuntimeAccessHelper (resolucion runtime, viewer key y unauthorized).
  - Archivo: VirtualWebDisplay.Tests/Infrastructure/RuntimeAccessHelperTests.cs
  - Accion aplicada: agregados tests para puertos HTTP/HTTPS, fallback, cookie session, IP fallback y respuestas 401.

- [x] Refactor Program.cs (entrypoint) para reducir complejidad del parseo de --set-custom-modes.
  - Archivo: VirtualWebDisplay_Parsec/Program.cs
  - Accion aplicada: extraidos helpers locales TryGetCustomModesArgument y ParseCustomModesArgument sin cambiar flujo de inicio.

- [x] Consolidar infraestructura de tests web en helper compartido.
  - Archivo: VirtualWebDisplay.Tests/Web/Handlers/WebHandlerTestHelper.cs
  - Accion aplicada: InputHandlerTests migrado a helper compartido para reducir duplicacion y facilitar nuevas pruebas de handlers.

- [x] Refactor InputHandler Fase 1 (extraccion de flujo principal sin cambiar contrato HTTP).
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs
  - Accion aplicada: extraidos metodos TryHandleGestureEndAction, TryResolveDesktopCoordinates, HandleLegacyAction y HandleSemanticAction.

- [x] Refactor InputHandler Fase 2 (extraer validacion, autorizacion y gates de acciones).
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs
  - Accion aplicada: extraidos metodos TryValidateTouchRequest, ResolveAuthorizedRuntime, TryHandleDisabledSemanticAction y ExecutePointerAction.

- [x] Refactor InputHandler Fase 3 (extraer telemetria/rate-limit y ayudas de cursor/estadisticas).
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs
  - Accion aplicada: extraidos TryRejectByRateLimit, GetTouchStatsSnapshot, SaveCursorIfNeeded y RestoreCursorIfNeeded.

- [x] Refactor InputHandler Fase 4 (centralizar acciones/tipos y eliminar magic strings).
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs
  - Accion aplicada: introducidas constantes de acciones/tipos tactiles y helpers de clasificacion (IsDragAction, IsScrollAction, IsGestureEndAction).

- [x] Refactor InputHandler Fase 5 (descomposicion de ejecucion de gestos).
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs
  - Accion aplicada: extraidos metodos ExecuteDragStart/ExecuteDragMove/ExecuteDragEnd y ExecuteScrollMove/ExecuteScrollEnd para reducir complejidad del switch principal.

- [x] Refactor InputHandler Fase 6 (encapsular estado mutable interno).
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs
  - Accion aplicada: encapsulados rate limiting, telemetria y estado de drag en componentes internos privados (RateLimiterRegistry, InputTelemetry, DragStateTracker).

- [x] Refactor InputHandler Fase 7 (dispatcher interno de acciones).
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs
  - Accion aplicada: extraido ActionDispatcher para enrutamiento pre/post coordenadas y simplificacion del metodo principal.

- [x] Invertir scroll de 2 dedos para comportamiento mas natural.
  - Archivos: VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs, VirtualWebDisplay_Parsec/Infrastructure/Input/MouseInputHelper.cs
  - Accion aplicada: cambio de signo en dy (MouseInputHelper.Scroll(dy, dx)) manteniendo intactos los gates por opciones TouchScrollEnabled/TouchInputEnabled.

- [x] Agregar tests de regresion para TransmissionModeOptions.
  - Archivo: VirtualWebDisplay.Tests/Configuration/TransmissionModeOptionsTests.cs

- [x] Agregar tests de regresion para VirtualDisplayPlacementOptions.
  - Archivo: VirtualWebDisplay.Tests/Configuration/VirtualDisplayPlacementOptionsTests.cs

- [x] Agregar tests de regresion para ViewerLimiter (capacidad y conteos).
  - Archivo: VirtualWebDisplay.Tests/Web/Security/ViewerLimiterTests.cs

- [x] Limpiar comentario XML truncado al final de ApplicationLifecycleManager.
  - Archivo: VirtualWebDisplay_Parsec/Infrastructure/Hosting/ApplicationLifecycleManager.cs

- [x] Agregar cobertura unitaria para TouchGestureOptions.ClampDelay (min/max/rango).
  - Archivo: VirtualWebDisplay.Tests/Configuration/TouchGestureOptionsTests.cs

- [x] Agregar cobertura unitaria para RuntimeAccessHelper.NormalizeBrowserImageFit.
  - Archivo: VirtualWebDisplay.Tests/Infrastructure/RuntimeAccessHelperTests.cs

- [x] Agregar test especifico: Clone copia SavedPositionX/SavedPositionY.
  - Archivo: VirtualWebDisplay.Tests/Configuration/VirtualScreenConfigCopyTests.cs

- [x] Agregar test especifico: CopyTo copia SavedPositionX/SavedPositionY.
  - Archivo: VirtualWebDisplay.Tests/Configuration/VirtualScreenConfigCopyTests.cs

- [x] Agregar test especifico: Clone y CopyTo copian H264BitrateKbps/H264Framerate.
  - Archivo: VirtualWebDisplay.Tests/Configuration/VirtualScreenConfigCopyTests.cs

- [x] Limpiar imports duplicados en SingleInstanceManager (Hosting).
  - Archivo: VirtualWebDisplay_Parsec/Infrastructure/Hosting/SingleInstanceManager.cs
  - Accion aplicada: eliminado bloque duplicado de using.

- [x] Corregir estado global compartido en InputHandler para evitar race conditions entre requests y entre pantallas.
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs
  - Accion aplicada: eliminados campos static de request (_runtime, _virtualX, _virtualY) y migrado a estado local por request.

- [x] Eliminar bloqueos sincronicos sobre tareas async en UI/lifecycle.
  - Archivos: VirtualWebDisplay_Parsec/UI/TrayIcon/VirtualDisplayTrayController.cs, VirtualWebDisplay_Parsec/Parsec/VirtualDisplayManager.cs, VirtualWebDisplay_Parsec/Infrastructure/Hosting/SingleInstanceManager.cs
  - Accion aplicada: reemplazado GetAwaiter().GetResult en ShowStartupConfiguration por flujo asincronico (ShowStartupConfigurationAsync + await en Program), migrados loops de listener/keepalive para no depender de Task async + Task.Delay y eliminado _ready.Wait() en inicializacion del tray mediante TaskCompletionSource de readiness.

- [x] Cerrar hueco detectado en tests de copia de configuracion.
  - Archivo: VirtualWebDisplay.Tests/Configuration/VirtualScreenConfigCopyTests.cs
  - Accion propuesta: actualizar BuildNonDefaultConfig para setear SavedPositionX, SavedPositionY, H264BitrateKbps y H264Framerate con valores no default.

## Importantes (siguiente fase)

- [x] Unificar manejo de excepciones silenciosas en servicios de infraestructura.
  - Archivos: VirtualWebDisplay_Parsec/Infrastructure/Hosting/SingleInstanceManager.cs, VirtualWebDisplay_Parsec/Infrastructure/Hosting/ApplicationBootstrapper.cs, VirtualWebDisplay_Parsec/Parsec/VirtualDisplayManager.cs, VirtualWebDisplay_Parsec/Infrastructure/Runtime/ScreenRuntimeContext.cs
  - Accion aplicada: agregado diagnóstico en catch vacíos mediante Debug.WriteLine en compilaciones DEBUG, sin alterar flujo funcional.

- [x] Revisar y retirar clase legacy no referenciada SingleInstanceManager (namespace Infrastructure).
  - Archivo sospechoso: VirtualWebDisplay_Parsec/Infrastructure/SingleInstanceManager.cs
  - Accion aplicada: confirmado no uso en código activo y eliminada clase legacy.

- [ ] Reducir complejidad ciclomática de InputHandler.
  - Archivo: VirtualWebDisplay_Parsec/Web/Handlers/InputHandler.cs
  - Accion propuesta: separar en componentes por responsabilidad (routing de acciones, mapeo de coordenadas, estado de gestos, telemetria).

- [x] Completar/limpiar ApplicationLifecycleManager.
  - Archivo: VirtualWebDisplay_Parsec/Infrastructure/Hosting/ApplicationLifecycleManager.cs
  - Accion aplicada: eliminado comentario XML truncado y validada compilación.

## Arquitectura y calidad (mediano plazo)

- [~] Introducir DI para handlers y quitar estado static mutable en capa Web.
  - Archivos: VirtualWebDisplay_Parsec/Web/Handlers/*.cs, VirtualWebDisplay_Parsec/Web/Api/WebApiEndpoints.cs, VirtualWebDisplay_Parsec/Web/Services/WebEndpointServices.cs
  - Avance aplicado: extraídos IAuthService/IConfigService/IKeepaliveService/ICaptureService/IWebRtcOfferService y usados desde DefaultWebEndpointOrchestrator; registrados en DI y mapeados desde WebApiEndpoints.
  - Avance aplicado: migrado InputHandler a IInputService inyectable y registrado en DI; WebApiEndpoints ahora usa servicio de input en lugar de llamar directamente al handler estático.

- [~] Definir pruebas de integracion para endpoints criticos.
  - Cobertura actual: 1 archivo de pruebas, 4 tests, enfocados solo en copia de configuracion.
  - Avance aplicado: agregada bateria de tests de comportamiento para InputHandler, AuthHandler, WebRtcHandler, ConfigHandler, KeepaliveHandler y CaptureHandler.
  - Archivos: VirtualWebDisplay.Tests/Web/Handlers/InputHandlerTests.cs, VirtualWebDisplay.Tests/Web/Handlers/AuthHandlerTests.cs, VirtualWebDisplay.Tests/Web/Handlers/WebRtcHandlerTests.cs, VirtualWebDisplay.Tests/Web/Handlers/ConfigAndKeepaliveHandlerTests.cs, VirtualWebDisplay.Tests/Web/Handlers/CaptureHandlerTests.cs.
  - Avance aplicado (bloque 1): agregadas pruebas de concurrencia para handlers criticos y robustez de ciclo de vida de estado.
  - Archivos: VirtualWebDisplay.Tests/Web/Handlers/HandlerConcurrencyTests.cs, VirtualWebDisplay.Tests/Infrastructure/ServiceStateManagerConcurrencyTests.cs.
  - Avance aplicado (bloque 1): agregadas pruebas end-to-end en memoria sobre host HTTP real (TestServer) para flujo auth/config/keepalive/input.
  - Archivos: VirtualWebDisplay.Tests/Web/Api/WebApiEndpointsIntegrationTests.cs.
  - Avance aplicado (bloque 1): cubierto exito real de captura/streaming para /cap y /mjpeg usando doubles de frame source en tests de handler.
  - Archivos: VirtualWebDisplay.Tests/Web/Handlers/CaptureHandlerTests.cs, VirtualWebDisplay.Tests/Web/Handlers/WebHandlerTestHelper.cs.
  - Avance adicional: prueba multi-cliente con host HTTP en memoria válida en paralelo.
  - Archivos: VirtualWebDisplay.Tests/Web/Api/WebApiEndpointsIntegrationTests.cs.

- [x] Revisar acoplamiento de ScreenRuntimeContext como agregador de dependencias concretas.
  - Archivo: VirtualWebDisplay_Parsec/Infrastructure/Runtime/ScreenRuntimeContext.cs
  - Accion aplicada: introducidas interfaces publicas para captura/encoder/stream, factoría interna por defecto y test de composición con factoría falsa.

## Validaciones recomendadas al cerrar cambios

- [ ] dotnet build .\VirtualWebDisplay_Parsec.slnx -v minimal
- [ ] dotnet test .\VirtualWebDisplay.Tests\VirtualWebDisplay.Tests.csproj -v minimal
- [ ] Prueba manual multi-pantalla con input concurrente (dos clientes simultaneos)
- [ ] Prueba de parada/inicio de servicio desde tray sin congelamientos
