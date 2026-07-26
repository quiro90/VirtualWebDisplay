---
tags: [arquitectura, estado, threading]
aliases: [ServiceStateManager, Estado del servicio]
type: componente
updated: 2026-07-08
---

# ServiceStateManager ⭐

**Namespace**: `VirtualWebDisplay.Infrastructure.Runtime`
**Archivo**: `Infrastructure/Runtime/ServiceStateManager.cs`

> [!important] Single Source of Truth
> Única fuente de verdad para el estado del servicio. Thread-safe y reactivo vía eventos.

## Máquina de estados

```
Stopped → Starting → Started → Stopping → Stopped
    ↑                                         ↓
    └─────────────────────────────────────────┘
```

Estados: `Stopped`, `Starting`, `Started`, `Stopping`. Solo se permiten **transiciones válidas**.

## Métodos públicos

```csharp
void RequestStart();                                    // Stopped → Starting
void RequestStop();                                     // Started → Stopping
void CompleteStart(IReadOnlyList<ScreenRuntimeContext>);// → Started
void CompleteStop();                                    // cualquier estado → Stopped
Task<bool> WaitForStartRequestAsync();                  // espera señal de reinicio
void SignalStartRequest();                              // señala reinicio deseado
void SignalNoRestart();                                 // señala salida
```

## Propiedades

- `CurrentState` (thread-safe) · `ScreenRuntimes` · `IsStarted` · `IsStopped` · `IsTransitioning`

## Eventos reactivos

- `StateChanged` · `ServiceStarted` · `ServiceStopped`

El [[VirtualDisplayTrayController]] se suscribe a estos eventos para actualizar UI/tray. La UI nunca gestiona el estado con booleanos sueltos; delega aquí.

## Patrones

- **State Machine** + **Observer** (eventos) + thread-safe (lock pattern `_stateLock`).

## Tests

- `VirtualWebDisplay.Tests/Infrastructure/ServiceStateManagerConcurrencyTests.cs` — ver [[Testing]].

## Enlaces

- [[ApplicationLifecycleManager]]
- [[VirtualDisplayTrayController]]
- [[ScreenRuntimeContext]]

## Continuar con
- [[ApplicationLifecycleManager]]
- [[ScreenRuntimeContext]]
