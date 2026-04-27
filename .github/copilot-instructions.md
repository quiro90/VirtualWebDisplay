# Copilot Instructions

## Directrices del proyecto
- En este proyecto, el usuario prefiere que el modo WebRTC use los mismos controles configurables que Web image (intervalo y calidad JPEG) y que la configuración de usuario se persista en una carpeta oculta `.virtualwebdisplay` dentro del perfil del usuario.
- El proyecto VirtualWebDisplay usa top-level statements en `Program.cs` y tiene una carpeta `/refactoring/PLAN.md` en la raíz del repo para tracking del refactoring. El usuario prefiere llevar un fichero de tracking de refactoring en `/refactoring/PLAN.md` en la raíz del repo, con el estado de cada paso, lo que ya fue hecho y lo que falta, para poder retomar sin repetir procesos.
- Los handlers se ubican en `Controllers/Handlers/`.