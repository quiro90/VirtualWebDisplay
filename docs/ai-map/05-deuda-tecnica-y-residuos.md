# Deuda técnica y residuos

## Objetivo de este análisis
Evaluar qué partes del workspace agregan ruido o coste de mantenimiento sin aportar al objetivo principal del proyecto:

**extender pantallas virtuales de Windows hacia dispositivos secundarios mediante acceso web**.

## Limpiezas aplicadas (historial)

### 1. Residuos de plantilla eliminados
Se eliminaron archivos que no formaban parte del dominio real:
- `VirtualWebDisplay/WeatherForecast.cs`
- `VirtualWebDisplay/Controllers/WeatherForecastController.cs`
- `VirtualWebDisplay/VirtualWebDisplay.http`

### 2. Centralización de defaults
`VirtualScreenSettingsStore` usa `CreateDefaults()` para centralizar valores por defecto.

### 3. Centralización de placement
Normalización y etiquetado de posición centralizado en `VirtualDisplayPlacementOptions.cs`.

### 4. Centralización de red
Construcción de URLs y detección de IP centralizada en `NetworkAddressHelper.cs`.

### 5. Corrección de `--vh: 85vh`
La página `BuildWebImagePage` tenía `--vh: 85vh` hardcodeado. Corregido a `100vh` para que la imagen ocupe el 100% de la pantalla del cliente.

### 6. Control de `BrowserImageFit` en UI
El campo `BrowserImageFit` ya existía en `VirtualScreenConfig` pero no tenía control en el formulario. Se agregó un combo en `ScreenTabControls` con tres opciones: Estirar (fill) / Recortar (cover) / Contener (contain). Se inicializa y guarda igual que los demás campos.

---

## Deuda técnica vigente, ordenada por prioridad

## Alta prioridad

### A. HTML cliente embebido en `Program.cs`
Las páginas de `WebImage` y `Rtc` están definidas como strings interpolados grandes en el entry point.

#### Riesgo
- `Program.cs` mezcla bootstrapping, servidor y frontend,
- hace más difícil evolucionar la UI web independientemente,
- complica testing o reutilización de las páginas.

#### Limpieza futura sugerida
Mover las plantillas a una clase generadora dedicada o archivos estáticos embebidos.

---

## Prioridad media

### B. Duplicación en copia de config
Hay tres métodos de copia en `VirtualDisplayTrayController`:
- `CopyConfig(source, target)`
- `CloneConfig(source)`
- `CloneSettings(settings)`

#### Riesgo
Si se agrega una propiedad nueva a `VirtualScreenConfig`, es fácil olvidar actualizar todas las copias.

#### Limpieza futura sugerida
Agregar un método de copia centralizado en el modelo o un mapper dedicado.

---

### C. Servicios acoplados al modelo mutable
`CaptureService` y `WebRtcStreamService` leen directamente `VirtualScreenConfig` mutable.

#### Riesgo
- efectos laterales si el objeto cambia en runtime,
- menos claridad entre configuración persistida y configuración aplicada.

#### Limpieza futura sugerida
Separar configuración editable de snapshot de runtime aplicado.

---

### D. Mezcla de idioma técnico y de negocio
El código combina nombres y mensajes en inglés y español.

#### Impacto
No rompe funcionalidad, pero aumenta fricción documental y consistencia interna.

---

## Baja prioridad

### E. `Program.cs` concentra demasiadas responsabilidades
Actualmente actúa como:
- bootstrapper,
- compositor de runtimes,
- fábrica de páginas HTML,
- definición de endpoints,
- control de errores de arranque.

#### Limpieza futura sugerida
Separar en piezas pequeñas: startup/runtime bootstrap, route mapping, page rendering.

---

### F. Uso intensivo de sleeps y sondeo
Hay varios `Thread.Sleep` y polling ligero para detectar el monitor virtual o esperar frames.

#### Nota
No necesariamente está mal para este tipo de integración con Windows/driver, pero es una zona sensible si aparecen problemas de timing.

---

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

## Próximas limpiezas de mejor relación beneficio/riesgo
1. separar HTML cliente fuera de `Program.cs`,
2. centralizar copia/clonado de `VirtualScreenConfig`,
3. evaluar separación entre settings editables y snapshot de runtime.
