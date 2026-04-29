# JavaScript Modules - VirtualWebDisplay

## 📁 Estructura

```
wwwroot/
└── js/
    ├── common/
    │   └── keepalive.js          # Keep-alive signal para mantener sesión activa
    ├── touch/
    │   └── touch-input.js        # Sistema de entrada táctil (gestos, tap, drag, scroll)
    ├── webimage/
    │   └── webimage-client.js    # Cliente JPEG polling (modo WebImage)
    └── webrtc/
        └── webrtc-client.js      # Cliente WebRTC (modo RTC)
```

## 🎯 Propósito

Esta refactorización migró el JavaScript embebido en C# (dentro de `TouchInputScriptHelper.cs`) a archivos `.js` independientes para:

- ✅ **Mejor mantenibilidad**: Editar JS con syntax highlighting completo
- ✅ **Debugging mejorado**: Sourcemaps y herramientas de navegador
- ✅ **Separación de concerns**: HTML templates no contienen lógica JS compleja
- ✅ **Reutilización**: Módulos compartidos entre WebImage y WebRTC
- ✅ **Cache busting**: Versionado con `?v=1.0.0` para invalidar cache

## 📝 Uso en Templates

### WebImagePageTemplate.cs

```csharp
<!-- External JavaScript modules -->
<script src="/js/common/keepalive.js?v=1.0.0"></script>
<script src="/js/webimage/webimage-client.js?v=1.0.0"></script>
<script src="/js/touch/touch-input.js?v=1.0.0"></script>

<!-- Initialization -->
<script>
(function() {
    'use strict';

    // Initialize keep-alive
    if (typeof Keepalive !== 'undefined') {
        Keepalive.start(10000);
    }

    // Initialize WebImage client
    if (typeof WebImageClient !== 'undefined') {
        WebImageClient.init({
            elementId: 'screen',
            intervalMs: 250,
            imageFit: 'cover'
        });
    }

    // Initialize touch input
    if (typeof TouchInput !== 'undefined') {
        TouchInput.init({
            elementId: 'screen',
            throttleMs: 50,
            holdDelayMs: 300
        });
    }
})();
</script>
```

### RtcPageTemplate.cs

Similarmente, carga:
- `keepalive.js`
- `webrtc-client.js` (en vez de webimage-client.js)
- `touch-input.js`

## 🔧 Configuración del Servidor

El middleware de archivos estáticos está habilitado en `ApplicationLifecycleManager.cs`:

```csharp
app.UseStaticFiles();
```

Esto sirve automáticamente archivos desde `/wwwroot/` en la ruta raíz (`/`).

## 🛠️ Herramientas de Desarrollo

### ESLint - Análisis de Calidad de Código

El proyecto usa **ESLint** para mantener calidad y consistencia en el código JavaScript.

**Comandos disponibles**:
```bash
# Analizar código (sin modificar)
npm run lint

# Auto-corregir problemas de estilo
npm run lint:fix
```

**Reglas configuradas**:
- Indentación de 4 espacios
- Comillas simples obligatorias
- Punto y coma obligatorio
- Comparación estricta (`===` en vez de `==`)
- Llaves obligatorias en if/for/while

Ver documentación completa en `/docs/ESLINT_Y_VERSIONADO.md`

### Versionado Dinámico

Los archivos JavaScript usan versionado automático sincronizado con la versión del ensamblado:

```html
<!-- La versión se toma de VirtualWebDisplay.csproj -->
<script src="/js/touch/touch-input.js?v=1.0.0"></script>
```

**Para actualizar la versión**:
1. Editar `VirtualWebDisplay.csproj`
2. Cambiar `<Version>1.0.1</Version>`
3. Compilar
4. ✅ Todos los archivos JS usan la nueva versión automáticamente

## 📚 Módulos Disponibles

### 1. Keepalive (`/js/common/keepalive.js`)

**Namespace**: `window.Keepalive`

**Métodos**:
- `Keepalive.start(intervalMs)` - Inicia pings periódicos
- `Keepalive.stop()` - Detiene el sistema

**Uso**:
```javascript
Keepalive.start(10000); // Ping cada 10 segundos
```

---

### 2. TouchInput (`/js/touch/touch-input.js`)

**Namespace**: `window.TouchInput`

**Métodos**:
- `TouchInput.init(config)` - Inicializa el sistema táctil
- `TouchInput.getStats()` - Obtiene estadísticas de rendimiento

**Configuración**:
```javascript
TouchInput.init({
    elementId: 'screen',       // ID del elemento HTML objetivo
    throttleMs: 50,            // Throttling de eventos (ms)
    holdDelayMs: 300           // Delay para activar hold-to-drag (ms)
});
```

**Gestos soportados**:
- **1 dedo**: tap (click), hold-to-drag (arrastrar)
- **2 dedos**: scroll (desplazamiento)
- **Modo absoluto**: El cursor se posiciona donde se toca

**Compatibilidad legacy**:
- `window.VirtualWebDisplayTouchInput.getStats()` sigue funcionando

---

### 3. WebImageClient (`/js/webimage/webimage-client.js`)

**Namespace**: `window.WebImageClient`

**Métodos**:
- `WebImageClient.init(config)` - Inicializa polling de imágenes
- `WebImageClient.stop()` - Detiene el polling

**Configuración**:
```javascript
WebImageClient.init({
    elementId: 'screen',       // ID del elemento (div con background-image)
    intervalMs: 250,           // Intervalo entre frames (ms)
    imageFit: 'cover'          // 'cover', 'contain', o 'fill'
});
```

**Características**:
- Preload de imágenes (evita parpadeo)
- Retry automático con backoff 4x en errores
- Tracking de viewport para iOS Safari

---

### 4. WebRtcClient (`/js/webrtc/webrtc-client.js`)

**Namespace**: `window.WebRtcClient`

**Métodos**:
- `WebRtcClient.init(config)` - Inicializa conexión WebRTC

**Configuración**:
```javascript
WebRtcClient.init({
    canvasId: 'screen',              // ID del canvas
    statusElementId: 'status',       // ID del elemento de estado (opcional)
    imageFit: 'cover',               // 'cover', 'contain', o 'fill'
    texts: {                         // Textos localizados (opcional)
        connecting: 'Conectando...',
        negotiating: 'Negociando...',
        connected: 'Conectado',
        disconnectedRetrying: 'Desconectado, reintentando...',
        errorRetrying: 'Error, reintentando...',
        negotiationFailed: 'Negociación fallida',
        viewerLimitFull: 'Límite de viewers alcanzado',
        startFailed: 'Inicio fallido'
    }
});
```

**Características**:
- DataChannel con `ordered: false`, `maxRetransmits: 0` (latencia mínima)
- Reensamblado de frames chunkeados (64KB)
- Retry automático con backoff progresivo
- Rendering eficiente con `createImageBitmap` + canvas

---

## 🔄 Migración desde TouchInputScriptHelper

**Antes** (C# embebido):
```csharp
<script>
{{TouchInputScriptHelper.GenerateKeepAliveScript()}}
{{TouchInputScriptHelper.GenerateTouchInputScript("screen", 50, 300)}}
</script>
```

**Después** (archivos externos):
```html
<script src="/js/common/keepalive.js?v=1.0.0"></script>
<script src="/js/touch/touch-input.js?v=1.0.0"></script>
<script>
Keepalive.start(10000);
TouchInput.init({ elementId: 'screen', throttleMs: 50, holdDelayMs: 300 });
</script>
```

## 🧹 Archivos Obsoletos

- **`UI/HtmlTemplates/TouchInputScriptHelper.cs`**: Marcado como `[Obsolete]`. Se mantiene temporalmente para referencia pero ya no se usa en los templates.

## 🚀 Beneficios Obtenidos

1. **Desarrollo más rápido**: Editar `.js` con herramientas estándar (VSCode, linters, formatters)
2. **Debugging simplificado**: Breakpoints en archivos `.js` directamente en DevTools
3. **Cache del navegador**: Los archivos `.js` se cachean (con busting por versión)
4. **Menos recompilaciones**: Cambios en JS no requieren recompilar C#
5. **Código más limpio**: Templates HTML más legibles (menos interpolación compleja)

## 📦 Versionado

Actualizar `AppVersion` en los templates cuando el código JS cambie:

```csharp
private const string AppVersion = "1.0.1"; // Incrementar al modificar JS
```

Esto invalida la cache del navegador automáticamente.

## 🔍 Testing

Para probar los cambios:

1. Compilar el proyecto
2. Iniciar la aplicación
3. Abrir navegador en `http://localhost:<puerto>/` o `https://localhost:<puerto+1>/`
4. Verificar que los archivos JS se carguen correctamente (Network tab en DevTools)
5. Probar gestos táctiles en tablet/smartphone o emulador de Chrome

## 🐛 Troubleshooting

### Los archivos JS no se cargan (404)

- Verificar que `app.UseStaticFiles()` esté en `ApplicationLifecycleManager.cs`
- Confirmar que los archivos existan en `wwwroot/js/`
- Limpiar y reconstruir el proyecto

### JavaScript no se ejecuta

- Abrir DevTools → Console para ver errores
- Verificar que `typeof Keepalive !== 'undefined'` sea `true`
- Revisar que no haya errores de sintaxis en los `.js`

### Cache antiguo del navegador

- Forzar refresh: `Ctrl+F5` (Windows) o `Cmd+Shift+R` (Mac)
- Incrementar `AppVersion` en templates
- Limpiar cache del navegador

---

**Última actualización**: 2024 (Refactorización Fase 1 completada)
