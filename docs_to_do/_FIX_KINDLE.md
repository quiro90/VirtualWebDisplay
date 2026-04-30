FIX Kindle
En mi proyecto en .NET, tengo un cliente web que realiza un "polling" rápido para descargar imágenes y simular un stream de video en el navegador. 

**El Contexto y el Problema:**
Recientemente cambiamos la forma de renderizar la imagen. Pasamos de usar una etiqueta `<img>` estándar a usar un `<div>` donde actualizamos la propiedad `background-image` por Javascript.
Hicimos este cambio porque en dispositivos iOS (Safari) y otros navegadores móviles, al usar `<img>`, cuando el usuario dejaba el dedo presionado para interactuar, el navegador interpretaba que quería "arrastrar" o "guardar" la imagen, lo cual rompía la experiencia táctil.

Sin embargo, este cambio rompió la visualización en el **Amazon Kindle Paperwhite 12** (su navegador es muy básico):
- La página carga bien, la seguridad y el código JS funcionan.
- **Pero la pantalla se ve completamente negra.**
- Si toco la pantalla en el Kindle, a veces se ve un "parpadeo" y el frame de la imagen aparece por un microsegundo y vuelve a desaparecer. Esto me indica que las imágenes sí se descargan, pero el motor de renderizado del Kindle no dibuja el `background-image` dinámico correctamente a menos que se fuerce un repintado (al tocar), eso es importante porque se simulan comportamientos del ratón por entrada tactil.

El Objetivo: Necesito una solución que me permita lograr AMBOS objetivos a la vez, es decir, que funcione fluido en el Kindle Paperwhite 12 (probablemente requiera volver a usar la etiqueta <img> o usar un <canvas>, ya que el Kindle maneja mejor esas actualizaciones) y evitar el comportamiento nativo de "arrastrar/guardar imagen" en Safari (iOS) y otros navegadores móviles, permitiendo que mis eventos táctiles sigan funcionando sin interrupciones.
Es importante destacar que en navegadores básicos (como es el de la kindle) me basta que solo se visualice el contenido no espero que funcione correctamente la interacción táctil eso es un por menor en esos casos.

Por favor, analízalo y proponme la mejor aproximación moderna (ej: volver a <img> + aplicar CSS como pointer-events: none, user-select: none, -webkit-user-drag: none, o implementar un <canvas> donde se dibuje el frame). 
Dame el código exacto de la solución (tanto el JS actualizado como el CSS necesario).

-----------------------
