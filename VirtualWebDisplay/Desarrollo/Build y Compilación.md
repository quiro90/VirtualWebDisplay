---
tags: [desarrollo, build, compilacion]
aliases: [Build y Compilación, Build, Compile]
type: guia
updated: 2026-07-08
---

# Build y Compilación

## Build debug

```powershell
dotnet build VirtualWebDisplay_Parsec.slnx
```

## Publish (AOT)

```powershell
dotnet publish VirtualWebDisplay_Parsec/VirtualWebDisplay.csproj -c Release -r win-x64
```

El proyecto tiene `PublishAot=true` → ver [[Native AOT]].

## Configuraciones del `.csproj`

- `TargetFramework`: `net10.0-windows`
- `AllowUnsafeBlocks`: `true` (P/Invoke VDD)
- `UseWindowsForms`: `true`
- `PublishAot`: `true`
- `Version`: `1.0.5`

## Dependencias clave

Ver [[Stack Tecnológico]] y [[02 - Stack Tecnológico]].

## Enlaces

- [[Native AOT]]
- [[Guía de Desarrollo]]
- [[02 - Stack Tecnológico]]