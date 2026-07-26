---
tags: [desarrollo, aot, nativo]
aliases: [Native AOT, AOT, Ahead-of-Time]
type: guia
updated: 2026-07-08
---

# Native AOT

El proyecto compila con **PublishAot=true** → binario nativo Ahead-of-Time, sin depender del runtime de C# instalado en el sistema destino.

## Ventajas

- **Sin dependencia del runtime**: no requiere .NET instalado.
- **Inicio más rápido**: sin compilación JIT en runtime.
- **Menor uso de memoria**: footprint reducido.
- **Ejecutable standalone**: un único binario nativo.

## Restricciones

- **No reflexión general** (usar `[JsonSerializable]` para Source Generators).
- **No cargar ensamblados en runtime**.
- **No dynamic IL emit**.
- Requiere `win-x64` (o `win-arm64`).

## Configuración aplicada (`VirtualWebDisplay.csproj`)

```xml
<PublishAot>true</PublishAot>
<InvariantGlobalization>false</InvariantGlobalization>
<TrimMode>partial</TrimMode>
<IlcOptimizationPreference>Speed</IlcOptimizationPreference>
<IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>
<EnableTrimAnalyzer>false</EnableTrimAnalyzer>
<EnableSingleFileAnalyzer>false</EnableSingleFileAnalyzer>
<SuppressTrimAnalysisWarnings>true</SuppressTrimAnalysisWarnings>
<_SuppressWinFormsTrimError>true</_SuppressWinFormsTrimError>
```

### Explicación de las opciones

- `PublishAot=true` — habilita compilación AOT nativa.
- `TrimMode=partial` — trimming parcial (compatible con Windows Forms).
- `IlcOptimizationPreference=Speed` — optimiza para velocidad, no tamaño.
- `IlcGenerateStackTraceData=false` — reduce tamaño eliminando datos de stack trace.
- `_SuppressWinFormsTrimError=true` — permite usar Windows Forms con AOT.

## Cómo publicar

### Opción 1: Visual Studio

1. Clic derecho en el proyecto → **Publicar**.
2. **Target Runtime**: `win-x64` (o `win-arm64`).
3. **Deployment Mode**: Self-contained.
4. Publicar.

### Opción 2: CLI

```powershell
# Windows x64
dotnet publish VirtualWebDisplay_Parsec\VirtualWebDisplay.csproj -c Release -r win-x64

# Windows ARM64
dotnet publish VirtualWebDisplay_Parsec\VirtualWebDisplay.csproj -c Release -r win-arm64
```

Salida:
```
VirtualWebDisplay_Parsec\bin\Release\net10.0-windows\win-x64\publish\
```

### Opción 3: Publicación optimizada (recomendada)

```powershell
dotnet publish VirtualWebDisplay_Parsec\VirtualWebDisplay.csproj `
  -c Release `
  -r win-x64 `
  -p:PublishSingleFile=false `
  -p:PublishTrimmed=true `
  -p:StripSymbols=true
```

## Advertencias

> [!warning] Windows Forms y AOT
> Aunque la configuración permite compilar con Windows Forms, algunas características dinámicas podrían no funcionar. Se usa `TrimMode=partial` para minimizar problemas.

1. **Reflection dinámica**: puede necesitar `rd.xml` o `[DynamicDependency]`.
2. **Tamaño del ejecutable**: mayor que el normal (incluye el runtime).
3. **Tiempo de compilación**: AOT toma más tiempo que el build estándar.

## Verificación de compatibilidad

```powershell
dotnet publish VirtualWebDisplay_Parsec\VirtualWebDisplay.csproj -c Release -r win-x64 /p:PublishAot=true
```

Revisar las advertencias que aparezcan durante la compilación.

## Troubleshooting AOT

### Windows Forms no disponible en runtime

1. Verificar que no se use reflection dinámica en componentes UI.
2. Añadir `[DynamicDependency]` donde sea necesario.
3. Crear un archivo `rd.xml` para preservar tipos específicos.

### Método no encontrado en runtime (trimming eliminó código)

1. Marcar el código con `[DynamicallyAccessedMembers]`.
2. Añadir el tipo al archivo `rd.xml`.
3. Usar `TrimMode=partial` (ya configurado).

## Referencias

- [Documentación oficial Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
- [Native AOT y Windows Forms](https://learn.microsoft.com/dotnet/core/deploying/native-aot/incompatibilities)

## Enlaces

- [[Build y Compilación]]
- [[02 - Stack Tecnológico]]
- [[Guía de Desarrollo]]

## Continuar con
- [[Build y Compilación]]
- [[Convenciones de Código]]
