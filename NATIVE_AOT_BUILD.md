# Compilación con Native AOT

Este proyecto ahora está configurado para compilarse de forma nativa sin depender del runtime de C#.

## ¿Qué es Native AOT?

Native AOT (Ahead-of-Time) compila la aplicación directamente a código nativo, eliminando la necesidad de tener .NET Runtime instalado en el sistema destino. Esto resulta en:

- **Sin dependencia del runtime**: No requiere .NET instalado
- **Inicio más rápido**: No hay compilación JIT en tiempo de ejecución
- **Menor uso de memoria**: Footprint reducido
- **Ejecutable standalone**: Todo compilado en un único binario nativo

## Configuración Aplicada

El archivo `VirtualWebDisplay.csproj` ahora incluye:

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

### Explicación de las opciones:

- `PublishAot=true`: Habilita la compilación AOT nativa
- `TrimMode=partial`: Usa trimming parcial (compatible con Windows Forms)
- `IlcOptimizationPreference=Speed`: Optimiza para velocidad en lugar de tamaño
- `IlcGenerateStackTraceData=false`: Reduce tamaño eliminando datos de stack trace
- `_SuppressWinFormsTrimError=true`: Permite usar Windows Forms con AOT

## Cómo Publicar

### Opción 1: Desde Visual Studio

1. Clic derecho en el proyecto → **Publicar**
2. Seleccionar el perfil de publicación o crear uno nuevo
3. Configurar:
   - **Target Runtime**: `win-x64` (o `win-arm64` según tu arquitectura)
   - **Deployment Mode**: Self-contained
4. Publicar

### Opción 2: Desde línea de comandos

```powershell
# Para Windows x64
dotnet publish VirtualWebDisplay_Parsec\VirtualWebDisplay.csproj -c Release -r win-x64

# Para Windows ARM64
dotnet publish VirtualWebDisplay_Parsec\VirtualWebDisplay.csproj -c Release -r win-arm64
```

La aplicación compilada estará en:
```
VirtualWebDisplay_Parsec\bin\Release\net10.0-windows\win-x64\publish\
```

### Opción 3: Publicación optimizada (recomendada)

Para obtener el mejor rendimiento y menor tamaño:

```powershell
dotnet publish VirtualWebDisplay_Parsec\VirtualWebDisplay.csproj `
  -c Release `
  -r win-x64 `
  -p:PublishSingleFile=false `
  -p:PublishTrimmed=true `
  -p:StripSymbols=true
```

## Consideraciones Importantes

### ✅ Ventajas

- **Sin instalación de .NET**: El ejecutable funciona sin .NET instalado
- **Rendimiento mejorado**: Inicio más rápido y mejor uso de memoria
- **Distribución simplificada**: Un único ejecutable nativo

### ⚠️ Advertencias

1. **Windows Forms y AOT**: Aunque la configuración permite compilar con Windows Forms, algunas características dinámicas podrían no funcionar. Se usa `TrimMode=partial` para minimizar problemas.

2. **Reflection**: Si el código usa reflection de forma dinámica, podría necesitar configuración adicional en un archivo `rd.xml` o atributos `[DynamicDependency]`.

3. **Tamaño del ejecutable**: El binario nativo será más grande que el ejecutable normal porque incluye todo el runtime.

4. **Tiempo de compilación**: La compilación AOT toma más tiempo que la compilación estándar.

## Verificación de Compatibilidad

Para identificar posibles problemas de compatibilidad con AOT, ejecuta:

```powershell
dotnet publish VirtualWebDisplay_Parsec\VirtualWebDisplay.csproj -c Release -r win-x64 /p:PublishAot=true
```

Revisa las advertencias que aparezcan durante la compilación.

## Troubleshooting

### Error: Funcionalidad de Windows Forms no disponible

Si encuentras problemas con controles de Windows Forms en runtime, considera:

1. Verificar que no se use reflection dinámica en componentes UI
2. Añadir atributos `[DynamicDependency]` donde sea necesario
3. Crear un archivo `rd.xml` para preservar tipos específicos

### Error: Método no encontrado en runtime

Esto generalmente indica que el trimming eliminó código necesario. Soluciones:

1. Marcar el código con `[DynamicallyAccessedMembers]`
2. Añadir el tipo al archivo `rd.xml`
3. Usar `TrimMode=partial` (ya configurado)

## Referencias

- [Documentación oficial de Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
- [Native AOT y Windows Forms](https://learn.microsoft.com/dotnet/core/deploying/native-aot/incompatibilities)
