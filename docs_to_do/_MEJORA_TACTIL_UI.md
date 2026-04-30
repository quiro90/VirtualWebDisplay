Mejoras a implementar en la entrada táctil y gestos:

1. Gesto de zoom:

Al hacer zoom (pellizco), la pantalla no debe rotar la imagen accidentalmente.
El zoom solo debe aplicarse si no hay un gesto de scroll activo.
La detección de separación o unión de dedos debe ser menos sensible: una separación mínima no debe activar el zoom, para evitar confusiones con el gesto de scroll (dos dedos).
Configuración de gestos táctiles:

2. Eliminar el combo box actual de modos táctiles (solo toques / gestos).
Reemplazarlo por checkboxes independientes para cada gesto, que puedan habilitarse/deshabilitarse en tiempo real.
Cada checkbox debe tener al lado un control para configurar el tiempo de respuesta (ms), independiente para cada gesto.
Los controles a agregar al final del tab de configuración táctil son:
Zoom (abrir dedos o pellizco) — valor por defecto: 50 ms
Mantener toque (presión larga) — valor por defecto: 250 ms
Scrolling (dos dedos) — valor por defecto: 250 ms
Persistencia y reactividad de la configuración:

3. Todas las configuraciones deben guardarse y restaurarse automáticamente al iniciar o salir de la aplicación, igual que el resto de configuraciones. Estas nuevas opciones deben habilitarse o deshabilitarse automáticamente según el estado del check de "Entrada táctil" (como todo el tipo de configuración actual lo hacen). La opción "Recordar posición del puntero" debe permanecer separada y funcionar como hasta ahora.

IMPORTANTE:
Los cambios implementados deberán respetar la organización, arquitectura y practicas actuales del codigo.
Evitar duplicar codigo (dividir responsabilidades correctamente y añadirla en lugares adecuados).

-----------------------
Mejoras sobre UI y tray icon.

Necesito implementar mejoras en la gestión del ciclo de vida de mi aplicación y el comportamiento del System Tray (NotifyIcon), además de resolver una excepción no controlada.

Por favor, analiza el código que te proporcionaré y dame las modificaciones exactas para cumplir con estos 3 requerimientos:

1. Instancia Única (Single Instance):

Implementar un control estricto para garantizar que solo se pueda ejecutar una única instancia de la aplicación a la vez (por ejemplo, usando un Mutex global). Si el usuario intenta abrir la app por segunda vez, la instancia actual debe traerse al frente y la nueva debe cerrarse inmediatamente.

2. Comportamiento del Tray Icon (NotifyIcon):

Al hacer un solo clic (izquierdo) sobre el icono de la bandeja del sistema, la ventana principal debe abrirse y mostrarse correctamente.

Si la ventana ya está abierta o minimizada, debe restaurarse (WindowState.Normal) y traerse al frente (BringToFront() / Activate()) para quedar visible por encima de otras ventanas.

3. Resolución de Excepción No Controlada:

Actualmente, el Tray Icon está generando un crash (excepción no controlada) en cierto momento.

IMPORTANTE:
Los cambios implementados deberán respetar la organización, arquitectura y practicas actuales del codigo.
Evitar duplicar codigo (dividir responsabilidades correctamente y añadirla en lugares adecuados).

-------------------------------------------
Mejora (investigar) -> USB data, transmitir imagen directa via USB (en vez de wifi) evitando tambien el uso de app interna (investirgar ver posibilidades)