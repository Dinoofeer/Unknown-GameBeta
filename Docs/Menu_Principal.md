# Menú principal — desarrollo por commits

Rama: `feature/menu`. Jesús revisa y realiza cada commit y push manualmente.

## Alcance acordado

1. Estructura y diseño con UI Toolkit (revisado por Jesús y subido).
2. Video de fondo, reproducción en bucle y respaldo estático.
3. Servicio de guardado local de una sola partida.
4. Jugar / Crear partida, Continuar partida y Salir del juego.
5. Guardar y restaurar el progreso de checkpoints.
6. Validación del menú en una compilación y ajustes finales.

El menú de pausa se hará después. El comportamiento al completar niveles se definirá cuando se integren los otros dos niveles. Antes de tener pausa, continuar se probará cerrando y abriendo la compilación.

## Entrega 2 — video de fondo

El archivo `Assets/Video/fondo_menu_new.mp4` es una copia del MP4 entregado: 1024 × 572, 24 FPS, 8 segundos y sin pistas de audio. El proyecto usa una referencia al VideoClip importado; no depende de la carpeta Descargas.

`MainMenuUI` tiene un VideoPlayer y MenuVideoBackground. El controlador prepara el video, crea una RenderTexture en memoria y la muestra en un Image de UI Toolkit solo cuando llega el primer fotograma. Al salir de la escena o desactivar el componente, detiene la reproducción y libera la textura. La RenderTexture no es un archivo que haya que agregar a Git.

El video se repite y usa tiempo sin escalar. Los Image usan ScaleAndCrop: conservan las proporciones y llenan la pantalla recortando bordes, sin estirar la imagen. Los elementos del fondo ignoran el ratón. Una sombra gradual a la izquierda mejora la lectura.

MenuBackgroundFallback.png contiene el primer fotograma del video, sin botones. Se muestra durante la preparación y si falta el clip, falla el reproductor o no llega un fotograma antes de diez segundos. En esos casos se emite un aviso y el menú sigue disponible.

La escena queda con Preview Saved Game desactivado. Sigue siendo una previsualización visual: los botones aún no cargan niveles, guardan ni cierran el juego.

### Comprobaciones realizadas

- Compilación del controlador en Unity, sin errores de C#.
- Video preparado y visible; se observaron frames y tiempo avanzando, y repetición del clip.
- Capturas del menú con dos y tres botones.
- Ausencia temporal del clip en Play: imagen de respaldo visible y selección del botón Salir funcionando. El clip se restauró después.
- Varias entradas y salidas de Play durante la integración.

### Revisión manual antes del segundo commit

1. Abrir Menu-Principal y pulsar Play. Deben verse el video detrás de JUGAR y SALIR DEL JUEGO.
2. Observarlo al menos 20 segundos: debe moverse y repetir su ciclo de ocho segundos. Valorar también si el empalme del video original resulta agradable.
3. Probar ratón, Tab / Shift+Tab y flechas. El fondo no debe bloquear la selección.
4. Probar 1280 × 720, 1920 × 1080 y 1024 × 768. El fondo debe llenar la pantalla sin deformarse; en 4:3 se recortan los lados. Los textos deben seguir siendo legibles.
5. Salir y volver a Play dos veces: debe reiniciarse el fondo sin quedarse negro.
6. Opcional: solo durante Play, quitar Video Clip del VideoPlayer y desactivar/reactivar Menu Video Background. Debe aparecer el respaldo con un aviso de falta del video. Salir de Play descarta estos cambios.
7. Opcional: probar Preview Saved Game como en la primera entrega y dejarlo desactivado al terminar.

El MP4 original puede producir en Windows el aviso `Color primaries 0 is unknown or unsupported by WindowsMediaFoundation`: faltan metadatos de color y el decodificador utiliza valores predeterminados. No impidió la reproducción, pero hay que revisar visualmente el color. También se observaron mensajes del paquete Unity AI Assistant, independientes del menú. La prueba de ausencia del clip genera su aviso de forma intencional.

### Archivos del segundo commit

- Assets/Scenes/Menu-Principal.unity.
- Assets/UI/MenuPrincipal.uxml y MenuStyle.uss.
- Assets/UI/MenuVideoBackground.cs y su .meta.
- Assets/UI/Textures/MenuBackgroundFallback.png y MenuBackgroundShade.png, con sus .meta.
- Assets/Video.meta y Assets/Video/, incluidos el MP4 y su .meta (Git LFS ya está configurado para MP4 y PNG).
- Docs/Menu_Principal.md.

Mensaje sugerido: `feat(menu): integrar video de fondo con respaldo estático`
