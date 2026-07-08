---
tags: [config, parsec, vdd, registry, uac]
aliases: [Resoluciones Personalizadas, VddCustomModesStore, Custom Modes, UAC]
type: referencia
updated: 2026-07-08
---

# Resoluciones Personalizadas VDD

El driver Parsec VDD soporta hasta **5 slots** de resoluciones personalizadas en el registro de Windows.

## Registro

- Ruta: `HKLM\SOFTWARE\Parsec\vdd\{0..4}`
- Valores por slot: `width` (DWORD), `height` (DWORD), `hz` (DWORD)
- **Requiere permisos de Administrador para escribir.**

## Componentes

- `Parsec/VddCustomModesStore.cs` — lectura/escritura de los slots.
- `UI/Forms/CustomModesDialog.cs` — diálogo con 5 slots editables (W×H@Hz), incluye flujo **UAC automático**.
- `Program.cs` — argumento CLI `--set-custom-modes "<w>x<h>@<hz>;..."` para el flujo UAC.

## Flujo UAC

Cuando el usuario guarda desde `CustomModesDialog` sin permisos de admin, se **relanza el proceso** con:

```
VirtualWebDisplay.exe --set-custom-modes "1920x1080@60;1280x720@60;..."
```

El proceso elevado escribe al registro y sale. El proceso original detecta el éxito y cierra el diálogo.

## Aplicación

> [!warning]
> Los cambios se aplican al **reiniciar el driver Parsec VDD**. Slot vacío (todos en 0) = ignorado.

## Enlaces

- [[VirtualDisplayManager]]
- [[Perfiles de Resolución]]
- [[Program (Entry Point)]]