---
tags: [touch, js, cliente]
aliases: [touch-input.js, TouchInput, Script Touch]
type: referencia
updated: 2026-07-08
---

# touch-input.js

**Archivo**: `wwwroot/js/touch/touch-input.js` (~580 líneas) · `window.TouchInput`.

Script touch **compartido** por ambos modos ([[WebImage (JPEG Polling)|WebImage]] y [[WebRTC (H.264)|WebRTC]]).

## API

```javascript
TouchInput.init({
    elementId: 'screen',
    throttleMs: 50,
    holdDelayMs: 300,
    // + parámetros granulares inyectados server-side
});
TouchInput.getStats();
```

## Comportamiento

- Modo **absoluto**: el cursor se posiciona donde se toca.
- Respeta los **delays configurados** y los **toggles por gesto** antes de emitir eventos.
- Throttling para evitar saturación de red.
- Ver [[Gestos Táctiles]] para el catálogo de gestos.

## Parámetros inyectados

Los templates HTML ([[HTML Templates]]) inyectan los parámetros granulares (zoom/hold/scroll enabled + delays) desde la config server-side en `TouchInput.init`.

## Compatibilidad legacy

`window.VirtualWebDisplayTouchInput.getStats()` sigue funcionando.

## Enlaces

- [[Entrada Táctil]]
- [[Módulos JavaScript]]
- [[InputHandler (Touch)]]
- [[HTML Templates]]