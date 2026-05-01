
Mejoras sobre UI y tray icon.

Necesito implementar mejoras en la gestión del ciclo de vida de mi aplicación y el comportamiento del System Tray (NotifyIcon)

Por favor, analiza el código que te proporcionaré y dame las modificaciones exactas para cumplir con estos 3 requerimientos:

1. Instancia Única (Single Instance):

Implementar un control estricto para garantizar que solo se pueda ejecutar una única instancia de la aplicación a la vez (por ejemplo, usando un Mutex global). Si el usuario intenta abrir la app por segunda vez, la instancia actual debe traerse al frente y la nueva debe cerrarse inmediatamente.

2. Comportamiento del Tray Icon (NotifyIcon):

Al hacer un solo clic (izquierdo) sobre el icono de la bandeja del sistema, la ventana principal debe abrirse y mostrarse correctamente.

Si la ventana ya está abierta o minimizada, debe restaurarse (WindowState.Normal) y traerse al frente (BringToFront() / Activate()) para quedar visible por encima de otras ventanas. Lo mismo aplica si apreta la opción "Configuracion..." que abre la ventana de la app.

3. Renombra del menu del TrayIcon la opción "Configuración..." a "Mostrar" es mas entendible.