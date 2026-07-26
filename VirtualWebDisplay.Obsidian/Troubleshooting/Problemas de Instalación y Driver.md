---
tags: [troubleshooting, driver, vdd, instalacion]
aliases: [Problemas de Instalación y Driver, VDD Install Issues, Parsec Driver]
type: guia
updated: 2026-07-08
---

# Problemas de Instalación y Driver

## Síntomas

- "No VDD device found" al iniciar.
- Pantalla virtual no aparece en `Display Settings`.
- `VirtualDisplayManager` falla al attach.

## Causas y soluciones

### Driver Parsec VDD no instalado

- Instalar **Parsec Virtual Display Driver** desde parsec.app.
- Verificar en `Device Manager` → `Monitors` → debe aparecer "Parsec VDD".
- Ver [[IDriverVerifier (Abstracción)]] y [[VirtualDisplayManager]].

### DeviceId no assignado

- [[VirtualDisplayManager]] busca dispositivos VDD libres con `FindFreeVddDevice`.
- Si todos están en uso → error. Máximo 2 pantallas.

### P/Invoke unsafe falla

- `setupapi.dll` / `user32.dll` no encontradas → ejecutar en Windows x64.
- `AllowUnsafeBlocks=true` requerido (ver [[Build y Compilación]]).

### Resolución no soportada

- Usar [[Perfiles de Resolución]] predefinidos.
- Para custom: [[Resoluciones Personalizadas VDD]].
- `VirtualResolutionWatcher` detecta cambios hardware.

## Enlaces

- [[Creación de Pantalla Virtual]]
- [[VirtualDisplayManager]]
- [[IDriverVerifier (Abstracción)]]
- [[Guía de Troubleshooting]]

## Continuar con
- [[VirtualDisplayManager]]
- [[IDriverVerifier (Abstracción)]]
