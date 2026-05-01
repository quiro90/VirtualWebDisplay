
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
