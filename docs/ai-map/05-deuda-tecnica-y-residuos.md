# Deuda técnica y residuos

## Objetivo de este análisis
Evaluar qué partes del workspace agregan ruido o coste de mantenimiento sin aportar al objetivo principal del proyecto:

**extender pantallas virtuales de Windows hacia dispositivos secundarios mediante acceso web**.

## Limpieza aplicada ahora

### 1. Residuos de plantilla eliminados
Se eliminaron archivos que no formaban parte del dominio real de la aplicación:
- `VirtualWebDisplay/WeatherForecast.cs`
- `VirtualWebDisplay/Controllers/WeatherForecastController.cs`
- `VirtualWebDisplay/VirtualWebDisplay.http`

### Por qué eran residuos
- pertenecían a la plantilla base de `ASP.NET Core`,
- no participaban en la creación de pantallas virtuales,
- no intervenían en captura, streaming, configuración ni tray,
- podían confundir a otra IA o a un desarrollador nuevo sobre el foco real del proyecto.

### 2. Deuda técnica menor reducida
En `VirtualScreenSettingsStore.cs` se centralizó la creación de settings por defecto mediante `CreateDefaults()`.

### Beneficio
- menos repetición en manejo de errores,
- menor probabilidad de inconsistencias futuras,
- lectura más clara del flujo de carga.

## Deuda técnica vigente, ordenada por prioridad

## Alta prioridad

### A. Duplicación de reglas de placement
Estado actual: **resuelto**.

La normalización y etiquetado de posición se centralizó en `VirtualDisplayPlacementOptions.cs`.

---

### B. Utilidades de red duplicadas
Estado actual: **resuelto**.

La construcción de URLs y la detección de IP se centralizaron en `NetworkAddressHelper.cs`.

---

### C. HTML cliente embebido en `Program.cs`
Las páginas de `WebImage` y `Rtc` están definidas como strings grandes en el entry point.

#### Riesgo
- `Program.cs` mezcla composición, servidor y frontend,
- hace más difícil evolucionar la UI web,
- complica testing o reutilización de las páginas.

#### Limpieza futura sugerida
Mover las plantillas a:
- archivos estáticos, o
- una pequeña clase generadora dedicada.

## Prioridad media

### D. Repetición de clonación/copias de config
Hay lógica de copia entre configs en `VirtualDisplayTrayController`:
- `CopyConfig(...)`
- `CloneConfig(...)`
- `CloneSettings(...)`

#### Riesgo
Si se agrega una propiedad nueva a `VirtualScreenConfig`, es fácil olvidar actualizar todas las copias.

#### Limpieza futura sugerida
Agregar un método de copia centralizado en el modelo o un mapper dedicado.

---

### E. Servicios muy acoplados al modelo mutable
`CaptureService` y `WebRtcStreamService` leen directamente `VirtualScreenConfig` mutable.

#### Riesgo
- efectos laterales si el objeto cambia en runtime,
- menos claridad entre configuración persistida y configuración aplicada.

#### Limpieza futura sugerida
Separar:
- configuración editable,
- snapshot de runtime aplicado.

---

### F. Mezcla de idioma técnico y de negocio
El código combina nombres y mensajes en inglés y español.

#### Impacto
No rompe funcionalidad, pero aumenta fricción documental y consistencia interna.

## Baja prioridad

### G. `Program.cs` concentra demasiadas responsabilidades
Actualmente actúa como:
- bootstrapper,
- compositor de runtimes,
- fábrica de páginas HTML,
- definición de endpoints,
- control de errores de arranque.

#### Limpieza futura sugerida
Separar en piezas pequeñas:
- startup/runtime bootstrap,
- route mapping,
- page rendering.

---

### H. Uso intensivo de sleeps y sondeo
Hay varios `Thread.Sleep` y polling ligero para detectar el monitor virtual o esperar frames.

#### Nota
No necesariamente está mal para este tipo de integración con Windows/driver, pero es una zona sensible si aparecen problemas de timing.

## Criterio para limpiar sin romper el producto
En este proyecto conviene priorizar limpiezas que:
1. reduzcan ruido de plantilla,
2. centralicen reglas repetidas,
3. no alteren el flujo de creación/captura/transmisión de la pantalla virtual.

Conviene evitar refactors grandes que puedan afectar:
- detección del monitor virtual,
- negociación WebRTC,
- persistencia de settings,
- compatibilidad con perfiles tipo Kindle/iPad.

## Recomendación inmediata siguiente
La siguiente limpieza de mejor relación beneficio/riesgo sería:
1. separar HTML cliente fuera de `Program.cs`,
2. centralizar copia/clonado de `VirtualScreenConfig`,
3. evaluar separación entre settings editables y snapshot de runtime.
