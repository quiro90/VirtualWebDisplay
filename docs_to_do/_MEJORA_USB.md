* Objetivo Transmision via USB:
Agregar un modo de transmisión por USB que funcione como alternativa automática o manual al modo actual Wi-Fi, sin modificar la arquitectura principal del streaming.

* Concepto clave
No se transmite “video por USB directo”.
USB funciona como interfaz de red cableada.
La app sigue sirviendo contenido vía HTTP/WebRTC.
El cliente (tablet) accede mediante navegador a una URL.
Modos de transmisión
1. Wi-Fi (actual)
Funciona como hoy.
Mantiene todas las configuraciones:
Número de espectadores
Seguridad de pantalla
etc.
2. USB (nuevo - experimental)
Usa red generada por USB tethering.
Comunicación vía IP local (ej: 192.168.42.1).
Restricciones:
maxViewers = 1
Seguridad desactivada (no aplica)
El resto del flujo (captura, encoding, render) no cambia.
UI
Nuevo selector de modo

Ubicación:

Barra superior, junto al botón de configuración (ícono de llave)

* Tipo:

ComboBox

* Opciones:

"Acceso WiFi"
"Acceso USB (experimental)"

* Comportamiento:

Default: Wi-Fi, si se detecta disponible USB.
Persistir última selección (opcional)
Al cerrar y abrir la app (cargar configuración) si USB no esta disponible y estaba guardado cargar Wi-Fi.

* Lógica de conexión
Modo USB
* Intentar conexión a IPs típicas de red USB:
192.168.42.1
192.168.137.1

* Endpoint de validación:
GET /ping o /health
Timeout corto (~500ms)

* Resultado:
Si responde → usar USB
Si falla → fallback a Wi-Fi
Ajustes automáticos por modo
Si USB:
Forzar:
maxViewers = 1
security = disabled
Ocultar o deshabilitar esos controles en UI
Si Wi-Fi:
Todo configurable como actualmente
Cliente (tablet / navegador)

* Acceso manual vía URL:
USB: http://IP_USB:PORT
Wi-Fi: http://IP_LOCAL:PORT
No requiere app nativa
Backend (mínimos cambios)
Agregar endpoint liviano:
GET /ping → 200 OK
Reutilizar servidor existente

* Consideraciones
No detectar “USB” directamente → detectar conectividad
No usar WebUSB (no compatible universalmente)

iPad: puede no soportar bien USB → fallback Wi-Fi
Resultado esperado

Conectar USB → abrir navegador → acceder a URL → ver stream
Menor latencia y mayor estabilidad que Wi-Fi
Sin instalar apps adicionales