---
tags: [arquitectura, drivers, di, abstraccion]
aliases: [IDriverVerifier, Driver Verifier, ParsecVddDriverVerifier, ParsecVddDriverApi]
type: componente
updated: 2026-07-08
---

# IDriverVerifier (Abstracción)

**Archivos**:
- `Infrastructure/Drivers/IDriverVerifier.cs`
- `Infrastructure/Drivers/ParsecVddDriverVerifier.cs`
- `Parsec/ParsecVddDriverApi.cs`

## Propósito

Abstracción para **verificar disponibilidad de drivers de display virtual**. Desacopla la verificación de la implementación concreta y abre la puerta a multi-plataforma (futuro Linux/macOS).

## Interfaz

```csharp
public interface IDriverVerifier
{
    (bool isAvailable, string statusMessage) Verify();
    string InstallUrl { get; }
    string DriverName { get; }
}
```

## Implementación

- `ParsecVddDriverVerifier` → Parsec VDD (Windows). URL de descarga embebida: `https://builds.parsec.app/vdd/parsec-vdd-0.45.0.0.exe`.

## Cadena de DI

```
ApplicationBootstrapper
  └─> new ParsecVddDriverVerifier()
      └─> RuntimeFactory.GetEnabledPorts(driverVerifier)
          └─> RuntimeFactory.TryCreate(..., driverVerifier)
              └─> ScreenRuntimeContext(..., driverVerifier)
                  └─> VirtualDisplayManager(driverVerifier)
```

## ParsecVddDriverApi (P/Invoke compartida)

> [!danger] Código unsafe
> API de bajo nivel P/Invoke (setupapi.dll, kernel32.dll). **Modificar con extremo cuidado.** Compartida entre `VirtualDisplayManager` y `ParsecVddDriverVerifier`.

Métodos clave: `OpenHandle`, `CloseHandle`, `AddDisplay`, `RemoveDisplay`, `Update` (keep-alive), `IsValidHandle`.

## Enlaces

- [[VirtualDisplayManager]]
- [[ApplicationLifecycleManager]]
- [[ScreenRuntimeContext]]

## Continuar con
- [[VirtualDisplayManager]]
- [[ApplicationBootstrapper]]
