---
tags: [arquitectura, web, kestrel, hosting, puertos]
aliases: [KestrelConfigurator, Configuración Kestrel, HTTP HTTPS Ports]
type: referencia
updated: 2026-07-26
---

# KestrelConfigurator

**Archivo**: `Web/Hosting/KestrelConfigurator.cs` · `internal static class`.

Configura los puertos HTTP y HTTPS en Kestrel para cada runtime activo.

## Regla clave

> [!important] HTTPS = Port + 1
> El puerto HTTPS es siempre `Config.Port + 1`. Por cada runtime se registra un par `(HTTP: port, HTTPS: port+1)`.

## API

```csharp
KestrelConfigurator.Configure(builder, runtimes, tlsCert);
KestrelConfigurator.Configure(builder, ports, tlsCert);
```

- `ListenAnyIP(port)` → HTTP.
- `ListenAnyIP(port + 1, UseHttps(tlsCert))` → HTTPS.

## Uso

Llamado por [[ApplicationLifecycleManager]] (paso 3 del arranque): `KestrelConfigurator.Configure(builder, ports, tlsCert)` asigna HTTP/HTTPS.

## Relacionados

- [[ApplicationLifecycleManager]] — quien lo invoca.
- [[Certificado SSL (HTTPS)]] — el `tlsCert` (`localca.pfx`).
- [[Resolución de Runtime por Puerto]] — resolución de runtime por `LocalPort`.
- [[Endpoints HTTP]] — endpoints servidos sobre estos puertos.

## Continuar con
- [[Endpoints HTTP]]
- [[Resolución de Runtime por Puerto]]
