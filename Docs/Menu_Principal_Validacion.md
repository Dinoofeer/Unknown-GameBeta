# Menú principal — validación final

## Alcance de la rama

El menú usa UI Toolkit, video de fondo y un único archivo de partida. Sin guardado muestra JUGAR y SALIR DEL JUEGO; con guardado válido muestra CREAR PARTIDA, CONTINUAR PARTIDA y SALIR DEL JUEGO. Crear otra partida requiere confirmación y sustituye la anterior.

Continuar restaura el nivel, el último checkpoint, las muertes, el tiempo y una llave previamente asegurada. Las trampas se reinician al cargar la escena. No se restaura la posición exacta en medio de un salto.

El menú de pausa, el flujo al completar niveles y las escenas de los niveles 2 y 3 quedan para trabajos posteriores.

## Guardado

La versión normal utiliza `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Unknown GameBeta/Save/partida.json`. El nombre y la carpeta dependen de Company Name y Product Name; cambiar esos valores en otra versión cambia dónde busca Unity el guardado.

El archivo se actualiza al alcanzar un checkpoint y al cerrar normalmente la aplicación. El temporal partida.json.tmp nunca se carga como otra partida. Un cierre forzado solo conserva la última escritura que haya terminado.

La llave se asegura al alcanzar un checkpoint nuevo después de recogerla. En el recorrido actual la llave está después de los dos checkpoints; recogerla por sí solo no la asegura.

## Controles y comportamiento

- Ratón para elegir y activar botones.
- Tab / Shift+Tab y navegación con flechas para cambiar el foco; Enter para activar.
- Cancelar o Escape cierran la confirmación conservando la partida.
- Salir detiene Play Mode en el Editor y cierra la aplicación en una compilación.
- En el Editor hay que abrir Menu-Principal antes de pulsar Play para probar el flujo completo. Play directamente en Level_01 no crea ni carga una partida automáticamente.

## Configurar niveles adicionales

1. Incluir la nueva escena en las escenas de compilación, manteniendo Menu-Principal primero.
2. Añadir LevelProgressPersistence y asignar jugador, RespawnManager, RunStats, cámara y llave si existe.
3. Dar identificadores estables y únicos a los checkpoints reales, y asignarlos en orden al componente. Evitar renombrar identificadores ya publicados sin una migración.
4. Integrar la transición entre niveles para actualizar el nivel de la sesión antes de guardar. Esa transición aún no forma parte de esta rama.

Los scripts se reutilizan entre niveles; no se duplican por cada escena.

## Compilaciones de revisión

La carpeta Builds está ignorada por Git. Para compartir una compilación Windows hay que entregar la carpeta completa del ejecutable, incluida su carpeta _Data y las bibliotecas contiguas.

La subcarpeta `UnknownGameBeta_BackUpThisFolder_ButDontShipItWithYourGame` es un respaldo generado por Unity para depuración; se excluye del paquete que se distribuye.

La compilación QA usa Product Name `Unknown GameBeta Menu QA`, de forma temporal durante su generación, para separar su guardado del de la versión normal. El proyecto debe quedar con Product Name `Unknown GameBeta` al terminar la validación.

## Pruebas automatizadas

En Window > General > Test Runner > EditMode, ejecutar UnknownGame.Save.EditorTests. El conjunto contiene 35 pruebas: 18 del archivo de guardado, 9 del flujo del menú y 8 del progreso de checkpoints. Las pruebas usan carpetas temporales dentro del proyecto.

## Resultados de la entrega 6 — 8 de noviembre de 2026

Entorno: Unity 6000.6.0f1, Windows de 64 bits. Las dos compilaciones finalizaron con resultado Succeeded y 0 errores.

| Comprobación | Resultado |
|---|---|
| Compilación QA | Correcta; aproximadamente 5 min 57 s en la primera generación de recursos. |
| Compilación normal | Correcta; aproximadamente 23 s con los recursos ya procesados. |
| Pruebas EditMode | 35 aprobadas, 0 fallidas, 0 omitidas. |
| Inicio del ejecutable | Abre Menu-Principal. |
| Primera partida en QA | JUGAR crea el archivo y abre Level_01. |
| Video | Visible y animado en el ejecutable; se observaron distintos fotogramas durante la revisión. Sin pista de audio. |
| Resoluciones solicitadas al ejecutar | 1280×720, 1024×768 y 1920×1080. Menú y confirmación completos, sin textos cortados en las capturas revisadas. |
| Cancelación de reemplazo | Escape cierra el diálogo; se mantiene la misma identidad de partida. |
| Continuar en la compilación | Se preparó un guardado QA con level01_cp02, 7 muertes y 124,5 s. El ejecutable restauró el segundo checkpoint, la cámara y las estadísticas. No se recorrió todo el nivel hasta ese checkpoint en esta prueba de compilación. |
| Teclado físico | Se confirmó que Tab, flechas y Enter cambian y activan la selección en el ejecutable QA. |
| Nueva partida | La revisión manual produjo otra identidad de partida y un inicio sin checkpoint previo. |
| Cierre del juego | El botón SALIR DEL JUEGO cerró el proceso de la compilación normal. El cierre de la ventana del nivel QA conservó estadísticas en su propio archivo. |
| Guardado real | Su huella SHA-256 antes y después de las pruebas coincidió. |

### Artefactos locales

- Versión normal: `Builds/MenuValidation/UnknownGameBeta.exe`.
- Versión QA: `Builds/MenuValidationQA/UnknownGameBetaQA.exe`.
- Tamaño de contenido reportado para la versión normal: 141.362.886 bytes (aproximadamente 135 MiB).
- Informes completos de compilación: `Temp/Menu6NormalBuildReport.json` y `Temp/Menu6QABuildReport.json`.
- Registros de las ejecuciones: `Logs/MenuNormal.log` y `Logs/MenuQA-*.log`.

Estos archivos son locales y están excluidos de Git. La versión normal utiliza la identidad y el guardado normales; QA utiliza su carpeta independiente. La configuración del proyecto quedó con su Product Name original.

### Avisos observados

La compilación normal reportó 650 avisos de shaders: 649 de recursos de Sentis/inferencia y uno de TextMeshPro/Mobile/Distance Field. La compilación QA reportó 510 avisos de esos mismos grupos. No fueron errores de compilación de los scripts del menú y no se retiraron ni actualizaron paquetes en esta entrega.

El reproductor de Windows siguió mostrando el aviso de metadatos del MP4 `Color primaries 0 is unknown or unsupported by WindowsMediaFoundation`. La imagen se reprodujo y se revisó visualmente. En los registros QA también apareció `Curl error 42: Callback aborted` coincidiendo con el cierre de la aplicación. No se observaron excepciones de los scripts del juego en los registros revisados.

### Cambios de este último paso

Unity produjo cambios temporales en prefiltrado y recursos runtime de URP, precarga de Input System y Unity Connect al compilar. Se restauraron a la configuración versionada original después de las pruebas. No deben incluirse como cambios de funcionalidad del menú.

## Revisión manual

1. Abrir el ejecutable normal y comprobar que inicia en el menú, con el video visible y animado.
2. Revisar botones y confirmación con ratón y teclado.
3. Comprobar que cancelar una partida nueva conserva el guardado y que confirmar empieza desde cero.
4. Jugar hasta un checkpoint, cerrar normalmente y abrir otra vez: Continuar debe recuperar ese checkpoint y las estadísticas.
5. Revisar el menú en pantallas 16:9 y 4:3: el fondo llena el área conservando proporciones y los textos no se cortan.
6. Probar Salir desde el menú: el proceso debe cerrarse.
7. Revisar los archivos preparados para el commit. No subir Builds, Temp, Logs ni los archivos de partidas.
