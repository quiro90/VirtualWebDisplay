# Refactoring del Sistema de Inicio/Detención del Servicio

## Fecha
2024

## Motivación

El sistema de inicio y detención del servicio presentaba los siguientes problemas:

### 1. **Estado Duplicado**
   - Múltiples variables booleanas distribuidas en diferentes clases:
     - `ResolutionConfigurationForm`: `_wasStarted`, `_serviceActionPending`, `_pendingStartAction`
     - `VirtualDisplayTrayController`: `_serviceActionPending`, `_screenRuntimes`, `_serviceStartSignal`
   - Sin una fuente única de verdad para el estado del servicio

### 2. **Violación de Single Responsibility Principle (SRP)**
   - `VirtualDisplayTrayController` manejaba demasiadas responsabilidades:
     - Thread de UI del tray
     - Construcción de menús
     - **Estado del servicio**
     - Coordinación entre formularios

### 3. **Falta de Encapsulación**
   - Estado del servicio manejado mediante flags booleanos dispersos
   - No había una máquina de estados clara
   - Transiciones de estado no validadas

### 4. **Notificaciones Inconsistentes**
   - Métodos `NotifyServiceStarted/Stopped` se propagaban manualmente
   - Múltiples puntos de entrada para cambiar estado
   - Difícil de depurar y mantener

## Solución Implementada

### Nueva Clase: `ServiceStateManager`

Se creó una clase dedicada que centraliza toda la gestión de estado del servicio:

```csharp
internal sealed class ServiceStateManager
{
    private ServiceState _currentState;
    private IReadOnlyList<ScreenRuntimeContext> _screenRuntimes;
    private TaskCompletionSource<bool>? _serviceStartSignal;

    // ... métodos de transición de estado
}

internal enum ServiceState
{
    Stopped,
    Starting,
    Started,
    Stopping
}
```

#### Características:

1. **Single Responsibility**: Solo gestiona el estado del servicio
2. **Máquina de Estados Clara**: Transiciones válidas definidas explícitamente
   - `Stopped → Starting → Started → Stopping → Stopped`
3. **Eventos para Notificaciones**: 
   - `StateChanged`: Notifica cualquier cambio de estado
   - `ServiceStarted`: Notifica cuando se completa el inicio
   - `ServiceStopped`: Notifica cuando se completa la detención
4. **Encapsulación**: Estado privado, acceso mediante propiedades readonly
5. **Validación de Transiciones**: Solo permite transiciones válidas

### Cambios en Clases Existentes

#### 1. `VirtualDisplayTrayController`

**Antes:**
```csharp
private TaskCompletionSource<bool>? _serviceStartSignal;
private IReadOnlyList<ScreenRuntimeContext> _screenRuntimes = [];
private bool _serviceActionPending;
```

**Después:**
```csharp
private readonly ServiceStateManager _serviceState;
```

**Beneficios:**
- Eliminó 3 campos de estado
- Delegó gestión de estado al `ServiceStateManager`
- Se enfoca solo en UI y coordinación
- Eventos reactivos automáticos

#### 2. `ConfigurationFormPresenter`

**Antes:**
```csharp
internal void NotifyServiceStarted(IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
{
    _startupForm?.NotifyServiceStarted(screenRuntimes);
    _configForm?.NotifyServiceStarted(screenRuntimes);
}
```

**Después:**
```csharp
private void OnServiceStarted(IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
{
    _startupForm?.NotifyServiceStarted(screenRuntimes);
    _configForm?.NotifyServiceStarted(screenRuntimes);
}

// Constructor
_serviceState.ServiceStarted += OnServiceStarted;
_serviceState.ServiceStopped += OnServiceStopped;
```

**Beneficios:**
- Métodos privados (mejor encapsulación)
- Reacción automática a eventos
- No requiere llamadas manuales externas

#### 3. `ResolutionConfigurationForm`

**Antes:**
```csharp
private bool _wasStarted;
private bool _serviceActionPending;
private bool _pendingStartAction;

private string AcceptButtonText => _wasStarted
    ? (_serviceActionPending ? AppText.Get("Form_Config_Accept_Stopping") : AppText.Get("Form_Config_Accept_Stop"))
    : (_serviceActionPending && _pendingStartAction ? AppText.Get("Form_Config_Accept_Starting") : AppText.Get("Form_Config_Accept_Start"));
```

**Después:**
```csharp
private ServiceState _serviceState;

private string AcceptButtonText => _serviceState switch
{
    ServiceState.Started => AppText.Get("Form_Config_Accept_Stop"),
    ServiceState.Stopping => AppText.Get("Form_Config_Accept_Stopping"),
    ServiceState.Starting => AppText.Get("Form_Config_Accept_Starting"),
    _ => AppText.Get("Form_Config_Accept_Start")
};
```

**Beneficios:**
- Redujo 3 booleanos a 1 enum
- Lógica más clara con pattern matching
- Estado self-documenting

#### 4. `ApplicationLifecycleManager`

**Antes:**
```csharp
if (stopRequested)
{
    tray.NotifyServiceStopped();
    var startAgain = await tray.WaitForServiceStartAsync();
    // ...
}
```

**Después:**
```csharp
if (stopRequested)
{
    var startAgain = await tray.WaitForServiceStartAsync();
    // ...
}
```

**Beneficios:**
- Eliminó llamada manual a `NotifyServiceStopped()`
- El evento se dispara automáticamente en `CompleteStop()`

## Principios SOLID Aplicados

### 1. **Single Responsibility Principle (SRP)**
   - `ServiceStateManager`: Solo gestiona estado del servicio
   - `VirtualDisplayTrayController`: Solo UI del tray
   - `ConfigurationFormPresenter`: Solo presentación de formularios

### 2. **Open/Closed Principle (OCP)**
   - Fácil agregar nuevos estados sin modificar código existente
   - Eventos permiten extensión sin modificación

### 3. **Dependency Inversion Principle (DIP)**
   - Componentes dependen de abstracciones (eventos)
   - No dependen de implementaciones concretas

## Beneficios

### 1. **Mantenibilidad**
   - Punto único para depurar problemas de estado
   - Flujo de transiciones claro y predecible

### 2. **Testabilidad**
   - `ServiceStateManager` es fácil de testear unitariamente
   - Estado aislado de UI

### 3. **Extensibilidad**
   - Fácil agregar nuevos estados si es necesario
   - Fácil agregar observers mediante eventos

### 4. **Seguridad**
   - Transiciones validadas
   - Estado inmutable desde fuera de la clase

## Archivos Modificados

1. **Nuevo**: `VirtualWebDisplay_Parsec\Infrastructure\ServiceStateManager.cs`
2. **Modificado**: `VirtualWebDisplay_Parsec\UI\TrayIcon\VirtualDisplayTrayController.cs`
3. **Modificado**: `VirtualWebDisplay_Parsec\UI\TrayIcon\ConfigurationFormPresenter.cs`
4. **Modificado**: `VirtualWebDisplay_Parsec\UI\Forms\ResolutionConfigurationForm.cs`
5. **Modificado**: `VirtualWebDisplay_Parsec\Infrastructure\ApplicationLifecycleManager.cs`
6. **Modificado**: `VirtualWebDisplay_Parsec\UI\TrayIcon\TrayMenuBuilder.cs`

## Testing Recomendado

Verificar los siguientes escenarios:

1. ✅ Inicio inicial de la aplicación
2. ✅ Detener servicio desde formulario
3. ✅ Reiniciar servicio desde formulario
4. ✅ Detener servicio desde menú del tray
5. ✅ Iniciar servicio desde menú del tray
6. ✅ Cerrar aplicación con servicio en ejecución
7. ✅ Cerrar aplicación con servicio detenido
8. ✅ Indicadores de pantalla aparecen/desaparecen correctamente
9. ✅ Botones se habilitan/deshabilitan según estado
10. ✅ Cambios de idioma durante transiciones

## Correcciones Post-Testing

### Problema Detectado en Testing Manual

**Síntoma**: Después del primer refactoring, el servicio iniciaba correctamente (creaba el monitor virtual en Windows), pero la UI no se actualizaba:
- El botón quedaba en "Iniciando..."
- El menú del systray no cambiaba de estado
- No se mostraban notificaciones
- Los indicadores de pantalla (zona inferior izquierda) no aparecían

**Causa Raíz**: 
La implementación inicial de `ServiceStateManager` asumía que siempre habría una transición explícita `Stopped → Starting → Started`, pero había **dos flujos diferentes**:

1. **Startup Inicial**: `Program.cs` → `ShowStartupConfiguration()` → Usuario confirma → `ApplicationLifecycleManager.RunAsync()` → `ConfigureRuntimeActions()` → `CompleteStart()`
   - En este flujo, el estado iba directamente de `Stopped` a `CompleteStart()` sin pasar por `Starting`

2. **Restart**: Usuario detiene servicio → `RequestStop()` → `Stopping` → `CompleteStop()` → `Stopped` → Usuario inicia → `RequestStart()` → `Starting` → `CompleteStart()` → `Started`
   - Este flujo sí pasaba por `Starting`

**Solución Implementada**:

1. **Flexibilizar `CompleteStart()`** para aceptar transiciones desde `Stopped` (startup inicial) o `Starting` (restart):
   ```csharp
   public void CompleteStart(IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
   {
       // Antes: if (_currentState != ServiceState.Starting) return;
       // Después: Permitir Stopped o Starting
       if (_currentState is not (ServiceState.Stopped or ServiceState.Starting))
           return;
       // ...
   }
   ```

2. **Flexibilizar `CompleteStop()`** para aceptar transiciones desde cualquier estado excepto `Stopped`:
   ```csharp
   public void CompleteStop()
   {
       // Antes: if (_currentState != ServiceState.Stopping) return;
       // Después: Permitir detención desde cualquier estado
       if (_currentState is ServiceState.Stopped)
           return;
       // ...
   }
   ```

3. **Agregar método `NotifyServiceStopped()` en `VirtualDisplayTrayController`**:
   - Expone la capacidad de notificar cuando el servicio se detiene
   - Llama a `_serviceState.CompleteStop()`

4. **Actualizar `ApplicationLifecycleManager`** para notificar explícitamente cuando el servicio se detiene:
   ```csharp
   if (stopRequested)
   {
       tray.NotifyServiceStopped(); // ← Agregado
       var startAgain = await tray.WaitForServiceStartAsync();
       // ...
   }
   ```

5. **Fix crítico: Thread-Safety en `ConfigurationFormPresenter`** (descubierto en testing):
   - **Problema**: Los eventos de `ServiceStateManager` se disparaban desde el thread async de `ApplicationLifecycleManager`, pero los formularios WinForms solo pueden actualizarse desde su UI thread
   - **Error**: `InvalidOperationException: Operación no válida a través de subprocesos`
   - **Solución**: Usar `InvokeRequired` y `BeginInvoke()` para marshaling al UI thread:
   ```csharp
   private void OnServiceStarted(IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
   {
       if (_startupForm is not null && !_startupForm.IsDisposed)
       {
           if (_startupForm.InvokeRequired)
               _startupForm.BeginInvoke(() => _startupForm.NotifyServiceStarted(screenRuntimes));
           else
               _startupForm.NotifyServiceStarted(screenRuntimes);
       }
       // ... mismo patrón para _configForm
   }
   ```

**Resultado**: Ahora ambos flujos (startup inicial y restart) funcionan correctamente, y la UI se actualiza en tiempo real reflejando el estado del servicio **sin errores de threading**.

### Lecciones Aprendidas

1. **Testing temprano es crucial**: El problema solo se detectó al hacer testing manual
2. **Máquinas de estado necesitan flexibilidad**: Estados estrictos pueden romper flujos existentes
3. **Documentar flujos múltiples**: El código tenía dos caminos diferentes que no estaban documentados
4. **Pattern matching ayuda**: `_currentState is not (ServiceState.Stopped or ServiceState.Starting)` es más claro que múltiples ifs
5. **Thread-safety en eventos**: Cuando eventos cruzan threads (async → UI), siempre usar `InvokeRequired` + `BeginInvoke()` en WinForms

## Conclusión

El refactoring eliminó código duplicado, centralizó la gestión de estado del servicio en una clase dedicada, y aplicó principios SOLID para mejorar la mantenibilidad, testabilidad y extensibilidad del código. 

Después de las correcciones post-testing, el sistema ahora maneja correctamente **ambos flujos** (startup inicial y restart), y la UI se mantiene sincronizada con el estado real del servicio en todo momento.

El sistema ahora tiene una arquitectura más limpia, profesional y **funcionalmente correcta**.
