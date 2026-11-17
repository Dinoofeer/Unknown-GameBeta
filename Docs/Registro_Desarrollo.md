# Registro de desarrollo — Unknown Game (Unity)

Documento vivo con las decisiones técnicas y de implementación tomadas durante el desarrollo, y los avances en el proyecto. La referencia de **qué** hace el juego es el GDD (`gdd-estudio-6`); aquí se registra **cómo** se implementa y por qué.

Tipos de entrada: **Decisión** (elegida por falta de detalle en el GDD), **Interpretación** (lectura de un texto ambiguo del GDD), **Aviso GDD** (contradicción o vacío a resolver por el equipo), **Pendiente**.

---

## Convenciones generales

| Tema | Decisión | Motivo |
|---|---|---|
| Escala | 1 bloque del GDD = 1 unidad de Unity. | Las alturas de salto del GDD (2/4 bloques) se traducen directamente. |
| Entrada | Input System nuevo, reutilizando `Assets/Settings/InputSystem_Actions.inputactions` (mapa `Player`). Las acciones se asignan en el Inspector con `InputActionReference`. | El proyecto ya usa solo el Input System nuevo; no añade paquetes. |
| Código | Identificadores en inglés (convención de Unity), comentarios en español. Un script = una responsabilidad. | Arquitectura Técnica del GDD: bajo acoplamiento y clases pequeñas. |
| Valores ajustables | Velocidades, tiempos y alturas son campos `[SerializeField]` editables en el Inspector. | GDD: "datos separados de lógica". |
| Escenas de prueba | Las mecánicas se prueban en `Assets/Scenes/Sandbox/` antes de montar niveles. | Probar cada mecánica aislada. |

---

## Mecánica 01 — Movimiento Lateral Horizontal

**Estado:** implementada; falta la prueba manual con teclado (ver "Pruebas").

### Decisiones

| # | Tipo | Decisión | Motivo / alternativa descartada |
|---|---|---|---|
| 1 | Decisión | Arranque instantáneo (sin aceleración). | Simétrico al frenado en seco del GDD; máxima precisión. Alternativa: aceleración de ~0,05 s. |
| 2 | Decisión | Izquierda + derecha a la vez = quedarse quieto. | El GDD permite esta opción o "priorizar la última tecla". Quedarse quieto lo hace el Input System solo y es predecible. |
| 3 | Interpretación | Misma velocidad en el aire que en el suelo. | "Corregir ligeramente" se interpreta como control aéreo total pero con poco tiempo en el aire. Alternativa: control aéreo reducido (~70 %). |
| 4 | Decisión | Velocidad máxima 8 bloques/s (ajustable). | El GDD no da un valor; se calibrará jugando. |
| 5 | Decisión | Se asigna `Rigidbody2D.linearVelocity.x` en `FixedUpdate`; la Y no se toca. | Frenado en seco sin inercia, y deja la Y libre para gravedad y salto. |
| 6 | Decisión | La dirección se redondea a -1/0/1. | El composite 2D normaliza: pulsar D+W daba ~70 % de velocidad. |
| 7 | Decisión | Material `PM_NoFriction` (fricción 0, rebote 0) en el collider del jugador. | Caso límite del GDD: no quedarse pegado a las paredes al caer. |
| 8 | Decisión | Collider del jugador: `CapsuleCollider2D` vertical de 1×1. | La cápsula no se engancha en las juntas entre bloques del suelo. |
| 9 | Decisión | Rigidbody2D: Dynamic, Interpolate, Continuous, rotación Z congelada, gravityScale 1. | Movimiento suave y sin atravesar paredes. La gravedad se recalibrará con el salto (Mecánica 02). |

### Archivos creados

- `Assets/Scripts/Player/PlayerMovement.cs`
- `Assets/Materials/Physics/PM_NoFriction.physicsMaterial2D`
- `Assets/Scenes/Sandbox/Sandbox_Player.unity`: copia de la plantilla URP 2D, con Main Camera ortográfica (tamaño 9,5) y Global Light 2D. Greybox: suelo 30×1 en y=-4, paredes 1×10 en x=±15,5 y bloque elevado 3×1 en (4, -0,5).
- `Assets/Prefabs/Player/Player.prefab`: la instancia de la escena está conectada al prefab.

No se añadió la escena a Build Settings: es solo de pruebas.

### Pruebas

- Compilación: sin errores en consola.
- Captura de la escena: el greybox y el jugador están colocados correctamente.
- Play Mode: arranca sin errores. Simulé la tecla D por código, pero Unity no avanzó frames porque la ventana del Editor no tenía el foco (Run In Background desactivado). **El movimiento todavía no se ha probado con teclado.**

**Prueba manual pendiente** (abrir `Sandbox_Player` y pulsar Play con el foco en el Game View):
1. A/D y las flechas mueven al jugador; al soltar, frena en seco.
2. A + D a la vez → quieto.
3. Contra la pared: no la atraviesa.
4. D + W → misma velocidad que solo D.
5. Salir de Play Mode y volver a entrar: el comportamiento se repite.

Contra la pared en el aire solo se podrá probar cuando exista el salto.

---

## Mecánica 02: Salto y doble salto

**Estado:** implementada y probada por simulación: alturas, combinaciones de doble salto, recarga y corte contra el techo. Falta la prueba manual con teclado real, sobre todo para el umbral de 0,12 s (ver "Pruebas").

### Decisiones

| # | Tipo | Decisión |
|---|---|---|
| 1 | GDD | Dos alturas fijas: leve 2 bloques, máximo 4, editables en el Inspector. Se salta con la barra espaciadora. |
| 2 | Decisión | Umbral de pulsación de 0,12 s: si se suelta antes, el salto es leve; si se mantiene, pasa a máximo. |
| 3 | Decisión | Alturas exactas: la velocidad se calcula con v = √(2gh) a partir de la gravedad. Al pasar a máximo se recalcula para completar exactamente 4 bloques desde el despegue. |
| 4 | Decisión | Rigidbody2D gravityScale = 3: cae más rápido y el salto flota menos. No cambia las alturas. |
| 5 | GDD | Doble salto: un único uso en el aire; solo se recarga al tocar suelo. |
| 6 | Decisión | Cuenta como superficie sólida la que tiene una inclinación ≤ 45°. Paredes y techos no recargan el doble salto, para que no se pueda escalar por las paredes. |
| 7 | Decisión | Caer de un borde sin saltar también permite usar el salto en el aire (cuenta como haber abandonado el suelo). |
| 8 | GDD | Un techo o un bloque invisible cortan la subida por física. Si ocurre antes del umbral, el salto ya no se amplía a máximo. |
| 9 | Decisión | Cada salto mide su altura desde donde empieza. Si el doble salto se usa en el punto más alto, se suman 4+2 = 6 o 4+4 = 8, como dice el GDD. Si se usa mientras aún se sube, el total es menor. |
| 10 | Decisión | La detección de suelo va en un script aparte (`PlayerGroundCheck`), por contactos y sin capas. |
| 11 | Riesgo | No están implementados: el coyote time (lo menciona Alcance pero no Mecánicas; ver aviso A1) ni el jump buffer (no está en el GDD). Se pueden añadir si se piden. |
| 12 | Decisión | Control aéreo sin cambios: misma velocidad horizontal que en el suelo. |
| 13 | Decisión | Corrección de integración: +g·Δt/2 para que las alturas coincidan con el GDD. La velocidad del salto es v = √(2gh) + g·Δt/2, con Δt = `Time.fixedDeltaTime`. Unity integra la física en pasos y, sin esta corrección, perdía ~5 % de altura (1,89 en vez de 2). Aprobada el 2026-10-26. |

### Archivos

- Nuevos: `Assets/Scripts/Player/PlayerGroundCheck.cs` y `Assets/Scripts/Player/PlayerJump.cs`.
- Prefab `Player.prefab`:
  - Se añadieron PlayerGroundCheck y PlayerJump.
  - `jumpAction` apunta a Player/Jump, que tiene los bindings Space, Gamepad buttonSouth y XR secondaryButton.
  - gravityScale = 3.
- Escena `Sandbox_Player`:
  - `AlturasReferencia` con Ref_2, Ref_4, Ref_6 y Ref_8: líneas sin collider a 2/4/6/8 bloques sobre la cara superior del suelo (y = -3,5).
  - `Techo_Prueba`: bloque sólido de 3×1 en x = -3, con la cara inferior a 3 bloques sobre el suelo.
- `PlayerMovement.cs` no se tocó.

### Pruebas

La consola no muestra errores de compilación. Los únicos errores son internos del Editor (barra de herramientas) y del paquete de IA de Unity, sin relación con el proyecto.

Unity no avanza frames con el Editor sin foco. Por eso probé en Play Mode simulando la física paso a paso (`Physics2D.Simulate`, 0,02 s por paso) y la tecla Espacio con el Input System. Al terminar dejé `simulationMode` y la configuración de entrada del Editor como estaban.

**Primera ronda (sin corrección)**

| Prueba | Resultado |
|---|---|
| El jugador cae y queda apoyado | OK: IsGrounded = true, y = -2,993 |
| Salto leve | 1,893 (esperado 2) |
| Doble salto leve en el punto más alto | total 3,779 (esperado 4) |
| Tercer salto en el aire | OK: no salta |
| Recarga al aterrizar | OK: vuelve a saltar |

Las alturas salían ~5 % bajas, y de ahí sale la decisión 13.

**Segunda ronda (con la corrección +g·Δt/2)**

| Prueba | Medido | Esperado |
|---|---|---|
| Salto leve | 1,999 | 2 |
| Leve + doble leve (en el punto alto) | 3,997 | 4 |
| Salto máximo | 4,000 | 4 |
| Máximo + doble leve | 5,994 | 6 |
| Leve + doble máximo | 5,998 | 6 |
| Máximo + doble máximo | 7,995 | 8 |
| Salto máximo bajo Techo_Prueba | sube 1,985 y se detiene (vy = 0), luego cae y aterriza (grounded) | ~2, cortado |

Cómo se probó el salto máximo:
- `Time.fixedTime` no avanza con la simulación manual (siguió en 0 tras varios pasos).
- Por eso puse `holdThreshold = 0` solo en la instancia en Play Mode (se descarta al salir), con Espacio mantenido de verdad (`IsPressed`).
- Así se probaron el paso a máximo y la recarga de altura. **No** se probó el umbral de 0,12 s en sí, ni el recálculo a mitad de subida que ocurre cuando el paso a máximo llega a los 0,12 s.
- El código no se cambió para la prueba.

**Prueba manual pendiente** (Play con el foco en el Game View):
- Un toque rápido de Espacio llega a Ref_2; mantenerlo llega a Ref_4.
- Las combinaciones del doble salto llegan a Ref_6 y Ref_8.
- Comprobar que el umbral de 0,12 s se siente bien al jugar.

---

## Mecánica 03: Bloques invisibles

**Estado:** implementado y probado por simulación. El reinicio tras morir se conectará en la Mecánica 04.

### Decisiones

| # | Tipo | Decisión |
|---|---|---|
| 1 | GDD | Invisible y sin colisión hasta que se golpea desde abajo. Entonces se vuelve visible y sólido y corta la subida. Su posición es fija. |
| 2 | Decisión | Detección con un trigger fino en la parte inferior del bloque: 0,35 de alto, que sobresale 0,1 por debajo del borde. Es lo bastante alto para que a la velocidad máxima de salto no se lo salte en un paso de física. |
| 3 | Decisión | Solo se activa si el jugador sube y su cabeza está por debajo del borde superior del detector. Tocarlo de lado o desde arriba no hace nada: lo atraviesa, como dice el GDD. |
| 4 | Decisión | Si la cabeza ya entró en el bloque, se recoloca justo debajo. Así no hay empujones laterales (el caso límite de la esquina del GDD); además, la fricción del jugador es 0. |
| 5 | Decisión | El doble salto se conserva tras el golpe, porque el GDD dice que el jugador puede reaccionar en el aire. |
| 6 | Decisión | `stayRevealedAfterDeath = false` por defecto, porque la Mecánica 03 dice "el resto del intento actual". La Mecánica 04 dice que se puede configurar para que siga visible: es una casilla por bloque en el Inspector. El reinicio real se conecta en la Mecánica 04 (método `ResetAfterDeath`). |
| 7 | Decisión | Se identifica al jugador por el tag "Player". |
| 8 | Decisión | Contorno rosa visible solo en el editor (Gizmo), para poder colocar los bloques. |

### Archivos

- Nuevo: `Assets/Scripts/Traps/InvisibleBlock.cs`.
- Nuevo: `Assets/Prefabs/Traps/InvisibleBlock.prefab`. Bloque de 1×1 con:
  - SpriteRenderer Square, gris claro.
  - BoxCollider2D sólido de 1×1, asignado a `solidCollider`.
  - BoxCollider2D trigger de 1×0,35 con offset (0, -0,425), asignado a `detector`.
  - `stayRevealedAfterDeath = false`.
- `Player.prefab`: tag cambiado de "Untagged" a "Player".
- Escena `Sandbox_Player`:
  - `InvisibleBlock_Bajo` en x = -11, con la cara inferior a 3,5 sobre el suelo.
  - `InvisibleBlock_Alto` en x = 9, con la cara inferior a 5,5 sobre el suelo.
- Scripts del jugador sin tocar.

**Desviación respecto a la propuesta inicial (2,5 y 4,5):** el jugador mide 1 bloque, así que su cabeza llega a 3 con el salto leve y a 5 con el máximo. Con el borde a 2,5, el salto leve también lo habría activado. Por eso los bordes se pusieron a 3,5 (lo golpea el salto máximo pero no el leve) y a 5,5 (solo con doble salto).

### Pruebas (simulación paso a paso en Play Mode)

| Prueba | Resultado |
|---|---|
| Estado inicial | Oculto y sin colisión: OK |
| Pasar andando por debajo | No se activa: OK |
| Salto leve debajo (la cabeza llega a 3,009) | No se activa: OK |
| Atravesarlo de lado a su altura | Lo atraviesa y no se activa: OK |
| Salto máximo debajo de Bajo | Se revela y queda sólido. Cabeza máxima a 3,490 con el borde a 3,500, sin quedar dentro. vy = 0 |
| Doble salto tras el golpe (ya fuera de debajo) | Funciona (vy = 10,56). Un tercer salto no hace nada: OK |
| Doble salto justo debajo del bloque recién revelado | Se gasta, pero el bloque lo frena enseguida (vy = 0). Es lo esperado físicamente: hay un techo encima. |
| Aterrizar tras el golpe | grounded: OK |
| De pie sobre el bloque revelado | y = 1,507, grounded: OK |
| `ResetAfterDeath` | Vuelve a ocultarse y pierde la colisión: OK |
| Salto máximo bajo Alto (cabeza a 5,010) | No se activa: OK |
| Máximo + doble máximo bajo Alto | Se revela; cabeza máxima a 5,500, sin quedar dentro: OK |

El salto máximo se probó con `holdThreshold = 0` solo durante la prueba en Play Mode. Al terminar se restauraron 0,12, `simulationMode` y la configuración de entrada del Editor.

---

## Mecánicas 04 y 05: Reaparición y puntos de control

**Estado:** implementadas y probadas en Play Mode (simulación paso a paso para las muertes y frames reales para el retardo de reaparición).

### Decisiones

| # | Tipo | Decisión |
|---|---|---|
| 1 | GDD | Sin Game Over ni pantallas de carga. Partículas al morir. Se reaparece en menos de 1 s en el último checkpoint. Solo hay un checkpoint activo y cambia de aspecto al activarse. Se cubre el caso límite del mismo frame. |
| 2 | Decisión | Retardo de reaparición de 0,5 s, editable. |
| 3 | Riesgo | Contradicción: Mecánicas dice menos de 1 s y Alcance menos de 0,2 s (aviso A2). Se eligió Mecánicas porque es la especificación detallada y deja ver las partículas. Si el equipo prefiere 0,2, solo hay que cambiar el valor. |
| 4 | Decisión | Arquitectura por eventos estáticos (`PlayerDeath.Died`, `Checkpoint.Reached`, `RespawnManager.PlayerRespawned`), como pide el principio de bajo acoplamiento del GDD: las trampas no conocen al checkpoint, ni al contador, ni al narrador. |
| 5 | Decisión | El punto de reaparición se lee al reaparecer, no al morir. Así se resuelve el caso límite del mismo frame sin código extra. |
| 6 | Decisión | Cada checkpoint se activa una sola vez: volver a uno anterior no retrocede el progreso. |
| 7 | Decisión | El reinicio de trampas ocurre al reaparecer, como dice la Mecánica 04. La Mecánica 05 dice "al activarlo", pero se interpreta como "para el siguiente intento". |
| 8 | GDD | Checkpoint falso (troll): se monta con el sprite de checkpoint y `LethalHazard` en vez de `Checkpoint`. No necesita código. |
| 9 | Decisión | Peligro letal genérico (`LethalHazard`), que sirve para pinchos, caída libre (KillZone) y checkpoints falsos. |
| 10 | Decisión | Aspecto provisional del checkpoint: cambia de gris a verde, hasta que haya arte o animación. |

### Archivos

- Scripts nuevos:
  - `Assets/Scripts/Player/PlayerDeath.cs`
  - `Assets/Scripts/Level/Checkpoint.cs`
  - `Assets/Scripts/Level/RespawnManager.cs`
  - `Assets/Scripts/Traps/LethalHazard.cs`
- `InvisibleBlock.cs`: se suscribe a `RespawnManager.PlayerRespawned` en `OnEnable`/`OnDisable` y se cambió el comentario de `ResetAfterDeath`.
- Prefabs nuevos:
  - `Prefabs/Effects/DeathParticles`: 20 partículas cuadradas del color del jugador en una sola ráfaga, vida 0,5 s, sin loop, Stop Action = Destroy, material URP `Sprite-Unlit-Default`.
  - `Prefabs/Traps/Spikes`: 1×0,5 con el sprite Triangle por defecto en rojo, trigger y `LethalHazard`.
  - `Prefabs/Traps/KillZone`: trigger de 40×2 con `LethalHazard`, sin sprite.
  - `Prefabs/Level/Checkpoint`: poste de 0,3×2, trigger, `spawnOffset` (0,8, -0,5) para aparecer de pie junto al poste.
- `Player.prefab`: añadido `PlayerDeath`, con controlScripts = [PlayerMovement, PlayerJump], spriteRenderer y DeathParticles.
- Escena `Sandbox_Player`:
  - `LevelStart` en (-8, -3).
  - `RespawnManager` con el player y `levelStart` asignados.
  - El suelo se partió en `Ground_Left` (x -15..-1) y `Ground_Right` (x 2..15), dejando un hueco de 3 bloques.
  - KillZone de 60×2 en y = -10.
  - Spikes en x = 12,5 y Checkpoint en x = 7.
- `PlayerMovement` y `PlayerJump` sin tocar.

### Pruebas

| Prueba | Resultado |
|---|---|
| Tocar Spikes | Muere a los 7 pasos. Aparecen las partículas; el jugador queda oculto, sin física y con movimiento y salto desactivados. `Died` se lanza **1 vez**, aunque se llamó a `Die()` dos veces más. |
| Reaparición tras los pinchos | En LevelStart, con vel = 0 y todo reactivado; las partículas se destruyeron. Primera vez x = -7,84 (ver R1), segunda vez x = -8,00 exacto. |
| Caer por el hueco | La KillZone lo mata en y = -8,6; `Died` 1 vez; reaparece en (-8,00, -2,99). |
| Bloque invisible revelado + muerte | Al reaparecer, el bloque vuelve a ocultarse y pierde la colisión. |
| Checkpoint y pinchos en el mismo paso | `Reached` = 1, `Died` = 1. El checkpoint se pone verde y reaparece en el checkpoint (≈7,8). |
| Morir de nuevo tras el checkpoint | Muere en los pinchos; ya no se puede medir la reaparición porque Jesús empezó a jugar en el Game View. |
| Consola | Sin errores CS ni excepciones del proyecto. |

**No medido:** el retardo exacto de 0,5 s. Solo se comprobó que reaparece en la siguiente lectura, unos segundos después.

**R1 (Riesgo, RESUELTO el 2026-10-26):** al reaparecer, `PlayerMovement` y `PlayerJump` conservaban el estado que tenían al morir.
- Si se soltaba la dirección mientras estaba muerto, el primer paso de física lo movía con la dirección vieja (+0,16 observado).
- Si moría durante los primeros 0,12 s de un salto con el botón mantenido, al reaparecer podía completar el salto máximo.
- Solución aprobada y aplicada en `OnDisable`:
  - `PlayerMovement` pone `direction = 0`.
  - `PlayerJump` pone `jumpRequested = false` y `canUpgrade = false`.

---

## Mecánica 07: Plataformas que caen

**Estado:** implementada y probada: pisarla, el contacto lateral, con y sin temblor, la caída, la muerte y la reaparición con la plataforma reiniciada. Solo falta la prueba manual de R1 (ver "Pruebas").

### Decisiones

| # | Tipo | Decisión |
|---|---|---|
| 1 | GDD | Contacto → temporizador corto → caída. Vuelve intacta al reaparecer. El contacto lateral también la activa. |
| 2 | Decisión | Retardo de 0,35 s, dentro del rango 0,2–0,5 del GDD y editable dentro de ese rango. |
| 3 | Decisión | Temblor activado por defecto, con una casilla por plataforma (el GDD deja las dos opciones). Solo tiembla el sprite, para no mover el collider. |
| 4 | Decisión | La caída combina las dos opciones del GDD: se desactiva el collider (el jugador pierde el apoyo al instante) y pasa a Dynamic con gravedad (se ve caer). |
| 5 | Decisión | Cualquier contacto cuenta, también golpearla desde abajo con la cabeza. |
| 6 | Decisión | Cae una vez por vida; tras 2 s de caída deja de simularse. |

### Archivos

- Nuevo: `Assets/Scripts/Traps/FallingPlatform.cs`.
- Nuevo: `Assets/Prefabs/Traps/FallingPlatform.prefab`.
  - Raíz: Rigidbody2D Kinematic, BoxCollider2D de 3×0,5 y FallingPlatform.
  - Hijo "Visual": SpriteRenderer Square naranja de 3×0,5, asignado a `visual`.
- Escena `Sandbox_Player`:
  - `FallingPlatform_Hueco` tapa el hueco (x -1..2) a ras de suelo.
  - `FallingPlatform_Elevada` en x 10..13, con la cara superior a 3 bloques del suelo.
- R1: `OnDisable` añadido en `PlayerMovement` y `PlayerJump` (ver Mecánicas 04 y 05).

### Pruebas

Unity no avanza con el Editor sin foco, así que los frames se avanzaron uno a uno con `EditorApplication.Step()` (Play Mode en pausa). El estado se registró cada ~0,1 s de tiempo de juego.

| Prueba | Resultado |
|---|---|
| Pisar FallingPlatform_Hueco | Contacto al aterrizar (t ≈ 0,04). Tiembla (desplazamiento de 0,01–0,04) hasta t ≈ 0,44. Luego pasa a Dynamic con el collider desactivado y cae. **El retardo medido es de ~0,4 s** (0,35 + granularidad de frames). |
| El jugador pierde el apoyo | Cae junto con la plataforma; la KillZone lo mata en t ≈ 1,06. |
| Reaparición | En t ≈ 1,62 (≈0,56 s tras morir) reaparece en LevelStart (-8,00; -2,99). La plataforma vuelve a y = -3,75, Kinematic, con collider y sin temblor. |
| Tocarla de lado (2.ª ronda) | OK. El jugador se empujó por física (velocidad 8 a la derecha, con `PlayerMovement` desactivado solo durante la prueba). Tocó el lateral en x = 9,50 (borde en 10), a y = -0,82, sin estar encima. La plataforma cayó **0,36 s** después, con el collider desactivado. |
| Sin temblor (2.ª ronda) | OK. Con `shakeBeforeFalling = false` (solo en esa sesión), el temblor máximo fue 0,000 y la plataforma cayó igual. |
| R1 (estado limpio tras reaparecer) | **No verificable de forma automática.** Con Unity sin foco, el Input System desactiva el teclado (`backgroundBehavior = ResetAndDisableNonBackgroundDevices`) y la tecla simulada no llega al juego (vx = 0 incluso con D "pulsada"). Probé además con el teclado activo en segundo plano (temporal) y tampoco llegó. El código del arreglo está aplicado; **pendiente de prueba manual**. |
| Consola | Sin errores CS ni excepciones del proyecto. |

Al salir de la prueba interrumpida dejé el Editor como estaba:
- Pausa desactivada.
- Configuración de entrada del Editor restaurada.
- Escena recargada desde disco, con el Player en (-8, -3) y sin cambios sin guardar.

**Método de prueba definitivo:** Unity salía solo de Play Mode entre comandos; en el log, justo antes, aparecían errores de licencia de Unity (`Licensing::Client ... 404`) y se restauraba la escena. Por eso cada prueba se lanza ahora en un solo comando, que hace todo seguido:
1. Entra en Play Mode.
2. Avanza los frames con `EditorApplication.Step()`.
3. Registra el resultado.
4. Restaura lo temporal.
5. Sale de Play Mode.

**Nota técnica:** el proyecto tiene "Enter Play Mode Options" **sin recarga de dominio**, así que los campos y eventos `static` no se reinician entre partidas en el Editor. Los scripts actuales se desuscriben en `OnDisable` y no guardan estado estático, pero cualquier estado `static` futuro (contador de muertes, etc.) tendrá que reiniciarse a mano, por ejemplo con `[RuntimeInitializeOnLoadMethod]`.

---

## Mecánica 06: Llave de nivel

**Estado:** implementada y probada (Play Mode autónomo con frames avanzados uno a uno).

### Decisiones

| # | Tipo | Decisión |
|---|---|---|
| 1 | GDD | La puerta está bloqueada hasta tener la llave. La llave desaparece del mundo al cogerla y se pierde al reaparecer en un checkpoint anterior. Puede activar cosas (troll). Se cubre el caso límite de cogerla y morir a la vez. |
| 2 | Decisión | La llave flota detrás del jugador (una de las dos opciones del GDD). Todavía no hay UI. |
| 3 | Decisión | "Checkpoint anterior a donde la recogió" = no se ha activado ningún checkpoint desde que la cogió. Como cada checkpoint se activa una sola vez, es equivalente y no hace falta ordenar los checkpoints. |
| 4 | Decisión | `onCollected` (UnityEvent) en el Inspector para los efectos troll. No reinicia esos efectos al morir; queda pendiente según el diseño de cada nivel. |
| 5 | Decisión | La puerta es sólida mientras está cerrada. Al tocarla con la llave se abre, gasta la llave y lanza `LevelCompleted`. `lethalWhenLocked` está desactivado por defecto (el GDD dice que "puede ser letal"). |
| 6 | Riesgo | Al completar el nivel todavía no se cambia de escena: el flujo entre niveles no está definido y no se inventa. Por ahora solo hay un evento y un mensaje en la consola. |
| 7 | Riesgo | El proyecto entra en Play Mode sin recargar el dominio. Los eventos estáticos se limpian bien porque todos se desuscriben en `OnDisable`, pero los contadores estáticos futuros tendrán que reiniciarse a mano. |

### Archivos

- Nuevos: `Assets/Scripts/Level/LevelKey.cs` y `Assets/Scripts/Level/GoalDoor.cs`.
- Nuevo: `Prefabs/Level/LevelKey`. Square amarillo de 0,5×0,5, con CircleCollider2D trigger y LevelKey.
- Nuevo: `Prefabs/Level/GoalDoor`. Square de 1×2 con:
  - BoxCollider2D sólido de 1×2, asignado a `blockingCollider`.
  - BoxCollider2D trigger de 1,3×2,1.
  - `spriteRenderer` asignado.
  - Como el objeto está escalado a 1×2, los tamaños de los colliders se pusieron en local.
- Escena `Sandbox_Player`:
  - LevelKey en (11,5; 0,1), encima de `FallingPlatform_Elevada`: al cogerla, la plataforma se cae.
  - GoalDoor en (14,5; -2,5), junto a la pared derecha y detrás de los pinchos, con `requiredKey` asignado.

### Pruebas

| Prueba | Resultado |
|---|---|
| Recoger la llave | Recogida, collider desactivado; la llave sigue al jugador con un desfase de (-0,79; 0,81) (objetivo: -0,8; 0,8). |
| Morir sin checkpoint nuevo | Al reaparecer, la llave vuelve a (11,50; 0,10) con su collider. OK. |
| Recogerla y morir en el mismo paso (pinchos movidos temporalmente) | Al reaparecer, la llave vuelve al pedestal. OK. |
| Recoger, tocar checkpoint y morir | Reaparece en el checkpoint (x = 7,80) **con la llave**. OK. |
| Puerta sin llave, con `lethalWhenLocked = true` (temporal) | Muere. OK. |
| Puerta sin llave, normal | Collider bloqueante activo y no se completa el nivel. **El empuje contra la puerta no se llegó a probar**: `PlayerMovement` anulaba la velocidad de prueba. |
| Puerta con llave | Se abre (bloqueante desactivado, color verde). La llave se consume (objeto desactivado). `LevelCompleted` = 1 y "Nivel completado." en la consola. |
| Volver a tocar la puerta abierta | `LevelCompleted` sigue en 1. OK. |
| `onCollected` | No verificado por separado (no tiene oyentes en la escena). |
| Consola | Sin errores CS ni excepciones del proyecto. |

Al terminar, el Editor quedó como estaba: fuera de Play Mode, sin pausa, entrada y física por defecto, y la escena sin cambios sin guardar.

---

## Mecánicas secundarias: cronómetro, contador de muertes y manchas

**Estado:** implementadas y probadas (Play Mode autónomo).

### Decisiones

| # | Tipo | Decisión |
|---|---|---|
| 1 | GDD | Cronómetro y contador de muertes dinámico en la interfaz. Mancha persistente en el punto exacto de cada muerte. |
| 2 | Decisión | Datos separados de la interfaz: `RunStats` cuenta y `StatsHUD` muestra (principio "un script, una tarea" del GDD). |
| 3 | Decisión | Formato del tiempo mm:ss.cc, pensado para speedrun. El cronómetro sigue corriendo mientras el jugador está muerto y se para al completar el nivel. |
| 4 | Decisión | "Dinámico" = el contador da un salto de escala cada vez que sube. |
| 5 | Decisión | Máximo de 200 manchas, reutilizando la más antigua, para no degradar el rendimiento con cientos de muertes (el GDD prevé contadores de cientos). |
| 6 | Decisión | Textos y manchas provisionales, a la espera de la dirección de arte. |
| 7 | Riesgo | `RunStats` cuenta por escena. El GDD (Datos y Persistencia) guarda `muertesTotales` y `tiempoJugado` acumulados en save.json; eso llegará con el sistema de guardado, que no está implementado. |
| 8 | Decisión | `RunStats` no es `static`, porque el proyecto no recarga el dominio al entrar en Play Mode. |
| 9 | Decisión | No se añadió EventSystem: el HUD no tiene elementos interactivos. |

### Archivos

- Scripts nuevos: `Assets/Scripts/Core/RunStats.cs`, `Assets/Scripts/UI/StatsHUD.cs` y `Assets/Scripts/Level/BloodStainSpawner.cs`.
- **TextMeshPro:** se importaron los "TMP Essential Resources" desde el paquete `com.unity.ugui` que ya estaba instalado (no es un paquete nuevo). Crean `Assets/TextMesh Pro`, con la fuente LiberationSans SDF. Durante la importación aparecen avisos de que sus shaders HDRP no son compatibles; son inofensivos en un proyecto URP.
- Prefabs nuevos:
  - `Prefabs/Effects/BloodStain`: sprite Circle por defecto de 0,6, rojo oscuro, sortingOrder 5 (entorno 0, jugador 10).
  - `Prefabs/UI/HUD`: Canvas Screen Space Overlay con CanvasScaler (Scale With Screen Size, 1920×1080), TMP "Timer" arriba a la izquierda, TMP "Deaths" arriba a la derecha (pivote centrado, para que el salto se vea bien) y StatsHUD en la raíz.
- Escena `Sandbox_Player`: objetos `RunStats` y `BloodStains` (con `stainPrefab` asignado) y una instancia de HUD con `stats` asignado.

### Pruebas

| Prueba | Resultado |
|---|---|
| Cronómetro | '00:00.04' al inicio y '00:01.06' tras ~1 s. Formato mm:ss.cc OK. |
| Contador | 'Muertes: 1' y luego 'Muertes: 3' tras tres muertes (Deaths = 3). |
| Salto del contador | Escala máxima 1,37 (objetivo 1,4; el muestreo es por frames) y vuelve a 1,00. |
| Manchas | Una por muerte, en (-5,-3), (6,-3) y (10,5,-3). Siguen ahí tras reaparecer. |
| Reutilización (maxStains = 3 temporal; se pidió 2) | La 4.ª muerte movió la mancha más antigua de (-5,-3) a (-12,-3), con 3 manchas en total. |
| Completar el nivel por la puerta | IsRunning = false; el tiempo se queda en 4,16 s (medio segundo después sigue en 4,16). |
| Consola | Sin errores del proyecto. |

**Sin captura del HUD:** las herramientas de captura disponibles renderizan la escena, no el Canvas en overlay.

---

## Cambio de diseño: altura del salto máximo (Decisión de Jesús)

| Tipo | Decisión |
|---|---|
| Decisión de Jesús (se aparta del GDD) | `maxJumpHeight` pasa de 4 a **3,2 bloques**. `shortJumpHeight` sigue en 2. Doble salto: leve+leve = 4, máximo+leve ≈ 5,2, máximo+máximo ≈ 6,4. |
| Origen | Capturas de referencia de otro juego que Jesús envió el 2026-10-26. Medidas en píxeles (con un bloque ≈ 29 px): toque ≈ 1,9, mantenido ≈ 3,2, mantenido+toque ≈ 4,9 y ambos mantenidos ≈ 6,3. |
| Recomendación | **Actualizar el GDD** (Mecánica 02 dice "Referencia de diseño: 4 bloques"). |
| Sandbox | Las líneas de referencia ahora están en 2, 3,2, 5,2 y 6,4. Los bloques invisibles de la Sandbox siguen teniendo sentido sin cambios: el Bajo (borde a 3,5) solo lo activa el salto máximo (cabeza ≈ 4,2) y no el leve (cabeza 3); el Alto (borde a 5,5) solo se activa con doble salto. |
| Reaparición con la tecla mantenida | Se queda como está: si se mantiene la dirección al reaparecer, el personaje se mueve (comportamiento habitual en plataformeros). |

---

## Nivel 1: El Despertar (`Assets/Scenes/Level_01.unity`)

**Estado:**
- Construido; es la primera escena de Build Settings.
- Recorrido validado con un simulador de física independiente y con pasadas automáticas en Unity.
- **Falta la prueba con teclado.**

### Decisiones

| # | Tipo | Decisión |
|---|---|---|
| 1 | Decisión | **Arte procedural provisional** generado por script en `Assets/Sprites` (pixel art a 32 px por unidad, filtro Point, sin compresión). Paleta del GDD: gris carbón, verde azulado, marrón rojizo, crema y rojo oscuro de peligro. Se sustituirá por el arte del equipo. |
| 2 | Decisión | **Tiles de 16×16 px (0,5 unidades)**, permitidos por el GDD ("16x16 o 32x32"), para poder hacer alturas de 1,5 y 2,5 con el nuevo salto de 3,2. Tilemap con `CompositeCollider2D`, para que no haya juntas. |
| 3 | Decisión | **Luces URP 2D:** global fría a 0,4, luz suave que sigue al jugador, rojas en los fosos, verde en el checkpoint, cian intensa en el portal y una cian muy débil como pista del troll 3. |
| 4 | Decisión | **Posprocesado:** Bloom, Vignette, Chromatic Aberration y Film Grain ligeros, para la estética de juego viejo con glitch. |
| 5 | Riesgo | El GDD (Iluminación) pide iluminación **plana sin sombras complejas**, propia de los 2000. Las luces puntuales y el posprocesado son una licencia estética que pidió Jesús. Si el equipo prefiere ser fiel al GDD, basta con subir la luz global a 1 y desactivar las demás luces y el Volume. |
| 6 | Decisión | **Portal de datos = `GoalDoor` sin llave** (`requiredKey` vacío = abierto desde el inicio). `GoalDoor.cs` se adaptó para eso. |
| 7 | Decisión | **Cámara propia (`CameraFollow`)**, sin Cinemachine, para no añadir paquetes. Tiene límites del nivel. |
| 8 | Decisión | **Narrador:** se hace hablar al Sistema con un evento estático (`Narrator.Say`). Las frases de muerte están en un ScriptableObject (`Data/Narrator/NivelUno_Burlas.asset`) y la UI escribe letra a letra como una terminal. |
| 9 | Decisión | **Frases:** solo la del inicio ("¿Te vas tan pronto? Apenas estábamos empezando...") sale del GDD (Enemigos y NPC). Todas las demás son **provisionales**, en el tono de "burla confiada" del Nivel 1. Algunas siembran pistas aprobadas del giro sin revelarlo: "Créeme: lo sé", "A todos nos pasa la primera vez" y "un niño de 2003". |
| 10 | Decisión | **Manchas antiguas grises** casi imperceptibles junto a varias trampas: es una pista aprobada en Premisa y Trama ("manchas del jugador anterior"). Una de ellas, sobre el pilar del troll 3, sirve también de pista. **Superada el 2026-11-03:** ver "Manchas de sangre: grises antiguas y rojas". |
| 11 | Decisión | ~~No hay plataformas que caen ni llave (el GDD las pone en el Nivel 2).~~ **Superada:** Jesús añadió la llave (2026-10-27) y una plataforma que cae en el Desafío (2026-10-30). |
| 12 | Pendiente | **Bugs lentos y coleccionable oculto** del Nivel 1: no tienen mecánica definida en el GDD, así que no se implementan. |
| 13 | Aviso GDD | Premisa y Trama dice "Superar el Nivel 1 y **conseguir la llave**", pero Niveles dice "cruzar el portal de datos". Se usó el portal. |
| 14 | Pendiente | La fuente es LiberationSans (TMP); Press Start 2P está pendiente porque no se añaden recursos externos sin aprobación. |

### Hallazgo de diseño (importante para los trolls)

Con velocidad horizontal constante, un bloque invisible encima de un foso **no puede matar si el salto leve es la solución**. Un salto máximo cortado por el bloque sigue una trayectoria igual o más alta que la del leve y llega igual de lejos. Comprobado por simulación: en el troll 1, de 20 saltos máximos que golpean el bloque, 0 mueren.

Por eso:
- El **troll 1 es un "susto"** que enseña que existen bloques invisibles sin castigar.
- El **troll 4 se rediseñó**: el portal está en una repisa 2,5 más alta, a la que solo se llega con el salto máximo, y el bloque corta ese salto.

### Mapa (x en unidades; suelo del inicio en y = 0)

| x | Zona | Contenido |
|---|---|---|
| 0–10 | Inicio | Seguro. Habla el Sistema (frase del GDD) y luego "Bienvenido a la demo...". |
| 10–16 | Zona A | Escalón de 1,5 (salto leve). |
| 16–24 | Zona A | Muro de 2,5 (salto máximo). |
| 24–27 | Zona A | Foso de pinchos de 3. **Troll 1** (susto): bloques invisibles en x 24,5–26,5 con el borde a y = 7,4; solo los golpea el salto máximo. |
| 33–41 | Doble salto | Muro de 5 (solo con doble salto), con su frase. |
| 47–68 | Desafío | Campo de pinchos con plataformas estáticas en 49–51, 54–56, 59–61 (+1) y 64–66. |
| 68–76 | Desafío | **Troll 2:** checkpoint falso en x 72,5 (mata), anunciado por el Sistema. |
| 76–97 | Desafío | **Troll 3:** hueco de 21 imposible con doble salto. Hay que bajar al pilar (79–83, y = 1), golpear desde abajo el bloque invisible "ayudante" (83,5–85,5, borde a y = 4), subirse encima con doble salto y saltar a la orilla. Pistas: mancha antigua en el pilar y un brillo cian débil. |
| 97–108 | Zona B | Checkpoint real (x 100,5) y descanso. |
| 108–112 | Final | Foso de pinchos. |
| 112–121 | Final | Pinchos en 114–116. |
| 121–124 | Final | **Troll 4:** foso, repisa del portal a y = 6,5 y bloques invisibles en 122–124 (borde a 7,4). El Sistema anima a "saltar con todas tus fuerzas". |
| 124–134 | Final | Portal de datos en x 130 y despedida del Sistema ("Eso no debía pasar..."). |

### Validación

**Simulador independiente** (réplica de PlayerMovement y PlayerJump en Python, con el jugador como caja de 1×1 y siempre corriendo a la derecha, que es el peor caso):
- Todos los saltos obligatorios son posibles con varios instantes de despegue válidos.
- El muro de 2,5 no se sube con el leve, y el de 5 solo con doble salto.
- **Troll 3 sin descubrir el bloque:** ninguna combinación de doble salto llega a la otra orilla, ni desde la orilla izquierda ni desde el pilar. La solución prevista funciona.
- **Troll 4:** sin bloque, el salto máximo llega en 33 de 60 instantes de despegue; con bloque, en 9. Máximo + leve llega en 171 combinaciones, así que hay solución aprendible.

**Pasadas automáticas en Unity:**
- El Sistema habla al empezar.
- El checkpoint falso mata y lanza una burla ("Muerte registrada...").
- El checkpoint real se pone verde y se reaparece en él (101,3; 3,5).
- La cámara sigue al jugador.
- El portal completa el nivel.
- 0 errores en consola.

**Errores encontrados y corregidos durante la construcción:**
- El collider del Tilemap no se había generado (el jugador atravesaba el suelo al reaparecer). Se regeneró.
- La referencia a las burlas se guardó vacía. Se reasignó.
- El perfil de posprocesado estaba vacío (sus efectos no se guardaron como sub-assets). Se rehízo.

**No probado:**
- La partida completa con teclado.
- La sensación de cada troll.
- La legibilidad del texto del Sistema en pantalla.
- El revelado del troll 4 dentro de Unity (solo está validado en el simulador; la mecánica del bloque se probó en la Sandbox).

### Llave del Nivel 1 (añadida el 2026-10-27)

| Tipo | Decisión |
|---|---|
| Decisión de Jesús | **Llave en el Nivel 1, antes del checkpoint**, encima del bloque invisible "ayudante" del troll 3 (84,5; 5,9). Resuelve la contradicción del GDD a favor de "1 llave por nivel" (Mecánica 06, Objetos, Premisa). El portal de datos **requiere la llave**. |
| Decisión | Solo se coge de pie sobre el bloque ayudante revelado. También se puede rozar en el aire, cayendo desde arriba, pero sin el bloque no hay forma de cruzar el hueco, así que obliga igualmente a resolver el troll 3. Si mueres antes de tocar el checkpoint, la llave vuelve a su sitio y el bloque se oculta: hay que repetir el truco. |
| Decisión | Portal cerrado = sprite oscurecido (gris azulado) y collider bloqueante; abierto = color normal. |
| Decisión (2026-10-27) | `GoalDoor` tiene dos campos opcionales nuevos. **`unlockedOnlyEffect`** se enciende solo mientras la puerta se puede cruzar; se actualiza en cada frame porque la llave puede cogerse y perderse en cualquier momento. **`completedLine`** es la frase del Sistema al completar el nivel. En Level_01 el primero apunta a la luz cian del portal. |
| Decisión | Despedida provisional: "...Eso no debía pasar. Bueno. El siguiente nivel no será tan amable." Va en la línea del desconcierto que pide Premisa y Trama al completar el Nivel 1, sin mostrar debilidad. |
| Decisión | Frase nueva al coger la llave: "¿Una llave? Cuídala bien... si te mueres, se queda aquí conmigo." |
| Decisión | La frase junto al portal pasó a ser neutral y repetible: "La puerta solo se abre con su llave. ¿La traes... o la dejaste por ahí?". Sustituye a la despedida "Eso no debía pasar", que ya no encajaba, porque se puede llegar al portal sin llave. |
| Pruebas | Luz del portal: apagada sin llave, encendida al cogerla, apagada al perderla por morir antes del checkpoint y encendida al recuperarla. Al cruzar el portal con la llave, el nivel se completa y el Sistema dice la despedida. 0 errores en consola. |

Pruebas (Play Mode autónomo; `PlayerMovement` desactivado solo para empujar al jugador por física):

| Prueba | Resultado |
|---|---|
| Portal sin llave, empujando contra él | Se queda en x = 129,00 (frenado por la cara del portal) y no completa el nivel. OK. |
| Coger la llave | Recogida; la llave sigue al jugador y sale la frase del Sistema. OK. |
| Morir antes del checkpoint | Reaparece en el inicio; la llave vuelve a (84,5; 5,9) con su collider y el bloque ayudante se oculta. OK. |
| Coger, tocar el checkpoint y morir | Reaparece en el checkpoint (101,3; 3,51) con la llave. OK. |
| Portal con llave, empujando | Lo cruza (x 131,8), se completa el nivel, la llave se consume y el portal se aclara. OK. |
| Consola | 0 errores. |
| Revelar el bloque ayudante con un impulso simulado | No concluyente en Unity (el impulso no se aplicó bien). La mecánica del bloque está probada en la Sandbox y el recorrido del troll 3, en el simulador. |

### Plataforma que cae en el Desafío (añadida el 2026-10-30)

| Tipo | Decisión |
|---|---|
| Decisión de Jesús | La plataforma alta del campo de pinchos pasa a ser una **plataforma que cae** (Mecánica 07). Ocupa x 59–61, y 3–4. Jesús lo aprobó aunque el GDD pone estas plataformas en el Nivel 2 ("total es una demo"). Anula en parte la decisión 11. |
| Interpretación | "Que se caiga la primera vez, con tiempo para reaccionar": cae **siempre**, también la primera vez, pero avisa temblando antes. Al reaparecer, vuelve a su sitio. |
| Decisión | `fallDelay` empezó en 0,5 s, el máximo del GDD. **Cambio de Jesús (2026-10-30): bajado a 0,35 s** porque tardaba demasiado en caer. Sigue dentro del rango del GDD (0,2–0,5) y es un override de la instancia; el prefab base no cambia (0,35). El temblor está activo. Se editó directamente en `Level_01.unity` con Unity cerrado; al abrir Unity se comprobó que cargó bien. |
| Cambio de Jesús (2026-10-30) | "Que caiga más rápido": `fallGravityScale` pasa de 3 a **6**, como override de la instancia (el prefab base sigue en 3). Bajar los 3 bloques hasta los pinchos tarda ahora ~0,32 s; antes eran ~0,45 s. |
| Implementación | Se quitaron las 8 celdas del Tilemap `Platforms` (celdas x 118–121, y 6–7). En su lugar hay una instancia `Level/Traps/FallingPlatform_Desafio` del prefab `Prefabs/Traps/FallingPlatform` en (60; 3,5). Overrides: collider de 2 × 1; `Visual` con el sprite `Tile_Platform` en modo Tiled de 2 × 1, escala 1 y color blanco. Se ve igual que antes (captura de escena idéntica). |
| Aviso técnico | Al quitar tiles **por script**, el `CompositeCollider2D` del Tilemap no se regeneró: seguía habiendo un collider invisible y el jugador flotaba. Se arregló desactivando y reactivando el `TilemapCollider2D` y llamando a `GenerateGeometry()`. Pintando a mano con la Tile Palette se actualiza solo. |

Pruebas (Play Mode automático, colocando al jugador encima):

| Prueba | Resultado |
|---|---|
| Aterrizar encima | Con 0,5 s: tiembla y cae a los **0,49 s** del aterrizaje. Con 0,35 s y gravedad 6: cae a los **0,34 s**, llega a los pinchos en 0,32 s y vuelve a (60; 3,5) con el collider activo al reaparecer. 0 errores. |
| Pierde el apoyo | El jugador cae con ella y los pinchos lo matan en (60; 0,68). |
| Reaparecer | El jugador vuelve al inicio y la plataforma, a (60; 3,5) con el collider activo. |
| Segunda vez | Vuelve a caer. |
| Plataformas vecinas | Siguen siendo sólidas (54–56 y 64–66). No queda una plataforma doble (tile + prefab). |
| Consola | 0 errores. |
| No probado | La partida con teclado y si los 0,5 s se sienten justos. |

### Manchas de sangre: grises antiguas y rojas (actualizadas el 2026-11-03)

| Tipo | Decisión |
|---|---|
| GDD | Premisa y Trama: "Manchas de muerte antiguas, grises y menos notorias que las de El Novato", "Casi imperceptibles en el Nivel 1; aumentan en cada nivel". Son decoración y pista ("Son las muertes del jugador anterior"), sin comportamiento propio. Mundo y Registro de Decisiones dicen lo mismo. |
| Decisión de Jesús (se aparta del GDD) | Las grises deben estar donde el jugador moriría normalmente o en las trampas troll, y ser **más notorias**. Esto se aparta del "casi imperceptibles" del GDD; conviene que el equipo actualice ese texto. Siguen siendo grises y menos notorias que las rojas. |
| Decisión | Prefab `Prefabs/Effects/OldBloodStain`: gris (0,5; 0,5; 0,52), opacidad 0,75, sin collider ni script. No lo crea `BloodStainSpawner` y no suma en `RunStats`. Se coloca a mano; cada instancia elige variante, giro y volteo. |
| Decisión | Gris **claro**, no oscuro: sobre los tiles oscuros, un gris más oscuro se perdía. Las dos manchas usan `Sprite-Unlit-Default`, porque con la luz global a 0,4 se apagaban. Las rojas pasan a color (0,72; 0,07; 0,09), opacidad 0,95. |
| Decisión | 4 sprites nuevos en `Sprites/Effects/Stain_01..04.png`: mancha irregular, regueros y gotas, borde oscuro y brillo. Están en escala de grises y el color lo pone el `SpriteRenderer`. Se regeneran desde el menú **Unknown Game > Regenerar sprites de manchas** (`Scripts/Editor/BloodStainSpriteGenerator.cs`). |
| Decisión | `BloodStainSpawner`: elige variante, volteo y escala (0,85–1,15) al azar. El giro es en pasos de 90°, no libre, para que los píxeles sigan alineados. La mancha sigue en el punto exacto de la muerte, como pide el GDD. |
| Decisión | La mancha del pilar del troll 3 se quitó porque ahí no se muere. La pista pasa a `Troll3_Ayudante`, en los pinchos justo debajo del bloque ayudante. El brillo cian se mantiene. |
| Decisión | No se ponen manchas en `KillZone`: está fuera de pantalla (y = −9). `Sandbox_Player` no lleva grises, pero su spawner usa las variantes nuevas. |
| Pendiente | Cuántas manchas más llevarán los niveles 2 y 3: el GDD solo dice que aumentan. Con 16 en el Nivel 1, habrá que poner más allí. |

Manchas grises de `Level/Deco` (nombre `ManchaAntigua_*`):

| Trampa | Instancia | Posición |
|---|---|---|
| Troll 1, foso con bloque invisible | `Troll1_A`, `Troll1_B` | (24,8; 2,9), (26,2; 2,9) |
| Desafío, campo de pinchos | `Desafio_A`, `Desafio_B` | (52,5; 0,4), (57,5; 0,4) |
| Plataforma que cae | `PlataformaCae_A`, `PlataformaCae_B` | (60; 0,4), (60,8; 0,4) |
| Troll 2, checkpoint falso | `Troll2_A`, `Troll2_B` | (72; 3,15), (73,1; 3,15), aplastadas contra el suelo |
| Troll 3, hueco | `Troll3_A`, `Troll3_Ayudante`, `Troll3_B` | (77,5; 0,4), (84,5; 0,4), (90; 0,4) |
| Final, foso | `Final_A`, `Final_B` | (109,5; 1,4), (111; 1,4) |
| Final, pinchos altos | `Final_Pinchos` | (115; 4,4) |
| Troll 4, foso | `Troll4_A`, `Troll4_B` | (122; 2,4), (123,1; 2,4) |

Pruebas (Play Mode automático, moviendo al jugador por código): 6 muertes seguidas en los pinchos dan `deaths` = 6 y 6 manchas rojas con variantes, volteo, giro y escala distintos, en el punto de la muerte. Las 16 grises siguen igual y no tienen colliders. 0 errores ni avisos en consola. No probado: la partida con teclado ni cómo se ven en movimiento.

### Archivos nuevos

- **Scripts:**
  - `Assets/Scripts/Core/CameraFollow.cs`
  - `Assets/Scripts/Narrator/Narrator.cs`, `NarratorLines.cs`, `NarratorTrigger.cs`, `NarratorDeathTaunts.cs` y `NarratorUI.cs`
- **Modificado:** `GoalDoor.cs` (llave opcional).
- **Sprites:** `Assets/Sprites/Environment/` (Tile_Metal, Tile_Platform, BG_Panels, Checkpoint_Flag, Portal_Datos, Splat y Tiles/*.asset), `Assets/Sprites/Traps/` (Spikes, InvisibleBlock_Revealed) y `Assets/Sprites/Player/Novato.png`.
- **Prefabs nuevos:** `Prefabs/Traps/FakeCheckpoint` y `Prefabs/Level/DataPortal`.
- **Prefabs actualizados al arte nuevo:** Player, Spikes, Checkpoint, InvisibleBlock, BloodStain y DeathParticles (color).
- **Datos y ajustes:** `Assets/Data/Narrator/NivelUno_Burlas.asset` y `Assets/Settings/Level01_PostFX.asset`.

---

## Nivel 1 fusionado: etapas 1 y 2 (2026-11-03)

**Estado:** nivel completo (ver también "Nivel 1 fusionado: etapa 3"). Falta la prueba con teclado. Las secciones "Nivel 1: El Despertar" y "Diálogos del Sistema – Nivel 1" describen el nivel anterior, guardado en `Scenes/Archive/Level_01_v1.unity`.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Fusión de los dos recorridos de Figma en un único Nivel 1, por etapas. Mecánica 08 (pinchos que caen). Sin coyote time, Bugs ni coleccionable oculto. Descartados: pinchos que se acercan, caja sorpresa y plataforma que empuja. |
| [Decisión] | `FallingSpike`: el objeto raíz es la franja de activación y un hijo con su propio `Rigidbody2D` y `LethalHazard` es el que cae, para que tocar la franja no mate. Al tocar suelo se desactiva y deja el camino libre. |
| [Decisión] | Valores de cada pincho: temblor 0,3 s, gravedad 3 (la del jugador) y franja de 1 × 6. Distancia de activación (`triggerOffsetX`): reto 8 = 3,5; reto 9 = 3,8; reto 10 = 4,3. Se descartó 3: frenando 0,25 s tarde, el jugador se detiene justo debajo y muere. |
| [Decisión] | Reto 7: bloque aparte y seguro, porque `InvisibleBlock` se oculta al morir. La repisa de 5,5 también se alcanza sin el bloque con máximo + máximo (6,4); el bloque lo facilita. |
| [Decisión] | Reto 11: mismo patrón que el antiguo Troll3_HelperBlock. El hueco mide 20 porque con doble salto se alcanzan unas 12,7 unidades: desde el pilar hay 14 hasta la orilla y sin el ayudante no se llega; desde el ayudante hay 11,5. |
| [Decisión] | Reto 16: `FallingPlatform` de 4 de ancho que cierra el pozo. Lleva el portal apagado como hijo, así que cae con ella. El jugador cae 9 unidades, hasta y = -3, sin morir: no hay daño por caída y el suelo no se atraviesa. |
| [Decisión] | Las `FallingPlatform` usan 0,35 s y gravedad 6, los valores ya aprobados. |
| [Decisión] | Reto 12: si el jugador salta justo debajo, choca con la cabeza y la plataforma cae igual (cualquier contacto la activa, como pide el GDD). Es suelo seguro, así que solo muestra cómo cae. |
| [Decisión] | La copia `Scenes/Backups/Level_01_Backup_2026-11-04.unity` sigue en el proyecto y es idéntica a la de `Archive`. Se puede borrar a mano. |
| Ajuste frente al plan | Por el hueco del reto 11, lo siguiente se desplaza algo: checkpoint en x 155 (plan ≈150), islas x 160–187 (plan ≈155–185), salida falsa x 194–198 (plan ≈188–196). La transición acaba en x 215, como en el plan. |
| [GDD] | Frase de inicio "¿Te vas tan pronto?...". Las demás frases son provisionales; dos son nuevas: "Mira hacia arriba de vez en cuando, Novato. O no." y "¿La salida? Ah, eso. Lleva años sin funcionar." |

### Disposición (x en unidades; 1 unidad = 1 bloque)

| Reto | x | Contenido |
|---|---|---|
| Inicio | 2 | Suelo y = 0, pared izquierda. Frases `T_Inicio_GDD` y `T_Demo` (x 6). |
| 1 | 8–14 | Escalón de 1,5 (salto leve). |
| 2 | 14–22 | Muro hasta 4: +2,5 (salto máximo). |
| 3 | 22–26 | Foso de 4 con pinchos, base y = 2,5. |
| 4 | 32–40 | Muro hasta 8,5: +4,5 (doble salto). Frase `T_DobleSalto` (x 29). |
| 5 | 40–56 | Bajada: plataformas 42–45 (y 6) y 46,5–49,5 (y 3), sin peligro. |
| 6 | 56–60 | Foso de 4 con pinchos, base y = -1,5. Bloques invisibles en x 57, 58 y 59, borde a 3,4 (solo los toca el salto máximo). Frase `T_Reto6` (x 53). |
| 7 | 60–77 | Bloque en (67,5; 2,6) con pista de luz. Muro-repisa x 70–77 hasta 5,5. |
| 8–10 | 79–103 | Pasillo con techo en y = 6. `FallingSpike` en x 87, 94 y 101. Frase `T_Reto8` (x 80). |
| 11 | 104–124 | Hueco con pinchos, base y = -3. Pilar x 107–110 (y -2). Ayudantes en x 111 y 112, y = 2, con pista de luz. Frase `T_Reto11` (x 103). |
| 12 | 129–132 | `FallingPlatform` de 3, parte superior 1,7, sobre suelo seguro; muro x 134–140 hasta 4. |
| 13 | 140–152 | Foso de 12 con pinchos y luz roja. `FallingPlatform` x 145–147, parte superior 4; huecos de 5. Frase `T_Reto13` (x 137). |
| 14 | 155 | Checkpoint 1 sobre pedestal x 154–157 (y 4,5), luz verde; reaparece en (155,8; 5). Frase `T_Checkpoint` (x 153). |
| 15 | 160–190 | Pinchos. Islas: 163–165 estática, 169–171 cae, 174–176 estática, 180–182 cae, 185–187 estática. Huecos de 4 como máximo. |
| 16 | 194–198 | Salida falsa, parte superior 6, portal apagado encima. Frase `T_SalidaFalsa` (x 192). |
| Transición | 198–215 | Pasillo inferior (suelo y = -3, techo y = 2) con techo roto, ladrillos sueltos, manchas y luces rojas. `_Temporal_FinEtapa2` en x 215–217. |

Apartados: `Level/_Pendiente_Etapa3` (desactivado) contiene `LevelKey` y `DataPortal`. Cámara: límites (-2; -8) a (219; 20). KillZone en y = -9, 240 de ancho, por debajo de todo. Manchas grises: 20 en `Level/Deco`.

### Pruebas (Play Mode automático, sin teclado)

| Prueba | Resultado |
|---|---|
| Pinchos, correr a velocidad máxima | Pasa vivo en los retos 8, 9 y 10. |
| Pinchos, frenar al empezar a temblar o 0,25 s después | Queda vivo en los tres. |
| Pinchos, avanzar a saltos leves a velocidad máxima | Vivo en 8 y 9. **En 10 muere** si está en el aire justo debajo; es el "si dudas, te alcanza" del plan, y el pincho tiembla antes. |
| Recorrido hasta la pared temporal | Completo con saltos de física reales (velocidad y salto aplicados por código, con los valores del juego), sin morir. Llega a x = 214,5. |
| Salida falsa | El jugador baja a y = -2,49 sin morir. |
| Reaparición | En el inicio (2; 0,51) antes del checkpoint y en (155,8; 5,01) después. |
| Al reaparecer | Las 5 `FallingPlatform` y los 3 pinchos vuelven a su sitio; los bloques se ocultan. |
| Consola y referencias | 0 errores, 0 avisos y 0 referencias rotas en `Level_01`. |

**No probado:**

- La partida con teclado ni la duración real.
- Subir de verdad encima de los bloques de los retos 7 y 11: en el recorrido coloqué al jugador encima por código. Por cálculo: reto 7, máximo + leve (5,2) frente a 3,1; reto 11, 5,2 frente a 4,5 desde el pilar.
- Si los pinchos colgados y los ayudantes se distinguen bien en pantalla.

---

## Nivel 1 fusionado: etapa 3 (2026-11-04)

**Estado:** secciones 4 y 5 construidas en `Level_01`; el nivel va de x 0 a x 387 y se completa por el portal. Falta la prueba con teclado.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Mecánica 09 (pared falsa), con ficha separada para el GDD. Primero la puerta y la burla del Sistema, con la llave visible en una repisa; se consigue con la pared falsa y el doble salto. Texto de la broma, de Jesús. |
| [GDD] | Llave de nivel y su regla de pérdida (Mecánica 06), checkpoint falso (Mecánica 05), portal como salida. |
| [Decisión] | `FakeWall`: sprite Tiled sin colisión sólida, con un trigger. Alfa 0,4 dentro, fundido de 0,15 s, vuelve a opaca al salir o al reaparecer. Se dibuja por delante del jugador. Grieta como hijo (`Sprites/Environment/Grieta_Overlay.png`) y mancha gris en la base. Su tamaño se ajusta por instancia. |
| [Decisión] | Narrador: `NarratorTrigger` admite `sequence` (frases con segundos; texto vacío = pausa) y `fullOnlyFirstTime`. `NarratorUI` descarta las frases sueltas, burlas incluidas, mientras suena una secuencia. Los triggers de una sola frase siguen igual. |
| [Decisión] | Broma de la puerta: 8,2 s medidos. Se repite como frase vacía, es decir, nada. No bloquea el control. |
| [Decisión] | `GoalDoor` tiene un campo nuevo, `lockedOnlyObject`, para el candado. `unlockedOnlyEffect` no servía: enciende al abrir y solo admite `Behaviour`. |
| [Decisión] | Puerta: collider sólido reducido por override a 1 × 1,2 (0,8 menos que el sprite). Así, ni desde lo alto de la puerta se llega a la repisa con doble salto (8,6 frente a 9). Se salta por encima. |
| [Decisión] | Reto 17, "puente" de bloques invisibles: dos veces el patrón del reto 11 (pilar + bloque + salto). Un bloque invisible solo se revela desde abajo, así que no puede ser un puente plano seguido. |
| [Decisión] | Reto 17: el pilar sigue por debajo del bloque, porque al golpear la esquina del bloque el jugador se desplazaba y caía a los pinchos (detectado en la prueba automática). |
| [Decisión] | `DeadlyRock`: prefab nuevo con `LethalHazard` y el sprite `Roca_Mortal`. No es una mecánica nueva. |
| Corrección | Reto 11: mismo fallo que en el reto 17. El pilar pasa a x 107–111, por debajo del ayudante, y se quita el pincho de x 110,5. La orilla sigue fuera de alcance sin el ayudante (13 frente a 11,8). Se puede deshacer. |
| Ajuste frente al plan | Con los alcances reales, la sección 4 acaba en x 360 (plan ≈310) y la 5 en x 387 (plan ≈340). Checkpoint 2 en x 353,5 (plan ≈305). |
| Ajuste frente al plan | Reto 25: el escalón está a +2,5 (plan ≈3) y la abertura a +5,5 desde él (plan ≈5), para dejar margen: requiere máximo + máximo. |
| Ajuste frente al plan | La llave se baja a y = 9,4 (repisa en y = 9) para que se vea entera desde la puerta. |
| Pendiente | Actualizar el GDD con las fichas de las mecánicas 08 y 09. Bugs lentos y coleccionable oculto siguen sin implementar. |

### Disposición

| Reto | x | Contenido |
|---|---|---|
| 17 | 222–259 | Puente: pilares x 225–229 y 242–246 (y -5), bloques en x 229–230 y 246–247 (y -1) con pista de luz y manchas; pinchos con base y = -6. |
| 18 | 264 | Checkpoint falso. Frase `T_Reto18` (x 259). |
| 19 | 273–286 | Repisa sin salida x 273–277 (y 0). `Reto19_FakeWall` x 282–286, y -3 a 0, con grieta y mancha. Frase `T_Reto19` (x 272). |
| 20 | 286–304 | Cámara en U, suelo y = -6, techo y = 0 hasta x 300. `FallingSpike` en x 292 (activación 3,5) y x 298 (3,8). Doble salto en x 300–304, a la cornisa y = -1,5. |
| 21 | 310–333 | 4 `FallingPlatform` de 2 (0,35 s, gravedad 6) en x 313–315, 318–320, 323–325 y 328–330, sobre pinchos. |
| 22 | 338–349 | Foso de 11. Rocas en x 344,5 y 346, y = 5,6, colgando de una losa. Pasa máximo + leve; máximo + máximo roza las rocas. |
| 23 | 353,5 | Checkpoint 2 sobre pedestal, luz verde; reaparece en (354,3; -0,5). |
| 24 | 360–363 | Troll 4: foso con pinchos y bloques en x 361,5 y 362,5 (borde a 1,9). Frase `T_Reto24` (x 356). |
| Puerta | 372 | `DataPortal` sobre el suelo y = 1, con candado. Broma `T_Puerta` en x 368. |
| Llave | 375 | `LevelKey` en y = 9,4 sobre la repisa x 373–380 (y = 9). Frase `T_Llave`. |
| 25 | 380–385 | `Reto25_FakeWall` x 380–382, y 1 a 4. Hueco x 382–385, escalón x 384–385 (y 3,5), abertura a la repisa en y 9–11. Pared final en x 385–387. |

Ambiente: 39 manchas grises en total y luces rojas en toda la sección 4. Cámara hasta x 387. KillZone de 400 de ancho en y = -9.

### Pruebas (Play Mode automático, sin teclado)

| Prueba | Resultado |
|---|---|
| Pared falsa en `Sandbox_Player` | Se atraviesa sin frenar ni atascar, y = 0,51 constante; alfa 0,4 dentro, 1 fuera y tras reaparecer. |
| Recorrido completo, inicio a portal | Repetido con la llave y el reto 11 en su posición final: sin morir, se completa el nivel en x = 370,9 y suena la despedida. Unos 60 s de script, más 9 s parado ante la puerta; no es la duración de una persona. |
| Pinchos reto 20 (correr, frenar, frenar 0,25 s tarde, a saltos) | Vivo en todos. Con el doble salto tras los pinchos llega a la cornisa. |
| Morir antes del CP1 | Reaparece en (2; 0,51). |
| Morir al tocar el CP1 (caso límite) | Reaparece en el CP1 (155,8; 5). |
| Morir entre CP1 y CP2 | Reaparece en el CP1. |
| Checkpoint falso | Mata y no cambia el punto de reaparición. |
| Morir en la sección 5 sin llave | Reaparece en el CP2 (354,3; -0,5). |
| Puerta sin llave | Frena en x = 371, no completa el nivel, candado visible, suena la broma. |
| Segunda visita a la puerta | La broma no vuelve a empezar. |
| Coger la llave | Candado oculto y luz encendida. |
| Morir con la llave antes de cruzar | La llave vuelve a (375; 9,4). |
| Morir al coger la llave (caso límite) | La llave se queda en la repisa. |
| Salida falsa | Baja al pasillo y no completa el nivel. |
| Narrador | Una burla y una frase durante la secuencia no la cortan; una frase simple corta a otra y dura 2,9 s, como antes; tras la secuencia vuelve a funcionar. |
| Vista desde la puerta | Con la llave en y = 9,4 y el jugador en x = 370, a 1920 × 1080: la llave se ve entera (y 995–1072 px) y las tres líneas de la broma no la tapan (texto y 909–960 px). Capturas revisadas. |
| Consola y referencias | 0 errores, 0 avisos y 0 referencias rotas en `Level_01`. |

**No probado:**

- La partida con teclado ni la duración real.
- Subir encima de los bloques de los retos 7, 11 y 17: en el recorrido coloqué al jugador encima por código. Por cálculo sí llega.
- El reto 24 solo se pasó saltando con el salto máximo desde x = 358, sin revelar los bloques. La solución alternativa no se probó.
- Si los pinchos, las rocas y la grieta se distinguen bien mientras se juega. La llave es un cuadrado amarillo provisional: se ve, pero quizá no se lea como llave.

---

## Nivel 1: rediseño troll (2026-11-04)

**Estado:** `Level_01` reconstruido, de x 0 a x 331, y guardado. Sustituye a la disposición de las etapas 1 a 3, cuyas x ya no son válidas. Falta la prueba con teclado. La versión anterior queda en `Scenes/Archive/Level_01_v2.unity`.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Rediseño para hacer el nivel más troll usando solo lo que ya existe. No se aprueban mecánicas nuevas: ni `TrapTrigger`, ni `PopSpike`, ni suelo falso como script, ni checkpoint con evento. Se pueden proponer más adelante. |
| [Aprobado por Jesús] | Primer checkpoint tras la salida falsa = `FakeCheckpoint` con la frase del Sistema "¡Mira, un punto de control! Tócalo, es gratis." Mata y devuelve al inicio. |
| [Decisión] | Sin scripts nuevos ni cambios en scripts o prefabs base. Todo son instancias con overrides de `FallingPlatform`, `FallingSpike`, `InvisibleBlock`, `FakeWall`, `Spikes`, `DeadlyRock`, `Checkpoint`, `FakeCheckpoint`, `LevelKey`, `GoalDoor` y `NarratorTrigger`. |
| [Decisión] | El "suelo que cede" es una `FallingPlatform` con sprite de metal, sin temblor y con retardo corto. El "suelo falso" no existe como mecánica: es esto, sobre pinchos. |
| [Decisión] | Islas de 1 de ancho (S2, S4): 0,3 s en lugar de 0,2–0,25. Sin jump buffer ni coyote time, con 0,2 s eran casi imposibles. |
| [Decisión] | Dos checkpoints reales y uno falso: `S3_CheckpointFalso` x 160, `Checkpoint` x 172 y `Checkpoint2` x 259,5. Se quitó el falso antiguo. |
| [Decisión] | Los pilares de los bloques invisibles siguen por debajo del bloque, porque al golpear la esquina el jugador se desplazaba y caía a los pinchos. |
| Omitido | R27, pincho en la vuelta a la puerta: se omite. |
| Riesgo | Inicio → checkpoint falso son unos 160 u, no 125. Morir ahí devuelve al inicio. No se añaden checkpoints; lo decide Jesús tras jugarlo. |

### Disposición (x en unidades)

| Sección | x | Retos |
|---|---|---|
| S1 demo | 0–52 | Foso 14–20 con `S1_SueloQueCede_A/B` (0,25 s) sobre pinchos. Bloque invisible `S1_Bloque_Muro` x 25. Muro +2,5 en x 26. Foso 34–40, con `S1_SueloTrasFoso` x 39 (0,2 s) justo en la llegada. Descanso 40–52 con frase de "aquí no pasa nada". |
| S2 | 52–154 | Ruta falsa 52–61 con pinchos de pared en x 61,75. `S2_FakeWall` x 64 (la salida real está abajo). Islas 66–85: plataforma fija x 70–71, `S2_Isla_A/B` x 75,5 y 80,5. Muro x 96–100 con atajo y ruta de arriba: ver "Muro con atajo". Pasillo 100–118 con `S2_Pincho_A` x 108 (activa a 3,5), pinchos 110–112 y `S2_Pincho_B` x 113 (3,8). Hueco 118–138: pilar 121–125 + `S2_Ayudante_A/B` x 125–126. Muro 146–152. `S2_SalidaFalsa` x 154 (0,35 s con temblor), con portal apagado. |
| S3 | 152–256 | `S3_CheckpointFalso` x 160, `Checkpoint` x 172 (reaparece en 172,8; -2,49). Foso 181–204: pilar 184–188 + `S3_Ayudante_A/B` x 188–189; `S3_SueloTrasSalto` x 203 (0,25 s). Túnel 206–218: pinchos 209–211, `S3_Roca` (210; 1,4), `S3_Pincho` x 215 (3,8). Plataforma señuelo 222–226. `S3_FakeWall` x 234. Cadena 236–256: `S3_Cadena_A/B/C` x 240, 245 (0,25 s) y 250 (0,2 s). |
| S4 | 256–304 | `Checkpoint2` x 259,5 (reaparece en 260,3; -1,99). Foso de 11 x 264–275 con rocas (269,5; 4,1) y (271; 4,1). Islas 279–298: fijas x 282–283 y 290–291, `S4_Isla_A/B` x 286,5 y 294,5. |
| S5 | 304–331 | Foso 304–307 con `S5_Bloque_A/B` x 305,5 y 306,5. `DataPortal` x 316. `S5_FakeWall` x 325, escalón x 328–329, muro x 324–326. `S5_RepisaLlave` x 319,5 (ancho 5, 0,35 s) y `LevelKey` (319; 7,9): la repisa se cae al cogerla. |

### Frases del Sistema (orden de juego)

Sustituye a la tabla de la sección "Diálogos del Sistema". Todos son `NarratorTrigger` bajo `NarratorTriggers`.

| GameObject | x | Frase | Origen |
|---|---|---|---|
| `T_Inicio_GDD` | 2 | ¿Te vas tan pronto? Apenas estábamos empezando... | GDD |
| `T_Demo` | 6 | Bienvenido a la demo, Novato. Hasta un niño la termina. | Provisional |
| `T_Suelo` | 21 | ¿Ves? Solo era el suelo. Le puede pasar a cualquiera. | **Nueva, provisional** |
| `T_Descanso1` | 44 | Descansa, Novato. Aquí no pasa nada. Lo prometo. | **Nueva, provisional** |
| `T_ParedesFalsas` | 67 | Las paredes de este juego son muy sólidas. Casi todas. | **Nueva, provisional** |
| `T_DobleSalto` | 87 | ¿Una pared? Qué pena que solo sepas saltar una vez... ¿o no? | Provisional |
| `T_Atajo` | 99,4 (y 0–2, dentro del atajo) | ...Eso no estaba en el manual. | **Nueva, provisional** |
| `T_Techo` | 117 | Mira hacia arriba de vez en cuando, Novato. O no. | Provisional |
| `T_Hueco` | 139 | Ese hueco no se salta. Créeme: lo sé. | Provisional |
| `T_SalidaFalsa` | 150 | ¿La salida? Ah, eso. Lleva años sin funcionar. | Provisional (no está en el GDD) |
| `T_CheckpointFalso` | 157 | ¡Mira, un punto de control! Tócalo, es gratis. | Provisional; idea de Jesús |
| `T_Tunel` | 205 | Plataformas y pinchos. Nada que un niño de 2003 no supere. | Provisional |
| `T_Checkpoint2` | 257 | ¿Sigues aquí? Qué terco. Descansa... no tengo prisa. | Provisional |
| `T_Salida` | 300 | La salida está justo ahí. ¡Salta con todas tus fuerzas! | Provisional |
| `T_Puerta` | 312 | Vaya, qué sorpresa. Una puerta cerrada. / ¿Has probado abrirla con... no sé, una llave? / Tranquilo, tampoco esperaba que lo supieras. | Jesús |
| `T_Llave` | 319 | ¿Una llave? Cuídala bien... si te mueres, se queda aquí conmigo. | Provisional |

### Pruebas (Play Mode automático, sin teclado)

| Prueba | Resultado |
|---|---|
| Recorrido inicio → portal | Sin morir y sin colocar al jugador por código. Sube de verdad a los bloques: (125,10; 3,01) y (188,08; 0,01). Coge la llave y aterriza vivo. Completa en x = 314,90 con 0 muertes, 54,2 s de script: no es la duración de una persona. Es una sola línea de ejecución, con los saltos ajustados por el script. |
| Cada trampa (29 muertes) | Todas matan. Antes del CP1 reaparece en (2; 0,51), también tras el checkpoint falso. Con CP1, en (172,8; -2,49). Con CP2, en (260,3; -1,99). |
| Reinicio | Todas las `FallingPlatform` probadas vuelven a su sitio con collider tras morir. |
| Salida falsa | No mata ni completa el nivel: cae a (154; -2,49). |
| Puerta sin llave | Frena en x = 315, no completa el nivel, secuencia una vez. |
| Coger la llave corriendo | Cogida, la repisa cae, aterriza vivo en (311,8; 0,01). |
| Morir con la llave | Vuelve a (319; 7,9) y la repisa a (319,5; 7). |
| Repisa caída sin coger la llave | El jugador queda vivo abajo. Andando desde la plataforma fija pasa por debajo; saltando y avanzando a toda velocidad, por encima. Con salto leve vertical desde x = 322,6 y avance en el aire a los 0,20–0,40 s la coge (5 de 5, velocidades fijadas por script). No hay atasco: también se puede morir en el foso x 304–307, y eso repone llave y repisa. |
| `S2_Pincho_A` | Correr sin frenar: muere (x = 109,66, esperado). Frenar antes o tarde: vivo en x 103,58 y 105,50. |
| Túnel, parar antes del pincho | Vivo en (213,92; -2,49); el pincho no llega a caer. |
| `Archive/Level_01_v2.unity` | Abre: 10 raíces y 2 checkpoints. No está en Build Settings. |
| Consola y referencias | 0 referencias rotas en `Level_01`. Tras recargar scripts y una pasada corta en Play con muerte y reaparición: 0 errores y 0 avisos. Durante las pruebas salieron `MissingReferenceException` por oyentes de mis scripts de prueba, no del juego; la recarga los eliminó. |

**No probado:**

- La partida con teclado y la duración real para una persona.
- El margen de cada salto, reto a reto: ni el margen de altura ≥0,3 ni que haya más de un instante válido se midieron uno a uno. El recorrido solo demuestra que existe una forma de pasarlos.
- Si las trampas se leen bien mientras se juega, y si la dificultad es justa. La roca flotante de `S3_Roca` y la llave cuadrada son provisionales.

---

## Nivel 1: muro con atajo troll, x 85–103 (2026-11-04)

**Estado:** montado y guardado en `Level_01`. Falta la prueba con teclado.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Cruzar el muro por arriba es difícil pero pasable, y la pared bajo los pinchos es falsa: quien la descubre se salta toda la subida. |
| [Decisión] | Solo instancias con overrides de mecánicas existentes; sin scripts nuevos ni cambios en prefabs base. |
| [Decisión] | `S2_Atajo_FakeWall`: (98; 1), 4 × 2, Grieta desactivada por override. Única pista: la mancha gris `ManchaAntigua_I`. El pincho de pared de y = 1,5 se quita; el más bajo empieza en y = 2,05. |
| [Decisión] | `T_Atajo` va dentro del atajo, bajo el techo de y = 2, para que no suene a quien baja por arriba. `T_DobleSalto` pasa de x 99 a x 87 para que empuje a subir. |
| [Decisión] | Muro subido de y = 7 a y = 9, con pinchos en y 2,5–8,5. Plataforma de ladrillo subida de y = 2,5 a y = 7, fuera del alcance desde el suelo (máx. 6,4), lo que obliga a usar el escalón. |
| [Decisión] | `S2_Muro_Pincho` en x 97,25 (no x 97), con `triggerOffsetX` = 0: quien aterriza en x ≈ 96 no lo activa hasta avanzar. |

### Ruta de arriba

| Paso | Elemento | Cómo se pasa | Margen de altura (cálculo) |
|---|---|---|---|
| 1 | `S2_Muro_Escalon`: x 90–91 (movido desde x 88–89), arriba y = 2,5, 0,3 s, sin temblor | Salto máximo. Hay que subir antes de llegar a x 89,5: debajo choca con la cabeza | 0,7 |
| 2 | Plataforma x 92–94, arriba y = 7 | Doble salto desde el escalón (máximo + leve) | 0,7 |
| 3 | `S2_Muro_Bloque` (95; 10,25), bloque invisible | Salto largo desde el borde izquierdo (x ≈ 92,4): pasa sin revelarlo. Saltando desde x 93,4 lo golpea, cae y muere en los pinchos si sigue hacia el muro. Revelado: volver a la plataforma y doble salto por encima | Sobre el muro 1,2 con salto máximo |
| 4 | `S2_Muro_Pincho` (97,25; 13) | Avanzar sin detenerse debajo | — |
| 5 | Pinchos de suelo x 98,5 y 99,5 (y 9,25); de techo en las mismas x (y 12,75), bajo la losa x 96,5–100, y 13–14 | Salto corto desde x ≤ 97,2. El máximo choca con los de techo | 0,63 (salto leve contra techo) |

### Pruebas (Play Mode automático, sin teclado)

| Prueba | Resultado |
|---|---|
| Atajo andando desde x 86 | Vivo, y constante en 0,51, sale en x 103 con 0 muertes. Alfa 0,40 dentro y 1 fuera. Suena `T_Atajo`. |
| Saltar dentro del atajo | No mata: el techo lo corta. |
| Ruta de arriba completa, desde x 85,7 hasta el suelo | 3 de 3 sin morir, con salto corto desde x 96,42, 97,06 y 97,22. No sube a ninguna parte por código; las velocidades las fija el script. |
| Salto corto desde x 97,54 | Muere en los pinchos de suelo. El límite está entre x 97,22 y x 97,54. |
| Salto obvio desde x 93,4 | Revela el bloque, y si sigue hacia el muro muere en los pinchos. |
| Bloque revelado | Se puede volver a la plataforma y pasar con doble salto. |
| Reaparición | Escalón vuelve a (88,5; 2) con collider; bloque se oculta; pincho vuelve a (97,25; 12,75). |
| Trampas nuevas | Matan: pinchos de pared en y 2,5, 5,5 y 8,5, de suelo y de techo, y pincho que cae a quien se queda debajo. |
| Consola | 0 errores y 0 avisos tras la última pasada. |

**No probado:**

- Con teclado: el bot fija velocidades, no pasa por `PlayerMovement` ni `PlayerJump`.
- Si el salto al escalón es cómodo para una persona. Se movió a x 90–91 porque en x 88–89 fallaba con teclado simulado (ver "Coyote time"). Ahora pasa saltando corriendo desde x 87,2.
- Tampoco se probó si el primer pincho del pasillo afecta a quien aterriza tras la ruta de arriba cerca de x 103: no se recorrió más allá. El resto del nivel no cambió.
- Si el atajo se descubre: sin grieta, la única pista es la mancha.

---

## Coyote time y corrección del doble salto (2026-11-04)

Jesús notó "un punto en el aire en el que el doble salto no responde". Cambio en `Scripts/Player/PlayerJump.cs`, sin nuevos scripts ni paquetes.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | `coyoteTime = 0,1` s (el GDD lo menciona en Alcance). Tras perder el suelo sin saltar, cuenta como salto de suelo y no gasta el doble. Vale una vez: no da un tercer salto. También al caer de una `FallingPlatform`. |
| [Decisión] | `doubleJumpMinBoost = 3`: si el doble salto ganaría menos que eso porque aún se sube rápido, la pulsación espera a poder darlo en lugar de gastarse casi sin efecto. Espera como mucho unas centésimas: hasta que la velocidad baja a unas 8. |
| [Decisión] | Sin jump buffer: la batería no lo señala como causa del fallo. Si el doble ya está gastado, una pulsación justo antes de aterrizar sigue sin hacer nada. |
| [Decisión] | Hueco x 125–138: el coyote permitía cruzarlo sin el ayudante. Columna de metal en x 131–132, de y = -3 a y = 6 (alcance sin ayudante: y ≈ 4,4). Mover la orilla no servía: la diferencia con y sin ayudante quedaba en unas décimas. Se puede quitar borrando esos tiles y reponiendo el pincho de x 131,5. |
| [Decisión] | `S2_Muro_Escalon` pasa de x 88–89 a x 90–91: con teclado simulado, el salto corriendo desde x 85,7 chocaba por debajo. |

**Causa encontrada** (batería de pulsaciones por teclado simulado, cada 0,02 s de 0 a 0,8 s, con salto leve y con máximo, 82 casos). El doble salto salía siempre, pero:

1. Pulsar tras salir de un borde o de una plataforma que cae lo gastaba como primer salto, y la siguiente pulsación no hacía nada.
2. Pulsar mientras se sube casi a su misma velocidad apenas cambiaba nada y lo gastaba: a 0,17 s de un máximo, vy 10,1 → 10,6; altura total 3,58 frente a 3,2 sin él.

### Pruebas (Play Mode, teclado simulado salvo lo indicado)

| Prueba | Resultado |
|---|---|
| Alturas | Leve 2,00. Máximo 3,20. Máximo + máximo 6,40. Leve + leve 4,00 (medido antes de `doubleJumpMinBoost`, que no afecta a pulsar en el punto más alto). |
| Triple salto | No sale. |
| Coyote andando desde un borde | A 0,02, 0,04, 0,06 y 0,08 s conserva el doble; a 0,14 s es el doble. Tiempos deducidos de la velocidad de caída. |
| Coyote al caer de una plataforma | A 0,00 y 0,04 s conserva el doble; a 0,12 s es el doble. |
| Batería tras corregir | 82 de 82 con impulso. Pulsaciones tempranas tras un leve: altura 3,09 (antes 2,41). A 0,17 s de un máximo: 4,21 (antes 3,58). Nunca supera lo previsto: tope 4,00 con leve, 5,20 con máximo. |
| Ruta de arriba del muro | Completa de principio a fin con teclado simulado, 0 muertes. |
| Recorrido completo inicio → portal | Sin morir, por el atajo del muro y el ayudante: completa en x = 314,90, 53,4 s de script. Este recorrido fija las velocidades por script, no usa `PlayerJump` para saltar. |
| Hueco sin ayudante | 4 de 4 mueren contra la columna (tras 0,00, 0,03, 0,05 y 0,08 s del borde). |
| Consola | Sin errores ni avisos del juego. Salió una vez un error de la barra de herramientas del editor de Unity (`MainToolbarWindow`), ajeno al proyecto. Vacía en la última lectura. |

**No probado:**

- Teclado físico en manos de una persona: las pulsaciones son simuladas, y falta que Jesús confirme que la sensación ha mejorado.
- Si la espera del doble salto se percibe. Por cálculo son unas centésimas.
- Cruzar el hueco con teclado simulado desde el ayudante: solo con el recorrido por script.
- Si el coyote vuelve trivial algún otro reto: solo se revisaron los dos huecos con ayudante. El de x 188–202 sigue sin cruzarse. No se midió el resto, aunque 0,1 s aporta como mucho 0,8 u de alcance.

---

## Nivel 1: pisos ocultos, trampas sorpresa y gráficos (2026-11-04)

Copia previa en `Scenes/Archive/Level_01_v3.unity` (abre; no está en Build Settings). Comparte tiles y prefabs con el nivel actual, así que se ve con el arte nuevo.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Más trampas y dinamismo, pisos más rápidos y menos predecibles, el piso del inicio tapa los pinchos, mejores gráficos. |
| [Decisión] | Sin mecánicas nuevas. Sigue sin haber activador, pinchos emergentes, suelo falso como script ni checkpoint con evento. |
| [Decisión] | `FallingPlatform`: retardo 0,2 s (mínimo del GDD), gravedad de caída 12 y sin temblor. Excepciones: islas de 1 de ancho 0,25 s, `S3_Cadena_B` 0,5 s para romper el ritmo, `S2_SalidaFalsa` con temblor. |
| [Decisión] | Piso disfrazado: collider de 1 de grosor y `Visual` tan alto como el foso, con el mismo tile que el suelo real. Tapa los pinchos y la mancha, que solo se ven al caer. Se quitó la luz roja `Red_S1_A`. |
| [Decisión] | Scripts nuevos solo visuales en `Scripts/Visual/`: `ParallaxLayer`, `LightPulse`, `PlatformFallDebris`, `AmbientParticlesZone`. No cambian colisiones ni reglas. |
| [Decisión] | Prefab base tocado: `Effects/DeathParticles.prefab` (solo aspecto). `Tile_Metal.png` y `Tile_Platform.png` regeneradas. |
| Provisional | Frases nuevas: `T_Mentira` x 54 "¿De verdad me creíste? Qué tierno."; `T_Orilla` x 144 "Aterrizar también es una decisión, Novato." |

### Tramos vacíos y qué se puso

| Tramo | Antes | Ahora |
|---|---|---|
| x 34–40 | Foso con salto sin más | `S1_Bloque_Foso` (35,5; 7,8): corta el salto máximo. El leve pasa por debajo con 0,36 de margen. No mata por sí solo: si sigues corriendo, aterrizas. |
| x 40–52 | Descanso de 12 | Descanso real x 40–47. `S1_SueloDescanso` x 47–51: suelo falso justo tras "Aquí no pasa nada. Lo prometo." |
| x 85–87 | Llegada segura | `S2_Aterrizaje`: el salto desde la isla cae en un piso falso. |
| x 140–146 | Orilla vacía | `S2_Orilla` x 140–142: piso falso tras el hueco. |
| x 160–172 | Pasillo vacío | x 166–170: techo bajo y pinchos arriba y abajo; solo vale el salto leve. |
| x 172–181 | Vacío tras el CP1 | `S3_Pincho_CP1` x 177: cae si te paras tras cruzar x 174. Corriendo se pasa. |
| x 218–232 | Vacío ante la pared falsa | `S3_SueloFalso` x 227–230. |
| x 279–298 | Fija, cae, fija, cae | Fija, fija, cae, cae: `S4_Isla_C` x 290,5 y bloque fijo en x 286. |
| x 298–304 | Vacío | `S4_SueloFalso` x 300–302. |
| x 307–316 | Vacío ante la puerta | `S5_SueloFalso` x 310–312. |

### Gráficos

`Scripts/Editor/LevelArtGenerator.cs`, menú Unknown Game > Arte: tiles con remaches, biselado y borde superior, 3 variantes limpias y 3 oxidadas o con glitch desde x 150; ladrillo; 2 capas de parallax (racks y cables) que funden a una versión rota entre x 140 y 165; llave dibujada con luz que late; polvo en la zona limpia y chispas en la deteriorada; escombros al caer una plataforma (`Prefabs/Effects/PlatformDebris.prefab`, solo tras caer); partículas de muerte; parpadeo de luces rojas desde x 150 y latido del portal. Material en `Materials/VFX/Particula.mat`.

### Pruebas (Play Mode automático)

| Prueba | Resultado |
|---|---|
| Recorrido completo inicio → portal, sin colocar al jugador | Completa en x = 315,22 con 0 muertes y la llave, 53,2 s de script. Fija velocidades por script. |
| Ruta de arriba del muro, teclado simulado, retardos finales | Completa sin morir. |
| Pisos falsos y plataformas | Matan y vuelven a su sitio con collider (inicio A y B, tras foso, descanso, islas, aterrizaje, orilla, S3, cadena, S4, S5). |
| Pinchos de techo, pasillo, checkpoint falso | Matan. |
| `S3_Pincho_CP1` | Parar debajo tras cruzar la franja mata; corriendo se pasa. Vuelve a su sitio. |
| `S1_Bloque_Foso` | Se revela con salto máximo; se oculta tras morir. |
| Reaparición | Inicio (2; 0,51), también tras el checkpoint falso. CP1 (172,8; -2,49). CP2 (260,3; -1,99). Ninguna sobre una trampa. |
| Puerta y llave | Sin llave frena en x = 315. Morir con la llave la devuelve a (319; 7,9) y la repisa a su sitio. |
| Rendimiento | 182 FPS de media en el Editor, parado al inicio, peor fotograma 10 ms. No se midió recorriendo el nivel ni en una build. |
| Consola y referencias | 0 errores, 0 avisos, 0 referencias rotas. |

**No probado:**

- Teclado físico en manos de una persona, y si las muertes se entienden a la segunda.
- Si los pisos falsos se distinguen con la cámara del juego: se revisaron en capturas de escena, no en juego. El reparto de variantes del suelo real no se repite en el piso falso.
- Cómo se ven las partículas y el parpadeo en movimiento.
- Las islas de 0,25 s y los pisos de 0,2 s con reacción humana: el bot salta de inmediato.
- Hay muchas muertes posibles antes del primer checkpoint real (unas 170 u): quizá haya que reconsiderarlo al jugarlo.

---

## Nivel 1: mejoras tras la prueba de Jesús, etapa 1 (2026-11-05)

Copia previa en `Scenes/Archive/Level_01_v4.unity` (no está en Build Settings).

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Propuesta completa por etapas, sin mecánicas nuevas: pisos y fosos que no se pasen andando, ruta alta del muro más accesible, cola de frases, nivel más largo. Para la etapa 2: separar los checkpoints y añadir un checkpoint real a mitad del tramo inicial. **Contradice el GDD ("1-2 por nivel")**, con aprobación expresa de Jesús. |
| Causa | A 8 u/s se avanza 1,6 u en los 0,2 s de retardo, y la cápsula del jugador sube sola caídas de hasta unas 0,25. Por eso se pasaban andando los pisos de 2–3 de ancho. Los fosos estrechos se cruzaban cayendo. Las pruebas anteriores no lo detectaron porque colocaban al jugador parado o saltando. |
| [Decisión] | Suelo falso de 4 de ancho como mínimo. `S1_SueloQueCede` x 14–20 es un solo piso de 6. `S2_Orilla` x 140–144, `S3_SueloFalso` x 226–230, `S4_SueloFalso` x 300–304, `S5_SueloFalso` x 309–313. |
| [Decisión] | `S2_Aterrizaje` x 85–92 (7 de ancho): con 4 aún se cruzaba, porque se aterriza ya dentro del piso. Obliga al doble salto desde la isla. |
| [Decisión] | Fosos: x 118–123 (pilar x 123–125) y x 181–186 (pilar x 186–188). El pincho que cae sobre el pilar de x 186 queda para la etapa 2. |
| [Decisión] | Muro: `S2_Muro_Escalon` a 0,35 s. Escalón fijo nuevo en x 94–95 (arriba y = 2,5). Plataforma alta recortada a x 92–93,5 para dejar paso desde el escalón fijo. `S2_Muro_Bloque` movido a (95,5; 10,25): el salto largo vale hasta x 92,8; desde x 93,1 lo golpea y mueres. La grieta del atajo ya estaba desactivada. |
| [Decisión] | `NarratorUI`: cola de frases. La frase nueva espera a la actual. Con otra en cola, se mantiene 1,5 s en vez de 2,5 s (`holdSecondsWithQueue`). `Narrator.SayIfIdle`: las burlas se descartan si el Sistema está hablando, porque las frases de trigger suenan una sola vez y se perderían. |

### Pruebas (Play Mode, teclado simulado salvo lo indicado)

| Zona | Andando sin saltar | Saltando |
|---|---|---|
| Inicio x 14–20 | Muere en x 18,6 | Pasa con salto máximo |
| Descanso x 47–51 | Muere | Pasa con salto leve |
| Aterrizaje x 85–92 | Muere (salto leve o máximo desde la isla y seguir) | Pasa con doble salto máximo |
| Foso x 118–123 | Muere | Pasa, cae en el pilar |
| Orilla x 140–144 | Muere | Pasa con salto leve |
| Foso x 181–186 | Muere | Pasa, cae en el pilar |
| Suelo x 226–230 | Muere | Pasa con salto leve |
| Suelo x 300–304 | Muere | Pasa con doble salto máximo |
| Suelo x 309–313 | Muere | Pasa con salto leve |

| Prueba | Resultado |
|---|---|
| Muro, ruta A (escalón que cae) | Completa sin morir. |
| Muro, ruta B (escalón fijo) | Completa sin morir, medida con el bloque aún en x 95. Con el bloque en 95,5 solo se repitió la ruta A; el tramo final que comparten pasa. |
| Diálogos | "¿Te vas tan pronto?" sale en t = 0 y se mantiene hasta t = 3,61 s; "Bienvenido..." espera a que termine, aunque se llega a x 7 a los 0,66 s. |
| Burlas | Con el Sistema callado, sale. Hablando, se descarta y no corta la frase. |
| Recorrido completo inicio → portal | Sin morir: x = 315,22, 52,2 s de script. Fija velocidades por script. |
| Consola | 0 errores y 0 avisos. |

**No probado:**

- Teclado físico en manos de una persona.
- Aterrizar muy adentro de un suelo falso y seguir corriendo, salvo en x 85–92. En x 309–313, un doble salto aterriza hacia x 310: podría cruzarse.
- Si la cola retrasa demasiado alguna frase: la separación entre frases se ajusta en la etapa 2.

**Pendiente, etapa 2:** ruta falsa x 52–62, separar checkpoints y checkpoint extra, bloques invisibles más estorbosos, pincho sobre el pilar de x 186, sección x 218–232, reto de la llave, alargar el nivel a unas 420–450 u y separar frases.

---

## Nivel 1: rediseño de la ruta x 52–66 (2026-11-06)

Copia previa en `Scenes/Archive/Level_01_v5.unity`. Sin scripts ni mecánicas nuevas. La captura de Jesús es esta zona, no el muro de x 96.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Rediseñar la zona para darle más desafío. Antes era una ruta falsa sin salida. |
| [Decisión] | Ruta alta real, con esta secuencia: `S2_Ruta_Cae` x 52–56 (arriba y = 4, 0,35 s, sin temblor) → salto al ladrillo x 57,5–59,5 (arriba y = 5) → doble salto al muro → salto a la isla. |
| [Decisión] | `S2_Ruta_Bloque` (58; 8): frena el salto máximo lanzado desde cerca. El salto leve pasa por debajo. Desde x 53 el máximo no llega a tocarlo. |
| [Decisión] | `S2_Ruta_Pincho` x 58, en una losa x 57–59, y 12–13. Franja de activación x 56,5–57,5. Quedarse donde se aterriza mata; a partir de x 59 se está a salvo. |
| [Decisión] | Muro x 62–66 rebajado de y = 14 a y = 8,5 para poder cruzarlo. Pinchos de pared hasta y = 8 (nuevo en y = 7,5). |
| [Decisión] | Pinchos en el suelo x 54–60. Seguros: x 52–54 y x 60–62. Bajar del ladrillo corriendo mata contra los pinchos de pared. |
| [Decisión] | Atajo: `S2_FakeWall` sin grieta (solo mancha), porque ya hay otra ruta visible. Se reactiva con el hijo `Grieta`. Antes esa grieta era la pista de la única salida. |
| [Decisión] | Segunda forma de subir: caer vivo en x 52–54 o estar en x 60–61 y subir al ladrillo con doble salto. Un piso caído no bloquea la ruta. |

### Pruebas (teclado simulado)

| Prueba | Resultado |
|---|---|
| Andar sobre el piso desde x 51,6 | Muere en x 59 |
| Andar por el suelo desde x 53 | Muere en x 53,6 |
| Ruta alta completa hasta la isla x 75,5 | Viva |
| Salto máximo desde x 55 | Revela el bloque y aterriza en el ladrillo. Medido antes de mover el pincho |
| Quedarse donde se aterriza / avanzar a x 59,2 y esperar 2 s | Muere / vivo |
| x 53 → ladrillo, x 53 → suelo x 60–62, x 60,7 → ladrillo, con doble salto | Vivo en los tres |
| Pared falsa desde x 60,5 | Vivo, sale en x 65,6 |
| Bajar del ladrillo corriendo hacia la pared | Muere en los pinchos |
| Reinicio | Piso, bloque y pincho vuelven tras morir. Piso y bloque medidos antes del último ajuste, que no los tocó |
| Recorrido completo inicio → portal, velocidades por script | Sin morir: x = 315,22, 52,5 s de script |
| Consola | 0 errores y 0 avisos |

**No probado:** teclado físico; si se descubre el atajo sin grieta; si el pincho interactúa con el bloque ya revelado (el bloque queda justo en su columna); bajar al suelo x 60–62 desde el ladrillo a velocidad controlada; llegar a x 60–62 a la primera sin saber que está ahí.

---

## Nivel 1: mejoras, etapa 2 (2026-11-06)

Copia previa en `Scenes/Archive/Level_01_v6.unity`. Sin scripts ni mecánicas nuevas, sin alargar el nivel (queda para la etapa 3).

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Tercer checkpoint real, a mitad del tramo inicial. **Contradice el GDD ("1-2 por nivel")**, con aprobación expresa de Jesús. |
| [Aprobado por Jesús] | Separar el checkpoint falso del real, bloques invisibles más estorbosos, dar sentido a la sección x 218–232, reto ligero para la llave. |
| [Decisión] | `Checkpoint_Medio` x 103,5, donde se juntan las dos rutas del muro, reaparece en x 102,7, antes de la franja de `S2_Pincho_A`. |
| [Decisión] | `S3_CheckpointFalso` x 165; `Checkpoint` real x 205,5 (reaparece en x 206,3); separación 40,5. Entre ambos: pasillo con pinchos, pincho que cae, foso con pilar y ayudante. `T_CheckpointFalso` sigue en x 157. |
| [Decisión] | Pasillo: pinchos y techo bajo movidos a x 170–172 (techo x 169–173) para no quedar pegados al falso. |
| [Decisión] | Bloques: `S1_Bloque_Muro`, `_B`, `_C` en fila x 23–26, y 5,4. `S1_Bloque_Foso`, `_B`, `_C` en fila x 34–37, y 7,8. El leve pasa por debajo. |
| [Decisión] | `S3_Pincho_Pilar` x 186,25 en una losa x 185–188, y 8–9, franja 14 de alto. En x 186,5 solo había unas décimas a salvo. Tarda unos 1,2 s en llegar: da tiempo a apartarse hacia x 187,4. |
| [Decisión] | Dilema x 218–236. Arriba: `S3_Dilema_Escalon` x 218–220 (0,35 s) → plataforma x 222–226 → `S3_FakeWall_Alta` x 232–236, y 0–4 → cadena. Abajo: pinchos x 218–226, suelo falso, y `S3_FakeWall` (ahora y -3 a -0,5) con pinchos ocultos en x 233–235. Las dos tienen grieta. Arriba es más lenta y segura; abajo, más rápida y con trampa. |
| [Decisión] | Llave: repisa partida en `S5_RepisaLlave` x 317,5–319,5 (0,35 s) y `S5_RepisaLlave_B` x 320,5–322 (0,3 s). Pinchos de techo x 317–321, y 11,25. Se coge con salto corto casi vertical y dejándose caer. |
| [Decisión] | `S5_SueloFalso` alargado a x 309–314,5: aterrizar dentro de él y seguir corriendo ya mata. |
| Hallazgo | El falso no se salta con salto corto: su altura supera casi todo el arco. Hace falta máximo o doble. Se deja así. |
| Hallazgo | Tras coger la llave, bajar corriendo hacia la izquierda sobrevuela la puerta y cae en el suelo falso, y se pierde la llave. Se deja como trampa; hay que bajar y entrar por la derecha. Jesús puede cambiarlo. |

### Pruebas (teclado simulado salvo lo indicado)

| Zona | Resultado |
|---|---|
| Fila x 23–26 | Salto máximo: se frena y vuelve abajo. Doble salto subiendo primero: llega al muro. |
| Fila x 34–37 | Máximo desde x 32: revela y muere. Desde x 33 y 33,4: revela y aun así llega. Leve: pasa sin revelar. |
| Checkpoints | Medio: reaparece (102,70; 0,51). Falso: mata y devuelve a x 102,7. Real: reaparece (206,30; -2,49). |
| Pasillo | Andando mueres. Salto máximo bajo el techo mata. |
| Pilar | Andando mueres. Con el pincho en x 186,5: quedarse en x 186,4 mata, y x 187,6 está a salvo. Con el pincho en x 186,25: x 187,42 a salvo. Ayudante accesible. |
| Dilema | Andar por abajo mata (pinchos). Pared baja andando mata; saltando pasa. Ruta alta completa hasta la cadena. |
| Llave | Máximo mata en el techo. Andar sin saltar no la coge. Salto vertical y caída: la coge. |
| Suelo x 309–314,5 | Andando mueres. Doble salto, aterrizar dentro y seguir: mueres. Salto máximo desde x 308,4: pasa. |
| Recorrido completo por script (velocidades fijadas), ruta alta del dilema | Sin morir, con llave: completa en x = 317,15, 56 s de script. |
| Consola | 0 errores y 0 avisos. |

**No probado:** teclado físico. Con teclado simulado: salto máximo sobre el falso y salto leve bajo el techo (solo con el recorrido por script). Quedarse en x 186,4 con el pincho ya en x 186,25. Si la ruta alta del muro, cayendo más allá de x 103,5, salta por encima de la bandera. Si se entiende que la pared baja tiene pinchos sin morir primero. Si el pincho interactúa con el ayudante revelado.

---

## Nivel 1: mejoras, etapa 3 (2026-11-06)

Copia previa en `Scenes/Archive/Level_01_v7.unity`. Sin mecánicas nuevas. **Longitud final: 463 u** (antes 331). Todas las x anteriores de este registro quedan desplazadas; las vigentes son las de esta sección.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Alargar el nivel y separar las frases para que no se pierda la narrativa. |
| [Decisión] | 463 u en vez de 420–450: es lo que exige separar las frases 30 u o más sin retirar más frases. |
| [Decisión] | Se retiran `T_Mentira` y `T_Orilla` (provisionales propias): no cabían a 30 u de sus vecinas sin perder el sentido de las demás. |
| [Decisión] | `Checkpoint_Medio` pasa a x 159, antes del muro, para repartir el tramo (157 y 160,5 u). Reaparece en x 159,5. |
| Riesgo | Con 3 reales y el falso a 40 u del real, no se llega a 120 u o menos entre checkpoints en los dos primeros tramos. Un cuarto real hacia x 240 lo resolvería; pendiente de Jesús. |
| [Decisión] | `LevelArtGenerator.DamagedFromX` = 210. El óxido empieza en el tramo tras la orilla, un poco antes de la salida falsa (x 248). |
| Hallazgo | Tras coger la llave sigue valiendo: bajar corriendo hacia la izquierda sobrevuela la puerta y cae en el suelo falso. |

### Disposición vigente (x)

| Zona | x | Contenido |
|---|---|---|
| Inicio | 0–30 | **Nuevo:** pinchos x 13 y x 21–23. |
| Suelo falso | 36–42 | `S1_SueloQueCede`. |
| Muro y bloques | 45–48 | Fila de bloques; muro x 48. **Nuevo:** pinchos x 55–57. |
| Foso | 64–70 | Fila de bloques x 64–67; suelo que cede x 68–70. |
| Previo al descanso | 75–97 | **Nuevo:** pinchos x 79–81, `S1_Pincho_Previo` x 85. Descanso real hasta x 103. |
| Descanso falso | 103–107 | `S1_SueloDescanso`. |
| Ruta x 108–122 | 108–122 | Piso que cae, ladrillo, bloque, pincho, muro y pared falsa (`S2_FakeWall` x 118–122). |
| Islas | 122–151 | Fija x 126. **Nuevo:** fija x 132 y `S2_Isla_Nueva` x 136. Islas x 141,5 y x 146,5. |
| Aterrizaje | 151–158 | `S2_Aterrizaje`. `Checkpoint_Medio` x 159. |
| Muro x 162 | 156–166 | Escalón que cae, escalón fijo, plataforma, atajo. |
| Entrada del pasillo | 167–181 | **Nuevo:** `S2_SueloFalso_Entrada` x 170–174; pinchos x 177–179 con `S2_Roca_Entrada`. |
| Pasillo | 181–198 | Pinchos que caen, pinchos de suelo. |
| Hueco | 198–218 | Pilar x 203–205, ayudantes, columna x 211–212. |
| Orilla | 220–224 | `S2_Orilla`. **Nuevo:** pinchos x 230–232 con `S2_Orilla_Bloque`. |
| Salida falsa | 240–250 | Muro x 240, salida x 248. |
| Pasillo previo | 252–272 | **Nuevo:** pinchos x 257–259, `S3_Pincho_Pasillo` x 264, `S3_SueloFalso_Pasillo` x 267–271. |
| Checkpoint falso | 279 | Pasillo con techo x 283–287, pincho x 291. |
| Foso y pilar | 295–316 | Pilar x 300–302, `S3_Pincho_Pilar` x 300,25. |
| Checkpoint real | 319,5 | Túnel x 320–332. |
| Dilema | 332–350 | Escalón, plataforma, pared alta y baja x 346–350. |
| Cadena y CP2 | 350–373,5 | Cadena; `Checkpoint2` x 373,5. |
| Rocas e islas | 378–412 | Foso con rocas; islas. |
| Suelo falso | 414–418 | `S4_SueloFalso`. Foso x 418–421. |
| Cámara final | 422–440 | **Nuevo:** pinchos x 428–430, `S5_Pincho_Camara` x 434,5. |
| Puerta y llave | 441–463 | `S5_SueloFalso` x 441–446,5; puerta x 448; llave x 451. |

Manchas nuevas en los pinchos y suelos falsos de los tramos x 75–97, x 122–151, x 167–181, x 225–239, x 252–272 y x 422–440. Sin mancha nueva: los primeros saltos x 13, x 21 y x 55, para no asustar antes de tiempo.

### Frases

| Frase | x | Separación |
|---|---|---|
| `T_Inicio_GDD` | 2 | — |
| `T_Demo` | 32 | 30 |
| `T_Suelo` | 62 | 30 |
| `T_Descanso1` | 92 | 30 |
| `T_ParedesFalsas` | 122 | 30 |
| `T_DobleSalto` | 152 | 30 |
| `T_Atajo` (secreta) | 165,4 | 13, solo por el atajo |
| `T_Techo` | 182 | 30 con `T_DobleSalto` |
| `T_Hueco` | 212 | 30 |
| `T_SalidaFalsa` | 242 | 30 |
| `T_CheckpointFalso` | 272 | 30; 7 antes de la bandera |
| `T_Tunel` | 319 | 47 |
| `T_Checkpoint2` | 371 | 52 |
| `T_Salida` | 414 | 43 |
| `T_Puerta` | 444 | 30 |
| `T_Llave` | 451 | 7; solo al llegar a la llave, por otro camino |

### Pruebas

| Prueba | Resultado |
|---|---|
| Recorrido completo, velocidades por script | Sin morir, con llave y portal: x = 449,15, 72,5 s de script. |
| Andar y saltar, teclado simulado | Mueren al andar y pasan al saltar: x 13, x 21–23, x 36–42, x 55–57, x 79–81, x 170–174, x 177–179, x 230–232, x 257–259, x 267–271 y x 428–430. Solo andar: x 103–107, x 220–224 y x 441–446,5, también mueren. |
| Roca x 177 | El salto corto pasa; el máximo muere. |
| Pinchos que caen | Parar debajo mata en x 264 y x 434,5. |
| Reaparición | Medio (159,5; 0,51). Falso devuelve a x 159,5. Real (320,30; -2,49). CP2 (374,30; -1,99). |
| Ritmo de frases a 8 u/s | Hasta `T_DobleSalto`: cada frase aparece al llegar a su x, sin retraso acumulado. La pasada se cortó en x 162. |
| Consola | 0 errores y 0 avisos. |

**No probado:** teclado físico; `S1_Pincho_Previo` x 85 parado debajo (la prueba arrancó tocando pinchos); ritmo de frases más allá de x 162; `S2_Isla_Nueva`, escalón y ruta alta del muro con teclado simulado después de desplazarlos (el script pasa las islas, no la ruta alta del muro); reinicio de las trampas nuevas una por una; rendimiento con el nivel más largo.

---

## Nivel 1: corrección de checkpoints, islas y bloques (2026-11-06)

Copia previa en `Scenes/Archive/Level_01_v8.unity`. Sin mecánicas ni scripts nuevos. Longitud igual: 463 u. Corrige las tablas de la etapa 3 en lo que se indica aquí.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Se quita `Checkpoint_Medio` (x 159). |
| [Aprobado por Jesús] | El primer checkpoint es el falso: `S3_CheckpointFalso` pasa de x 279 a x 183,5, en el pasillo plano, tras la primera salida falsa de la entrada. |
| [Aprobado por Jesús] | El checkpoint real pasa de x 319,5 a x 289, invertido en el pasillo con pinchos arriba y abajo (x 284–286). Reaparece en (289,80; -2,49). Luz `Green_Checkpoint` movida con él. |
| [Aprobado por Jesús] | Pinchos que caen en la sección de islas: tres `S4_Pincho_Isla` en x 400,5, x 404,5 y x 408,5. |
| [Aprobado por Jesús] | `S5_Bloque_A` y `S5_Bloque_B` (x 419,5 y x 420,5) suben de y 0,9 a y 3,4 para que el jugador no quede debajo al saltar. |
| [Decisión] | `S2_Pincho_A` (x 188) pasa a `triggerOffsetX` = 1, con franja x 186,5–187,5: no cae al tomar la bandera falsa ni antes de x 186,5. |
| [Decisión] | Pinchos de islas con `triggerOffsetX` = 1, franja de 14 de alto y losa `Solid` x 399–410 en y 10–11 de la que cuelgan. Con offset 4 mataban en el aire al saltar sin pararse. La losa en y 7 dejaba poco margen. |
| [Decisión] | `T_CheckpointFalso` pasa a x 174; `T_Techo` se retira, porque ya no cabía a distancia razonable de la bandera falsa. |
| Interpretación | "Pinchos que caigan del suelo" se implementó con la mecánica existente: caen desde arriba sobre las islas. Que salgan del suelo sería mecánica nueva, sujeta a aprobación. |
| Consecuencia | El falso mata y devuelve al inicio (2; 0,51), porque antes de él no hay checkpoint real. Distancias: inicio → real 287 u; real → `Checkpoint2` 84,5 u; `Checkpoint2` → puerta ~75 u. |
| Pendiente de Jesús | Una última sección antes de la parte final del nivel. Aún no está definida; no se ha construido nada. |

### Frases vigentes (x)

`T_Inicio_GDD` 2, `T_Demo` 32, `T_Suelo` 62, `T_Descanso1` 92, `T_ParedesFalsas` 122, `T_DobleSalto` 152, `T_Atajo` 165,4, `T_CheckpointFalso` 174, `T_Hueco` 212, `T_SalidaFalsa` 242, `T_Tunel` 319, `T_Checkpoint2` 371, `T_Salida` 414, `T_Puerta` 444, `T_Llave` 451. Retiradas: `T_Mentira`, `T_Orilla`, `T_Techo`.

### Pruebas

| Prueba | Resultado |
|---|---|
| Recorrido completo, velocidades por script | Sin morir, con llave y portal: x = 449,15, 72,6 s de script. |
| Andar a la bandera falsa, teclado simulado | Muere en x 182,76 con el pincho colgante sin caer (y 5,75). Reaparece en (2; 0,51). |
| Saltar la bandera falsa | Salto corto y máximo la pasan vivos. Pararse bajo `S2_Pincho_A` mata. |
| Checkpoint real x 289 | Andar a los pinchos mata en x 283,6. Salto corto llega vivo a la bandera. Reaparece en (289,80; -2,49) y 2 s quieto ahí no matan. |
| Cadena de islas sin pararse, con losa en y 7 | x 400, x 404, x 408 y tierra x 413: vivo. No se repitió entera con la losa en y 10; el script del recorrido sí la pasa. |
| Margen del pincho x 400, losa y 10 | Quedarse mata a 0,79 s. Esperar 0–0,6 s y saltar sale vivo; esperar 0,8 s mata. |
| Bloques x 419–421 | Doble máximo desde x 412,6 y máximo más leve desde x 413 llegan a la cámara con los bloques revelados. Doble máximo desde x 413,4 pasa sin revelarlos. |
| Consola | 0 errores y 0 avisos en el recorrido completo. |

**Sin explicar:** en una tanda, esperar 0,2 s en x 400 y saltar murió cayendo en x 401,51. Repetido 3 veces con el mismo script no se reprodujo. Es 1 caso de 4 a 0,2 s; no sé si fue el script o el juego.

**No probado:** teclado físico; `S1_Pincho_Previo` x 85 con un arranque limpio; ritmo de frases más allá de x 162; ruta alta del muro con teclado simulado; el salto a la altura exacta bajo los bloques x 419–421 desde otros puntos; rendimiento.

### Revisión y copia v9 (2026-11-08)

- **"Se perdió la versión":** no se perdió. En el editor estaba abierta `Archive/Level_01_v8.unity`, la copia anterior a estas correcciones; según `Editor.log` se abrió a mano. `Level_01.unity` (guardada el 2026-11-06 a las 22:56) conserva todas las correcciones, comprobado en disco y en el editor.
- `Archive/Level_01_v9.unity`: copia byte a byte de `Level_01` con estas correcciones.
- **Muerte sin explicar en x 400:** 0 de 14 al repetirla (esperas de 0,15, 0,2 y 0,25 s) y 0 de 3 antes. En todas, el salto salió desde el suelo (vy 10–10,6). No se pudo reproducir; se atribuye al script de aquella tanda y no se cambió nada del nivel.
- Recorrido completo por script: sin morir, con llave y portal (x 449,15; 72,5 s). Consola: 0 errores y 0 avisos.

### Pinchos de las islas y bloques "?" más bajos (2026-11-08, v10)

Copia previa en `Archive/Level_01_v9b_antes.unity` (igual que v9). Copia final en `Archive/Level_01_v10.unity`, idéntica a `Level_01`.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Los pinchos de las islas siguen cayendo desde arriba. Se bajan para que caigan antes. |
| [Aprobado por Jesús] | Los bloques `S5_Bloque_A/B` se bajan para que estorben más. |
| [Decisión] | `S4_Pincho_Isla` x 400,5, x 404,5 y x 408,5: raíz de y 10 a **y 7**. La losa `Solid` x 399–410 pasa de y 10–11 a y 7–8. Franja sin cambios (offset 1, 1 × 14). Distancia de la punta a la cabeza del jugador: de 11,6 a 8,6 u. Caída teórica: 0,3 s de temblor + 0,76 s (antes 0,89 s). |
| [Decisión] | `S5_Bloque_A/B`: de y 3,4 a **y 1,9**. Se descartó y 1,4: al saltar desde x 415,8 el bloque tiraba al jugador al foso las 3 veces. |

| Prueba (teclado simulado) | y 10 / 3,4 (antes) | y 7 / 1,9 (ahora) |
|---|---|---|
| Quedarse quieto en la isla x 400 | Muere a 0,79 s | Muere a 0,58 s |
| Esperar en x 400 y saltar | Pasa hasta 0,6 s | Pasa hasta 0,4 s; a 0,5 s muere |
| Cadena x 400 → 404 → 408 → tierra sin pararse | Pasa | Pasa |
| Bloques: 21 saltos desde x 412,6 a x 417,4 (doble máximo, máximo + leve, solo máximo) | — | 0 muertes por bloque; 4 revelan el bloque y el jugador sigue. Las 3 muertes son saltos máximos sencillos desde x ≤ 414,2 que se quedan cortos, igual a cualquier altura. |
| Recorrido completo por script | Sin morir | Sin morir, llave y portal (x 449,15; 72,5 s) |
| Consola | — | 0 errores, 0 avisos |

Quedarse en x 404 también mata (0,64 s), pero porque esa isla es la plataforma que cae, no por el pincho.

**No probado:** teclado físico; saltos desde x 418 o más (ya sobre el foso); con y 1,9, bloques revelados que luego se pisan por arriba.

### Pinchos de las islas más rápidos; cuadros "?" pendientes (2026-11-08, v11)

Copia previa en `Archive/Level_01_v10b_antes.unity` (idéntica a v10). Copia final en `Archive/Level_01_v11.unity`.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Pinchos de las islas: misma altura (y 7), caída más rápida. `fallGravityScale` de 3 a **5** en los tres `S4_Pincho_Isla`. Temblor sin cambios (0,3 s). Caída teórica de la punta a la cabeza (8,6 u): de 0,76 s a 0,59 s; total desde que se activan, de 1,06 s a 0,89 s. |
| Pendiente de Jesús | Cuadros `S5_Bloque_A/B`: quiere que el jugador salte, choque y vuelva al sitio desde donde saltó. Siguen en y 1,9 (ver hallazgo). |

| Prueba de los pinchos (teclado simulado) | Gravedad 3 | Gravedad 5 |
|---|---|---|
| Quedarse en x 400 | Muere a 0,58 s | Muere a 0,42 s |
| Quedarse en x 408 | — | Muere a 0,44 s |
| Esperar en x 400 y saltar | Pasa hasta 0,4 s | Pasa hasta 0,25 s; a 0,3 s muere |
| Cadena sin pararse | Pasa | Pasa 3 de 3 |

Recorrido completo por script: sin morir, llave y portal (x 449,15; 73,7 s). Consola: 0 errores, 0 avisos.

**Hallazgo sobre los cuadros.** Se probaron 4 tipos de salto desde 6 puntos (x 412,6 a 416,6), con los cuadros a y 1,9, 1,4 y 0,9, sin soltar la derecha y volviendo a la izquierda al chocar (con el doble salto si quedaba). En ningún caso el jugador volvió al sitio de salto:
- `InvisibleBlock` solo anula la velocidad vertical. El jugador sigue avanzando a 8 u/s y cae al foso (x 418–421).
- Volver a la izquierda tampoco salva: si saltó desde `S4_SueloFalso` (x 414–418), ese suelo ya ha caído.
- Bajarlos solo añade muertes: a y 1,4 y 0,9 matan desde x 415,8 y 416,6.
- Con los cuadros sobre el pilar (x 412,5–413,5; y 0,4 y 0,9; prueba sin guardar), el jugador ya ha pasado bajo ellos antes de llegar arriba y casi nunca choca. Cuando choca, cae al foso igual.

Para que "choque y vuelva", el choque tendría que frenar o empujar hacia atrás. Eso cambia la mecánica 03 en el código, y no se ha hecho sin aprobación.

### Cuadros "?" a y 1,4 (2026-11-08, v12)

[Aprobado por Jesús] Opción C: `S5_Bloque_A/B` de y 1,9 a **y 1,4**, sin tocar código. Chocar con ellos tira al foso: es una muerte, no una vuelta. Copia final en `Archive/Level_01_v12.unity`; la previa es v11.

Cómo se pasa, según las pruebas de y 1,4 (teclado simulado):
- **Seguro:** saltar desde el pilar (x 412,6–414,2) con doble máximo, máximo más leve o leve más máximo. Pasan por encima sin chocar.
- **Seguro:** desde el suelo falso en x 415 con doble máximo, máximo más leve o solo máximo.
- **Trampa:** saltar desde x 415,8 mata con cualquier salto; desde x 416,6 depende del salto.
- **Sin relación con los cuadros:** el salto máximo solo desde el pilar se queda corto y cae al foso.

Recorrido completo por script: sin morir, llave y portal (x 449,15; 72,4 s). Consola: 0 errores, 0 avisos. No se añadió mancha nueva en el foso: no la pidió Jesús.

### Pared alta y cuadros "?" como escalón (2026-11-08, v13)

Copia previa en `Archive/Level_01_v12b_antes.unity` (igual que v12). Copia final en `Archive/Level_01_v13.unity`. Sin código nuevo.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Opción 2: pared tan alta que el doble salto directo no llega; hay que revelar los cuadros "?" y usarlos de escalón. El suelo que cae (x 414–418) se mantiene. |
| [Decisión] | Pared x 421–423 en `Solid`: de y −0,5 a **y 5** (+5,5). Copia la tile que ya había en x 421; detrás, el suelo de la cámara sigue en y −0,5. |
| [Decisión] | `S5_Bloque_A` de (419,5; 1,4) a **(413,5; 0)**, sobre el pilar. `S5_Bloque_B` de (420,5; 1,4) a **(414,5; 0)**, sobre el borde del suelo que cae: es la trampa para quien salta desde ahí. |

**Ruta (teclado simulado):**
1. Ponerse debajo de A, en x 413,5 sobre el pilar, y dar un salto leve. A aparece y el jugador vuelve al pilar.
2. Ir al borde izquierdo del pilar (x 412,4), hacer doble salto hacia arriba y meterse encima de A. Queda en y 1,01 y aguanta 3 s sin caer.
3. Desde A, saltar a la derecha. Doble máximo, máximo + leve y leve + máximo llegan a la cima o a la cámara. Esperar mucho el segundo salto, o salir andando de A antes de saltar, mata.
4. Bajar de la cima saltando. Si se baja andando, se cae sobre los pinchos x 428–430.

| Prueba | Resultado |
|---|---|
| Doble salto directo desde el pilar o el suelo que cae (7 puntos × 3 saltos) | Ninguno pasa. Desde x ≥ 413,8 mueren; desde el pilar se chocan con A y caen en el suelo que cae. |
| Andar | Muere. |
| Llegar al pilar desde la isla x 408 | Vivo, sin revelar A. |
| Recorrido completo por script con la ruta nueva | Sin morir, llave y portal (x 449,15; 75,0 s). |
| Consola | 0 errores, 0 avisos. |

**Riesgos:** los cuadros se ocultan al morir, así que hay que revelarlos en cada vida. El borde de la pared deja caer justo sobre los pinchos x 428–430. Los tiles nuevos de la pared no pasaron por `LevelArtGenerator.ApplyToScene` y pueden verse distintos en los bordes.

**No probado:** teclado físico; revelar B desde otros puntos; aspecto visual de la pared.

### Sección final nueva: salto de fe y pilares (2026-11-08, v14)

Copia previa en `Archive/Level_01_v13b_antes.unity`; final en `Archive/Level_01_v14.unity`. Sin código nuevo. Se volvió a pasar `LevelArtGenerator.ApplyToScene`. **Longitud del nivel: 508 u** (antes 463).

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | La puerta pasa arriba, al final. Hay que caer desde arriba (salto de fe: al saltar no se ve la primera plataforma) a pilares estrechos, algunos con pinchos, y saltar varios hasta la puerta. |
| [Aprobado por Jesús] | Llave arriba, como estaba (x 451; repisa con techo de pinchos). Pilares fijos y que caen mezclados. Puerta más alta que los pilares: el último salto es hacia arriba. Pista del salto de fe a criterio. |
| [Decisión] | El salto de fe se logra con la geometría, sin tocar `CameraFollow`. Desde el borde, la cámara ve hasta y 2,5 y el primer pilar tiene la cima en y 0. Se ve cuando el jugador ya ha caído a y ≈ 2,5. |
| [Decisión] | Pista discreta: mancha gris en el borde del salto (x 465,4) y frase nueva provisional `T_SaltoFe` (x 464,5): "Abajo no hay nada. Te lo prometo." |
| [Decisión] | `T_Puerta` se mueve de x 444 a x 498. La puerta (`DataPortal`) y su luz `Cyan_Portal` pasan de (448; 0,5) a (502,5; 6). |
| [Decisión] | `CameraFollow.maxBounds.x` de 463 a 508. `KillZone`: centro x 249 y ancho 520 (cubre x −11 a 509). |

**Disposición (x; cima y):**

| Elemento | Posición |
|---|---|
| Salida del pozo de la llave | Se abre la pared x 461–463 entre y 7,5 y 9,5. Desde arriba del pozo (el mismo por el que se sube a la llave) se sigue a la derecha. |
| Repisa del salto | x 463–466, cima 7,5 (acantilado `Solid`). Mancha gris en el borde. |
| Pozo | x 466–499, suelo en y −6,5 con pinchos `S6_Pinchos_Pozo` en todo el fondo y 3 manchas grises. |
| P1, fijo (`Platforms`, ladrillo) | x 475–477, cima 0. |
| P2, cae (`S6_Pilar2_Cae`, copia de la repisa de la llave, `fallDelay` 0,35) | Centro (481; −0,5), 2 × 1, cima 0. |
| P3, fijo con pinchos a la izquierda (`S6_Pinchos_P3`, x 485,5) | x 485–488, cima 1. Se pisa la parte derecha. |
| P4, cae (`S6_Pilar4_Cae`) | Centro (492; 0,5), cima 1. |
| P5, fijo y estrecho | x 496–497, cima 2. |
| Repisa de la puerta | x 499–506, cima 5. Puerta en x 502,5. Pared final x 506–508. |

**Recorrido:** coger la llave → bajar → volver al pozo de x 458–461 y subir con doble salto → a la derecha por la salida nueva → saltar desde el borde (salto leve o máximo) → P1 → leve a P2 (sin pararse, cae) → máximo a la parte derecha de P3 → leve a P4 (cae) → leve a P5 → máximo o doble a la repisa → puerta.

| Prueba (teclado simulado salvo lo indicado) | Resultado |
|---|---|
| Andar y dejarse caer desde el borde | Muere (cae en x ≈ 474, antes de P1). |
| Salto leve / máximo desde el borde | Aterrizan en P1 (x 475,9 / 476,2). |
| Doble máximo desde el borde | Aterriza en P2 (x 481). |
| Máximo + leve desde el borde | Muere (cae entre P2 y P3). |
| Recorrido de pilares P1 → repisa de la puerta | 3 de 3 vivos. |
| Quedarse en P2 | Cae y muere. |
| Caer en la mitad con pinchos de P3 | Muere. |
| P5 → repisa con un solo máximo | Llega. |
| Nivel completo por script, con llave | Sin morir; portal abierto en x 501,4, 83,0 s. |
| Consola | 0 errores, 0 avisos. |

**Riesgos:**
- Si el jugador sale por la salida nueva sin la llave, llega a la puerta cerrada y no puede volver: tiene que morir y reaparece en `Checkpoint2` (x 373,5).
- Morir en el pozo o en los pilares devuelve a `Checkpoint2`, a unas 100 u de la sección: se repite la pared alta y la llave.
- La primera pasada del script murió en x 189,6, en la zona de pinchos x 190–192, que no se tocó: saltó tarde por un tirón del editor. Con más margen en el script pasó.

**No probado:** teclado físico; aspecto visual de la sección nueva en el juego; ritmo de la frase nueva junto a `T_Llave`.

### Sección final: bloques sueltos en lugar de pilares (2026-11-08, v15)

Copia previa en `Archive/Level_01_v14b_antes.unity` (= v14); final en `Archive/Level_01_v15.unity`. Sin código nuevo. Se volvió a pasar `LevelArtGenerator.ApplyToScene`. **Longitud: 525 u.** Sustituye la disposición de pilares de v14: se mantienen la llave, la salida, la repisa del salto, la pista y `T_SaltoFe`.

[Aprobado por Jesús] Después del salto de fe, una secuencia de bloques sueltos de ladrillo, separados, que suben bloque a bloque hasta la puerta; algunos con pinchos. Los detalles son [Decisión] dentro de lo pedido.

| Bloque | x | Cima y | Tipo | Salto desde el anterior |
|---|---|---|---|---|
| B1 | 475–477 | 0 | Fijo | Salto de fe: leve o máximo desde el borde (x 465,3). |
| B2 | 480–482 | 1 | Fijo | Leve. |
| B3 | 485,5–487,5 | 1,5 | Cae (`S6_Bloque3_Cae`, 0,35 s) | Leve, sin pararse. |
| B4 | 491–493 | 3 | Fijo; pinchos en la mitad izquierda (`S6_Pinchos_B4`, x 491,5) | Máximo, cayendo en la mitad derecha. |
| B5 | 496–498 | 4,5 | Fijo; pinchos debajo (`S6_Pinchos_B5_Abajo`, x 496,5 y 497,5) | Máximo; quedarse corto contra su cara inferior mata. |
| B6 | 501–503 | 5,5 | Cae (`S6_Bloque6_Cae`) | Leve, sin pararse. |
| B7 | 506–508 | 7 | Fijo; pinchos en la mitad derecha (`S6_Pinchos_B7`, x 507,5) | Máximo, cayendo en la mitad izquierda. |
| B8 | 510–512 | 8,5 | Fijo | Máximo, por encima de los pinchos de B7. |
| Repisa de la puerta | 515–523 | 12,5 | `Solid` | **Doble salto** hacia arriba: con un solo máximo no llega. |

- La puerta (`DataPortal`) y su luz `Cyan_Portal` están en (519; 13,5); `T_Puerta` en x 513. La pared final va de x 523 a 525.
- Bloques fijos en `Platforms` (`Tile_Platform`, ladrillo); los que caen son copias de la repisa de la llave en 2 × 2.
- Fondo del pozo x 466–515 en y −6,5, con `S6_Pinchos_Pozo` en todo su ancho. Manchas grises nuevas en x 489, 495,2 y 504,5.
- `CameraFollow.maxBounds.x` = 525. `KillZone`: centro x 257,5, ancho 537 (x −11 a 526).

| Prueba (teclado simulado) | Resultado |
|---|---|
| Salto de fe leve / máximo | Aterrizan en B1 (x 476,1). |
| Secuencia completa B1 → repisa de la puerta | 3 de 3 vivos. |
| Quedarse en B3 | Cae y muere. |
| B3 → mitad izquierda de B4 | Muere en los pinchos. |
| B4 → B5 con salto leve | Llega (no se queda corto). |
| Andar a la derecha en B7 | Muere en los pinchos. |
| B8 → repisa: un solo máximo / doble máximo / máximo + leve | Muere (2 de 2) / llega (3 de 3) / llega. |
| Nivel completo por script, con llave | Sin morir; portal en x 517,9, 85,8 s. |
| Consola | 0 errores, 0 avisos. |

**Riesgos:** los mismos de v14: salir sin la llave deja al jugador encerrado en la repisa, y morir en esta sección devuelve a `Checkpoint2`.

**No probado:** teclado físico; aspecto visual.

---

## Frases del Sistema iguales al GDD (2026-11-10)

Rama `Nivel01`, ya con el menú y el audio de `feature/audio`. Copia previa en `Archive/Level_01_v15b_antes_frases.unity`. Fuente única: GDD, `03 - Historia y Mundo/04 - Diálogos y Narrativa.md`, Nivel 1.

| Etiqueta | Decisión |
|---|---|
| [Aprobado por Jesús] | Solo se usan frases del GDD, con el texto literal. Se quita todo lo demás. |
| [GDD] | Burlas al morir con las tres reglas: frase única en la primera muerte del juego; otra frase única en la primera muerte en trampa troll (si la primera muerte ya fue troll, se guarda para la siguiente muerte troll); burla al azar cada 3 muertes entre las 6 del GDD. |
| [Decisión] | "Trampa troll", según la definición del GDD: plataforma que cae, checkpoint falso, pincho que cae, bloque invisible y pared falsa. Cuenta como troll una muerte que ocurre hasta 2 s después de activar una de ellas (`TrollTrap.Window`). Los pinchos visibles y el vacío no cuentan. |
| [Decisión] | Las frases fijas mantienen su orden en el GDD, pero se colocan en el sitio equivalente del nivel actual (tabla). |
| Pendiente | El cuadro del Sistema sigue encolando las frases en vez de reemplazarlas (regla de Interfaz Visual). Se cambiará junto con la ventana Sistema.exe, la fuente y el sonido por letra. |

| # GDD | Momento (GDD) | Dónde sale ahora |
|---|---|---|
| 1 | Al aparecer | `T_Inicio_GDD` (x 2), primera frase de la secuencia. |
| 2 | Justo después de la 1 | `T_Inicio_GDD`, segunda frase de la secuencia. |
| 3 | Antes de la primera trampa (pinchos) | `T_PrimerosPinchos` (x 10), antes de los pinchos de x 13. Va en cola tras la 1 y la 2, así que quien corre la lee algo tarde. |
| 4 | Muro del doble salto | `T_DobleSalto` (x 152). |
| 5 | Campo de pinchos | `T_CampoPinchos` (x 319), túnel con pinchos. |
| 6 | Checkpoint falso | `T_CheckpointFalso` (x 174). |
| 7 | Hueco con plataforma invisible | `T_Hueco` (x 212). |
| 8 | Al recoger la llave | `T_Llave` (x 451). |
| 9 | Antes del checkpoint real | `T_AntesCheckpoint` (x 276), antes del checkpoint de x 289. |
| 10 | Antes de la última trampa | `T_UltimaTrampa` (x 464,5), en el borde del salto de fe. |
| 11 | Frente al portal | `T_Puerta` (x 513), se repite cada vez que se entra. |
| 12 | Al completar el nivel | `GoalDoor.completedLine`. |
| Primera muerte | — | `NarratorDeathTaunts.firstDeathLine`. |
| Primera muerte troll | — | `NarratorDeathTaunts.firstTrollDeathLine`. |
| Cada 3 muertes | — | `Data/Narrator/NivelUno_Burlas`: las 6 frases del GDD. |

**Eliminado:** `T_Demo` ("Bienvenido a la demo…"), `T_Suelo` ("¿Ves? Solo era el suelo…"), `T_Descanso1` ("Descansa, Novato…"), `T_ParedesFalsas` ("Las paredes de este juego…"), `T_Atajo` ("...Eso no estaba en el manual."), `T_SalidaFalsa` ("¿La salida? Ah, eso…"), `T_Checkpoint2` (su frase pasa a x 276), `T_Salida` (su frase pasa al salto de fe), `T_SaltoFe` ("Abajo no hay nada…"), la broma de la puerta en tres frases y la burla "Es la demo, Novato. La DEMO.". La primera frase del inicio y la #5 se corrigieron al texto del GDD.

**Código:** nuevo `Traps/TrollTrap.cs`. Cambian `NarratorDeathTaunts.cs` (las tres reglas), `LethalHazard.cs` (opción `trollTrap`; activada en `S3_CheckpointFalso` y en el prefab `FakeCheckpoint`) y, para marcar la trampa troll, `FallingPlatform`, `FallingSpike`, `InvisibleBlock` y `FakeWall`.

| Prueba | Resultado |
|---|---|
| Recorrido completo por script con registro de frases | Salen las 12 frases fijas, una vez cada una y en este orden: 1 y 2 (inicio), 3 (x 9), 4 (151), 6 (173), 7 (211), 9 (275), 5 (318), 8 (451), 10 (463,5), 11 (512) y 12 al cruzar la puerta. Sin morir; consola limpia. |
| Muertes forzadas | 1.ª en pinchos visibles → "Y ahí va la primera…"; 2.ª en checkpoint falso → "Esa trampa lleva años…"; 3.ª y 6.ª → burla al azar del GDD; 4.ª en checkpoint falso → nada (la frase troll ya salió); 5.ª → nada. |
| Recorrido por script | Una de dos pasadas murió en el primer bloque tras el salto de fe; tres saltos aislados aterrizan bien. Probablemente es un tirón del script, como el de x 189,6. |

**Fuera del nivel:** el menú de pausa (de un compañero) tiene textos del Sistema que no están en el GDD: "Enserio necesitas un respiro? Apenas vamos empezando" y "JAJAJA sabia que no ibas a poder escapar, Mediocre". No se han tocado; pendiente de Jesús.

## Ventana «Nivel completado» y restauración de pausa y guardado (2026-11-10)

Copia previa en `Archive/Level_01_v15c_antes_nivelcompletado.unity`. [Aprobado por Jesús]: implementar solo lo que define el GDD, sin nada provisional.

| Etiqueta | Decisión |
|---|---|
| [GDD] | Interfaz Visual, estado Éxito: ventana emergente «Nivel completado» con borde verde azulado (#3A9B8D). Muestra tiempo, muertes y un comentario mordaz de Sistema.exe. Aparece de golpe, sin transiciones suaves. Cuerpo gris carbón (#342F33) y texto crema (#FDFEC2), de la paleta. |
| [GDD] | El comentario es la frase #12 ("...Eso no debía pasar…"). Para no mostrarla dos veces, se dice en la ventana y no en el cuadro del Sistema (`GoalDoor.lineShownByCompleteView`). |
| [GDD] | Datos y Persistencia: se guarda al completar el nivel (`LevelProgressPersistence` escucha `GoalDoor.LevelCompleted` y llama a `SaveNow`). |
| [Decisión] | Al abrirse la ventana se desactivan el movimiento y el salto y se anula la velocidad horizontal, porque sin control el personaje se deslizaba. El cronómetro ya se paraba. |
| [Decisión] | Fuente: la del HUD (LiberationSans). Press Start 2P no está en el proyecto. |
| Hallazgo | Al unir `Nivel01` con `feature/audio`, `Level_01` perdió el menú de pausa (`PauseMenu`) y el guardado de checkpoints (`LevelProgressPersistence`) que hizo `feature/menu`, y los ids de los checkpoints. Se restauraron con la configuración de su rama: `LevelProgressPersistence` en `RespawnManager` (respawn, stats, jugador, llave, cámara, checkpoints) e ids `level01_cp01` (x 289) y `level01_cp02` (x 373,5). |

**Huecos del GDD (pendientes del equipo, no se implementan):** botones o cierre de la ventana, efecto de transición al cambiar de nivel, qué hacer sin nivel siguiente, animación de apertura de la puerta (sin fotogramas) y fanfarria de victoria (sin archivo). Hoy, la única salida tras completar es la pausa (ESC) → volver al menú.

**Código:** nuevo `UI/LevelCompleteView.cs`. Cambian `GoalDoor.cs` (`CompletedLine` y `lineShownByCompleteView`) y `LevelProgressPersistence.cs` (guardar al completar).

| Prueba (teclado simulado) | Resultado |
|---|---|
| ESC en mitad del nivel | Pausa (timeScale 0) y vuelve con otro ESC. |
| Cruzar la puerta con la llave | La ventana aparece en el mismo instante: "Nivel completado", "Tiempo: 00:02", "Muertes: 0" y la frase #12. |
| Pulsar derecha y salto 1 s con la ventana abierta | El personaje no se mueve; cronómetro parado. |
| ESC con la ventana abierta | Pausa y reanuda. |
| Consola | 0 errores, 0 avisos. |

**No probado:** el aspecto de la ventana en pantalla (la captura de cámara no incluye la UI), el flujo Menú → Continuar después de completar, y teclado físico.

---

## Ajustes para cumplir el GDD antes de la entrega (2026-11-13)

Contexto: el GDD ya no se puede modificar y la evaluación es contra él tal como está. Copias previas en `Scenes/Archive/Level_0X_antes_gdd_final.unity` (y `Level_02/03_antes_frases_gdd.unity`).

| Cambio | Justificación en el GDD | Dónde |
|---|---|---|
| Frases del nivel 3 completas como en el GDD (#3, #4, #5); se quitan "Umbral de salida alcanzado.", las 3 frases viejas de la puerta y las 2 del nivel 1 | Diálogos › Nivel 3; "Ninguna frase se repite entre niveles" | `Level_03` |
| Se quita la frase del fragmento oculto | No existe en el GDD | `HiddenFragment.cs` |
| Las 9 frases restantes de la tabla del nivel 2 (la #10 ya estaba en la ventana final); se conserva la de la ficha del Sistema | Diálogos › Nivel 2; Enemigos y NPC | `Level_02`, `Referencias del recorrido/Sistema - 0X` |
| Indicaciones del tutorial solo de teclado | Alcance › No incluido: mandos | `TutorialHints.cs` |
| Error de guardado solo en consola, sin frase en pantalla | No hay frase del GDD para ese caso | `LevelExitTransition.cs` |
| Reaparición en 0,15 s | Alcance y Requisitos Técnicos: menos de 0,2 s | `RespawnManager` (los 3 niveles) |
| Copia `.bak` al sobrescribir el guardado | Datos y Persistencia › Formato y escritura | `SaveGameService.cs` |
| Destello rojo breve con parpadeo al morir | Interfaz Visual › Estados › Muerte | `DeathFlash.cs`, `HUD/DestelloMuerte` |
| Música al 65 % y atenuada al morir o mientras habla el Sistema | Audio y Música › Mezcla | `MusicMix.cs` en la fuente de música |
| Pixel Perfect Camera 640×360 (tamaño ortográfico 5,625) en los 3 niveles | Entornos › Medidas; Riesgos técnicos | Cámara de `Level_01` y `Level_03` (el 2 ya la tenía) |
| Sin sombras en tiempo real (39 luces) | Entornos › Iluminación; Requisitos Técnicos | Los 3 niveles |
| Manchas antiguas: 5 en el nivel 1, 6 en el nivel 2 (el 3 ya tenía 55) | Premisa › Preparación del giro: "aumentan en cada nivel" | `ManchasAntiguas` |

**Pruebas (script):** nivel 3 completo sin morir, con las 8 frases en orden y ventana final (sin comentario); 10 frases del nivel 2 al pasar por sus puntos; en los 3 niveles, muerte → destello visible, reaparición en ~150 ms y música de 0,65 a 0,26 y de vuelta. Consola: 0 errores, 0 avisos.

**No probado:** aspecto en pantalla del destello, de la cámara 640×360 (en los niveles 1 y 3 se ve un 20 % menos de escenario que antes, porque el tamaño ortográfico pasa de 7 a 5,625) y de las luces sin sombra; teclado físico.

### Final único "Salir" y sala de servidores (2026-11-13)

Copia previa en `Scenes/Archive/Level_03_antes_final.unity`.

| Cambio | Justificación en el GDD | Dónde |
|---|---|---|
| Al completar el nivel 3, tras 3 s, se pasa a la sala de servidores | Premisa › Eventos 4 y 5: "Acceso a la sala de servidores" | `Level_03/SalidaNivel` (`LevelExitTransition`) |
| Sala de servidores sin trampas y sin frases del Sistema | Premisa › Acto 3: "No es un nivel con trampas"; Diálogos › Sala de servidores: "el Sistema guarda silencio" | `Scenes/SalaServidores.unity` |
| 30 manchas antiguas grises en suelo, paredes y techo como pruebas del jugador anterior | Premisa › Preparación del giro: manchas = "las muertes del jugador anterior" | `SalaServidores/ManchasAntiguas` |
| Portal de datos sin llave como salida | Premisa › Acto 3: "desde donde sale del juego"; Eventos 6: "el juego deja salir a Emmet" | `SalaServidores/DataPortal` + `SalidaJuego` |
| Pantalla final con el párrafo literal de Premisa › Finales › "Qué ocurre", en Press Start 2P crema sobre gris carbón | Premisa › Finales › "Cómo se muestra": párrafo en pantalla | `Scenes/Final.unity`, `UI/FinalScreen.cs` |
| Vuelta al menú principal: con cualquier tecla tras 3 s o sola a los 15 s; corte directo, sin créditos | El GDD no define créditos ni transición | `FinalScreen.cs` |
| `SalaServidores` y `Final` añadidas a Build Settings tras `Level_03` | — | `ProjectSettings/EditorBuildSettings.asset` |

El guardado no tiene un campo "juego completado". Al salir de la sala, `LevelExitTransition` guarda la escena `Final` como punto de la partida, así que "Continuar" lleva a la pantalla final.

**Prueba (script):** Level_03 completado → sala de servidores a los 6 s (sin trampas y sin frases) → portal → pantalla final con el párrafo en Press Start 2P → menú principal a los 15 s. Consola: sin errores. Solo queda el aviso del vídeo del menú, que ya existía.

**No probado:** aspecto visual de la sala y de la pantalla final, y "Continuar" desde el menú después del final.

### Configuración en el menú principal (2026-11-13)

Copia previa en `Scenes/Archive/Menu-Principal_antes_config.unity`. El menú de pausa no se toca.

| Cambio | Justificación en el GDD | Dónde |
|---|---|---|
| Botón "CONFIGURACIÓN" en el menú principal, con panel AUDIO (VOLUMEN GENERAL, MÚSICA, EFECTOS) y VIDEO (PANTALLA COMPLETA) | UI y Menús: Menú principal › Configuración › Audio y Video; Interfaz Visual › Pantallas | `MenuPrincipal.uxml`, `MenuStyle.uss`, `MenuSettings.cs` |
| Valores `volumenMaster`, `volumenMusica`, `volumenSFX` (0 a 1) y `pantallaCompleta`, guardados en JSON con `.tmp` y réplica `.bak`, y aplicados al arrancar | Datos y Persistencia › configuracion; Formato y escritura | `Core/GameSettings.cs` → `configuracion.json` |
| La música (niveles y menú) y los efectos (SfxPlayer y PlayerAudio) usan esos volúmenes | Audio y Música › Mezcla | `MusicMix.cs`, `SfxPlayer.cs`, `PlayerAudio.cs` |
| `MenuPresentation` incluye el botón nuevo en su selección visual | — | `MenuPresentation.cs` (una línea) |

**Decisión:** la configuración va en un archivo propio (`configuracion.json`) y no dentro de la partida, porque "Crear partida" reemplaza la partida y borraría los ajustes. El GDD pide un único JSON; se anota como diferencia. Controles y Accesibilidad no se incluyen porque sus tablas están vacías en el GDD. El texto del botón "VOLVER" no está en el GDD; es el mínimo necesario para cerrar el panel (también se cierra con ESC).

**Prueba (script):** botón visible; al abrir, el menú queda bloqueado; general 0,8 → AudioListener 0,80; música 0,5 → música del menú de 0,68 a 0,34; efectos 0,3; al cerrar se guarda; al reabrir conserva los valores; segundo guardado crea el `.bak`. Se dejaron los valores en 1/1/1/pantalla completa. Consola: sin errores.

**No probado:** aspecto del panel en pantalla y navegación con flechas del teclado dentro del panel.

### Sala de servidores rediseñada (2026-11-13) [Aprobado por Jesús]

Copia previa en `Scenes/Archive/SalaServidores_antes_rediseno.unity`.

| Elemento | Justificación |
|---|---|
| Se quitan las 30 manchas de la sala | Petición de Jesús (sin gore) y coherencia con el GDD: el jugador anterior no superó el último nivel (Mundo), así que nunca estuvo en la sala. Lo que queda de él son sus datos. Las manchas grises de los niveles 1–3 (pista aprobada) se mantienen. |
| Recorrido de unas 64 celdas en 4 etapas: del deterioro (racks rotos, rojo oscuro) a lo limpio (racks limpios, verde azulado) | Premisa › Acto 3; Entornos › Zonas Colapsada → Inicial; paleta de Dirección de Arte. Sin luces nuevas (iluminación plana). |
| Parche de Estabilidad: nodo de datos en la entrada; al tomarlo, aura de código binario verde, inmunidad a las zonas de corrupción y reconstrucción de la pasarela rota. Solo en la sala; sin inventario | Objetos › Parche: "corrige temporalmente los errores del entorno", "código binario verde". La corrupción es un error del entorno y no una trampa, así que "sin trampas" se mantiene. Autor = el nombre borrado de la Partida 0 (motivo narrativo aprobado). |
| Pantallas: Partida 1 (muertes y tiempo reales), Partida 0 (nombre corrupto, 99 999+ muertes, 26 años que siguen contando, último punto Nivel 3), sus muertes más repetidas (láseres del nivel 2, circuitos en colapso y salto milimétrico del nivel 3), evidencia "RENDIDO · PROCESO ACTIVO: SISTEMA.EXE" con su retrato y "Dos partidas" | Pistas de Premisa (aprobadas: conoce las trampas; **pendiente de confirmar**: registro con nombre corrupto y contador enorme); Mundo (26 años, se rinde en el último nivel). Los textos son contenido nuevo aprobado por Jesús. |
| Silencio: ventana del Sistema abierta 7 s solo con el cursor, en la evidencia | Diálogos: "guarda silencio por primera vez"; "Después de la línea 8, el Sistema no vuelve a hablar". Sin burlas en la sala (se quitó `NarratorDeathTaunts`) y sin música. |
| Cierre: al cruzar el portal la Partida 1 pasa a COMPLETADO; la sala se apaga sola de la entrada al portal; solo queda el portal; luego la pantalla final | Mundo, regla 5: el Sistema no produce efectos, es la sala la que se apaga. Personajes › arco de Emmet. |
| Contradicción del GDD | Eventos #6 ("El Sistema exige que se quede") frente a Diálogos ("no vuelve a hablar"): se toma como exigencia la línea 8 en la puerta del nivel 3 y la sala queda en silencio. |

**Código:** `Level/ServerRoom/StabilityPatch.cs`, `CorruptionZone.cs`, `RebuildingWalkway.cs`, `ServerRoomDirector.cs`; `Narrator.ShowSilence` y su soporte en `NarratorUI` (cursor sin texto). Sprite técnico `Pixel_Blanco.png` para los marcos de las pantallas.

**Prueba (script):** Level_03 → sala; parche aplicado en x 6; barreras y pasarela superadas sin morir; pantallas con datos reales; silencio de 7 s en la evidencia (x 46,5); ninguna frase del Sistema en la sala; portal → COMPLETADO → apagón (51 de 56 sprites; portal intacto; controles bloqueados) → pantalla final → menú. Consola: sin errores (solo el aviso previo del vídeo del menú).

**No probado:** aspecto en pantalla (tamaños de texto, legibilidad, encuadre), y la muerte por corrupción sin parche (no se puede llegar a ella sin tomarlo).

#### Ambiente sonoro de la sala (audio de Jesús)

Copia previa en `Scenes/Archive/SalaServidores_antes_ambiente.unity`. El audio que mandó Jesús (`.mp4` de WhatsApp, solo audio AAC, 32 s) se convirtió a `.wav` con el convertidor de Windows (Media Foundation), sin cambiar su contenido: `Materials/Audio/Amb_SalaServidores.wav`, carga en streaming. En la sala suena en bucle, en 2D (`AmbienteSala`), con el volumen de Música de la Configuración. `ServerRoomDirector` lo baja al 30 % durante los 7 s de silencio del Sistema y lo apaga durante el apagón. La música del nivel sigue cortada en la puerta. **Pendiente:** probarlo en Play; el audio es bajo y ya va al 100 %.

#### Más protagonismo para el parche (propuesta aprobada por Jesús)

Copia previa en `Scenes/Archive/SalaServidores_antes_parche_protagonista.unity`.

- **Etapa 0 propia:** nicho enmarcado por dos columnas de cable, con pedestal y una pantalla verde. Al entrar, la pantalla escribe letra a letra el `LEEME.TXT` (en la pantalla del nodo, no en la ventana del Sistema). El icono del parche aparece cuando termina de escribirse; antes, la primera barrera de corrupción bloquea el paso (colisionador sólido que se retira con el parche).
- **Aplicarlo es un momento:** Emmet se detiene 1,5 s con "APLICANDO PARCHE..." y una barra de 0 a 100 %, mientras el binario verde sube desde sus pies. Después: "ESTADO: APLICADO / USUARIO: NOVATO" y el aura se queda.
- **Revelación:** la evidencia añade "AUTOR DE PARCHE_ESTABILIDAD: PARTIDA 0".
- **Cierre:** "PARCHE_ESTABILIDAD: USADO 1 VEZ" en la pantalla de las dos partidas; el aura del parche es lo último que se apaga antes del portal.

**Textos nuevos (contenido no presente en el GDD, aprobado por Jesús):** `LEEME.TXT - PARCHE_ESTABILIDAD.EXE / AUTOR: #%&#@? / EL JUEGO SE ESTÁ ROMPIENDO. / ESTO CORRIGE LOS ERRORES DEL ENTORNO. / LO HICE PARA SALIR. / SI LEES ESTO, YO NO LO LOGRÉ. / ÚSALO TÚ.` La cuarta línea es la función literal del GDD. Se cambió "LA SALA ESTÁ ROTA" por "EL JUEGO SE ESTÁ ROMPIENDO" porque el autor nunca llegó a la sala (Mundo); el deterioro del juego sí lo vivió como jugador.

**Prueba (script):** bloqueado ante la barrera mientras se escribe el LEEME; parche aplicado a los 6,4 s de entrar, en 1,48 s; silencio en x 46,5; evidencia con la línea del autor; al cruzar el portal: COMPLETADO + "USADO 1 VEZ"; aura visible a los 2,0 s y apagada a los 2,8 s, portal intacto; pantalla final y menú. Consola: sin errores.

### Justificaciones (contradicciones del GDD y lectura elegida)

| Tema | Qué dice el GDD | Lectura elegida |
|---|---|---|
| Final | Premisa y Registro de Decisiones: final único "Salir". Niveles, Misión 3 y Enemigos y NPC: elegir Salir o Quedarse. Datos y Persistencia: 3 finales | Final único "Salir": es la decisión registrada y la más reciente |
| Parche de Estabilidad | Objetos y Coleccionables lo describe; Alcance excluye los ítems recolectables y Datos y Persistencia dice "no hay inventario" | No se implementa: manda el Alcance |
| Reaparición | Mecánicas: menos de 1 s; Alcance y Requisitos: menos de 0,2 s | 0,15 s: cumple las dos |
| Dash | No está en Mecánicas; Niveles lo introduce en el nivel 2 | Se respalda en la ficha del nivel 2 |
| Coyote time | Alcance lo incluye; Mecánicas no lo describe | Se respalda en el Alcance (0,1 s) |
| Tablas vacías | Controles, Sistemas, Progresión, Reglas y Estados, Pantallas (UI y Menús), Guardado y Configuración, Accesibilidad son plantillas sin rellenar | No piden nada concreto; se sigue lo definido en otras secciones |
| Palanca y diamante falso | Solo aparecen como arte en Entornos; no están en Mecánicas ni en las fichas de nivel | Recursos de arte sin uso en el diseño de niveles |

---

## Integración de sprites del Nivel 1 y cambios de `develop` (2026-11-11)

Commits de unión: `0770893` (`feature/sprites_level01`) y `0816ed4` (`develop`); reaplicación en `1d76881`. Respaldo previo: commit `7501225`.

| Etiqueta | Decisión |
|---|---|
| Unión | Entran los sprites del Nivel 1, la fuente Press Start 2P y la calavera del HUD, los audios nuevos y los ajustes del menú de pausa. |
| Conflicto | `Level_01.unity`: se tomó la escena de ellos (misma geometría que nuestra v15) y se reaplicó por script lo nuestro: disparadores con las frases #1–#11 del GDD, frase #12 en la ventana, burlas por muerte (`NarratorDeathTaunts`), `trollTrap` en el checkpoint falso y la ventana «Nivel completado». El menú de pausa y `LevelProgressPersistence` se dejan como vienen de su rama. |
| Conflicto | `FallingPlatform`, `FallingSpike`, `InvisibleBlock`: se conservan las dos líneas (`TrollTrap.Mark()` nuestro y `SfxPlayer.Play(...)` de ellos). |
| [Decisión] | La ventana «Nivel completado» usa ahora Press Start 2P, que ya está en el proyecto. |

| Prueba (recorrido completo automatizado) | Resultado |
|---|---|
| Frases del Sistema | Las 12 del GDD, en orden y en su sitio. |
| Ventana al cruzar la puerta | Visible: "Nivel completado", "Tiempo: 01:26", "Muertes: 0" y la frase #12. |
| Consola | 0 errores, 0 avisos. |

**No probado:** aspecto visual de los sprites y la ventana en pantalla, y teclado físico.

---

## Diálogos del Sistema – Nivel 1

> **Desfasado desde el 2026-11-04:** esta tabla describe el nivel original. Las frases vigentes están en "Nivel 1: rediseño troll".

Sacado de la escena `Level_01` y de sus assets el 2026-10-27, en el orden en que el jugador los oye. Todos los `NarratorTrigger` cuelgan de `NarratorTriggers`, en la raíz de la escena (antes colgaban de `Sistema/NarratorTriggers`) y son instancias de `Prefabs/Narrator/NarratorTrigger`. Salvo T10, son franjas verticales de 1 × 18 unidades (de y = −5 a y = 13), así que saltan al cruzar esa x a cualquier altura.

| # | GameObject | Posición (x; y) | Tamaño del trigger | Zona | Frase exacta | `repeat` | Origen |
|---|---|---|---|---|---|---|---|
| 1 | `T0_Inicio_GDD` | (2; 4) | 1 × 18 | Inicio (el jugador aparece dentro, en x = 2) | "¿Te vas tan pronto? Apenas estábamos empezando..." | No | **GDD literal** (Enemigos y NPC, "Inicio Nivel 1") |
| 2 | `T1_Demo` | (7; 4) | 1 × 18 | Inicio | "Bienvenido a la demo, Novato. Hasta un niño la termina." | No | Provisional |
| 3 | `T2_Troll1` | (20; 4) | 1 × 18 | Zona A, antes del troll 1 | "Un saltito sobre esos pinchos. Sin miedo, Novato." | No | Provisional |
| 4 | `T3_DobleSalto` | (30; 4) | 1 × 18 | Muro del doble salto | "¿Una pared? Qué pena que solo sepas saltar una vez... ¿o no?" | No | Provisional |
| 5 | `T4_Desafio` | (44; 4) | 1 × 18 | Desafío (campo de pinchos) | "Plataformas y pinchos. Nada que un niño de 2003 no supere." | No | Provisional (pista "niño de 2003") |
| 6 | `T5_Troll2` | (69; 4) | 1 × 18 | Troll 2 (checkpoint falso) | "¡Mira, un punto de control! Tócalo, es gratis." | No | Provisional |
| 7 | `T6_Troll3` | (75; 4) | 1 × 18 | Troll 3 (hueco imposible) | "Ese hueco no se salta. Créeme: lo sé." | No | Provisional (pista "lo sé") |
| 8 | `T10_Llave` | (84,5; 6) | 2 × 1,5 (x 83,5–85,5; y 5,25–6,75) | Llave, sobre el bloque ayudante del troll 3 | "¿Una llave? Cuídala bien... si te mueres, se queda aquí conmigo." | No | Provisional |
| 9 | `T7_ZonaB` | (99; 4) | 1 × 18 | Zona B, justo antes del checkpoint real | "¿Sigues aquí? Qué terco. Descansa... no tengo prisa." | No | Provisional |
| 10 | `T8_Troll4` | (117; 4) | 1 × 18 | Final, antes del troll 4 | "La salida está justo ahí. ¡Salta con todas tus fuerzas!" | No | Provisional |
| 11 | `T9_Puerta` | (127; 4) | 1 × 18 | Delante del portal | "La puerta solo se abre con su llave. ¿La traes... o la dejaste por ahí?" | **Sí** | Provisional |
| 12 | `DataPortal` (`GoalDoor.completedLine`) | (130; 7,5) | — | Portal de datos, al completar el nivel con la llave | "...Eso no debía pasar. Bueno. El siguiente nivel no será tan amable." | — (una vez) | Provisional |

**Burlas al morir** (`Sistema` → `NarratorDeathTaunts`): `everyNDeaths = 3`, asset `Assets/Data/Narrator/NivelUno_Burlas.asset`. Hablan en la 1.ª muerte y después en la 3.ª, 6.ª, 9.ª... Eligen una frase al azar, que puede repetirse. Todas son **provisionales**:

1. "¿Ya? Ni siquiera había empezado a mirar."
2. "El suelo no muerde. Bueno... a veces sí."
3. "Tómate tu tiempo, Novato. Aquí sobra."
4. "Esa trampa lleva años esperando a alguien como tú."
5. "Muerte registrada. Llevo muy bien la cuenta."
6. "Tranquilo. A todos nos pasa la primera vez." (pista)
7. "Es la demo, Novato. La DEMO."

**Otras llamadas a `Narrator.Say`** en `Assets/Scripts`: solo hay tres. Están en `NarratorTrigger`, `NarratorDeathTaunts` y `GoalDoor` (`completedLine`). No hay más frases escritas en el código.

**Notas:**
- Los triggers no repetibles no vuelven a hablar tras morir y reaparecer, porque `said` no se reinicia.
- Una burla de muerte o la frase de otro trigger corta la frase que se esté mostrando: `NarratorUI` muestra solo una cada vez.
- `T9_Puerta` también habla si ya llevas la llave.
- La decisión 11 de la sección "Nivel 1" ("No hay... llave") quedó desfasada desde que se añadió la llave.

---

## Mapa de archivos del proyecto

Inventario de `Assets` revisado el 2026-10-27. `TextMesh Pro/` y `Welcome/` son de Unity y no se listan.

### Scripts (`Assets/Scripts/`)

| Ruta | Para qué sirve |
|---|---|
| `Core/CameraFollow.cs` | Cámara que sigue al jugador con suavizado y límites del nivel. |
| `Core/RunStats.cs` | Cronómetro y contador de muertes de la partida. |
| `Level/BloodStainSpawner.cs` | Deja una mancha en el punto de cada muerte, con variante, giro, volteo y escala al azar. |
| `Level/Checkpoint.cs` | Mecánica 05: fija el punto de reaparición al tocarlo. |
| `Level/GoalDoor.cs` | Mecánica 06: puerta o portal de meta, con llave opcional, efecto de desbloqueo y frase final. |
| `Level/LevelKey.cs` | Mecánica 06: llave que sigue al jugador y se pierde si muere antes de un checkpoint nuevo. |
| `Level/RespawnManager.cs` | Mecánicas 04 y 05: reaparición y reinicio de trampas. |
| `Narrator/Narrator.cs` | Evento estático `Narrator.Say` para hacer hablar al Sistema. |
| `Narrator/NarratorDeathTaunts.cs` | Burlas al morir (1.ª muerte y cada N). |
| `Narrator/NarratorLines.cs` | ScriptableObject con listas de frases. |
| `Narrator/NarratorTrigger.cs` | Zona que hace hablar al Sistema al entrar el jugador: una frase o una secuencia. |
| `Narrator/NarratorUI.cs` | Texto del Sistema letra a letra (TMP); las frases sueltas no cortan una secuencia. |
| `Player/PlayerDeath.cs` | Mecánica 04: muerte del jugador y evento `Died`. |
| `Player/PlayerGroundCheck.cs` | Detección de suelo por normales de contacto. |
| `Player/PlayerJump.cs` | Mecánica 02: salto leve/máximo y doble salto. |
| `Player/PlayerMovement.cs` | Mecánica 01: movimiento lateral con arranque y frenado en seco. |
| `Traps/FallingPlatform.cs` | Mecánica 07: plataforma que cae y se restaura al reaparecer. |
| `Traps/FallingSpike.cs` | Mecánica 08: pincho que tiembla y cae del techo; se restaura al reaparecer. |
| `Traps/FakeWall.cs` | Mecánica 09: pared que se atraviesa y se vuelve semitransparente con el jugador dentro. |
| `Editor/BloodStainSpriteGenerator.cs` | Editor: regenera las manchas (`Stain_01..04`). |
| `Editor/PlaceholderSpriteGenerator.cs` | Editor: regenera `Grieta_Overlay`, `Candado` y `Roca_Mortal`. |
| `Traps/InvisibleBlock.cs` | Mecánica 03: bloque invisible que aparece al golpearlo desde abajo. |
| `Traps/LethalHazard.cs` | Mata al tocar: pinchos, zona de caída y checkpoint falso. |
| `UI/StatsHUD.cs` | Muestra el cronómetro y las muertes. |

**Scripts de editor de generación de arte:** solo `Scripts/Editor/BloodStainSpriteGenerator.cs` (genera las manchas; menú Unknown Game > Regenerar sprites de manchas). El resto del arte procedural se generó con comandos puntuales que no se guardaron.

### Prefabs (`Assets/Prefabs/`)

- `Player/Player.prefab`, que incluye la luz hija `PlayerLight`.
- `Level/Checkpoint.prefab`, `Level/DataPortal.prefab` (portal del Nivel 1), `Level/GoalDoor.prefab` (puerta de la Sandbox) y `Level/LevelKey.prefab`.
- `Traps/Spikes.prefab`, `Traps/InvisibleBlock.prefab`, `Traps/FallingPlatform.prefab`, `Traps/FallingSpike.prefab`, `Traps/FakeWall.prefab`, `Traps/DeadlyRock.prefab`, `Traps/KillZone.prefab` y `Traps/FakeCheckpoint.prefab`.
- `Effects/BloodStain.prefab`, `Effects/DeathParticles.prefab` y `Effects/OldBloodStain.prefab` (mancha gris antigua).
- `UI/HUD.prefab`.
- `Narrator/Sistema.prefab` (añadido el 2026-10-27): el objeto `Sistema` con `NarratorDeathTaunts` (`NivelUno_Burlas` por defecto, cada 3 muertes) y su UI (`SistemaUI` → `SistemaText`, con `NarratorUI`). En cada nivel nuevo se arrastra y se cambia el asset de burlas.
- `Narrator/NarratorTrigger.prefab` (añadido el 2026-10-27): una franja trigger de 1 × 18 con `line` vacía y `repeat` desactivado. La frase, la posición y, si hace falta, el tamaño se ponen en cada instancia.
- `Level/LevelSystems.prefab` (añadido el 2026-10-27): agrupa `RespawnManager`, `RunStats` y `BloodStains` como hijos. Las referencias a `Player` y `LevelStart` se asignan en cada escena y quedan como override de la instancia; en el asset están vacías.

### Escenas (`Assets/Scenes/`)

- `Level_01.unity`: Nivel 1. Desde el 2026-10-27 es la **única** escena de Build Settings.
- `Sandbox/Sandbox_Player.unity`: prueba de mecánicas.
- `Archive/Level_01_v1.unity`: copia de `Level_01` antes de la fusión; abre bien. No está en Build Settings. Usa los mismos prefabs, así que si se cambia un prefab también cambia en la copia.
- `Archive/Level_01_v2.unity`: nivel fusionado (etapas 1 a 3) antes del rediseño troll; abre bien. No está en Build Settings. Mismos prefabs: si se cambia un prefab también cambia aquí.
- `Archive/Level_01_v3.unity`: nivel antes de los pisos disfrazados y las trampas sorpresa; abre bien. No está en Build Settings. Comparte prefabs y tiles.
- `Archive/Level_01_v4.unity`: nivel antes de la etapa 1 de mejoras; se copió pero no se abrió para comprobarla. No está en Build Settings.
- `Archive/Level_01_v5.unity`: nivel antes del rediseño de x 52–66; se copió pero no se abrió para comprobarla. No está en Build Settings.
- `Archive/Level_01_v6.unity`: nivel antes de la etapa 2 de mejoras; se copió pero no se abrió para comprobarla. No está en Build Settings.
- `Archive/Level_01_v7.unity`: nivel antes de la etapa 3 (331 u); se copió pero no se abrió para comprobarla. No está en Build Settings.
- `Archive/Level_01_v8.unity`: nivel antes de la corrección de checkpoints, islas y bloques (463 u); se copió pero no se abrió para comprobarla. No está en Build Settings.
- `Archive/Level_01_v9.unity`: copia idéntica de `Level_01` con la corrección de checkpoints, islas y bloques (2026-11-08). No está en Build Settings. **Las copias de `Archive` no son el nivel que se edita.**
- `Archive/Level_01_v9b_antes.unity`: igual que v9; copia de seguridad antes de bajar pinchos y bloques. No está en Build Settings.
- `Archive/Level_01_v10.unity`: copia idéntica de `Level_01` con pinchos de islas a y 7 y bloques S5 a y 1,9 (2026-11-08). No está en Build Settings.
- `Archive/Level_01_v10b_antes.unity`: igual que v10; copia de seguridad antes de acelerar los pinchos. No está en Build Settings.
- `Archive/Level_01_v11.unity`: copia idéntica de `Level_01` con los pinchos de las islas a gravedad 5 (2026-11-08). No está en Build Settings.
- `Archive/Level_01_v12.unity`: copia idéntica de `Level_01` con los cuadros S5 a y 1,4 (2026-11-08). No está en Build Settings.
- `Archive/Level_01_v12b_antes.unity`: igual que v12; copia de seguridad antes de la pared alta. No está en Build Settings.
- `Archive/Level_01_v13.unity`: copia idéntica de `Level_01` con la pared x 421–423 hasta y 5 y los cuadros como escalón (2026-11-08). No está en Build Settings.
- `Archive/Level_01_v13b_antes.unity`: igual que v13; copia de seguridad antes de la sección final nueva. No está en Build Settings.
- `Archive/Level_01_v14.unity`: copia idéntica de `Level_01` con el salto de fe, los pilares y la puerta arriba (508 u, 2026-11-08). No está en Build Settings.
- `Archive/Level_01_v14b_antes.unity`: igual que v14; copia de seguridad antes de los bloques sueltos. No está en Build Settings.
- `Archive/Level_01_v15.unity`: copia idéntica de `Level_01` con la secuencia de 8 bloques sueltos y la puerta en (519; 13,5) (525 u, 2026-11-08). No está en Build Settings.
- `Backups/Level_01_Backup_2026-11-04.unity`: copia idéntica anterior; se puede borrar a mano.
- `SampleScene.unity`: viene de la plantilla de Unity y no se usa. Se quitó de Build Settings el 2026-10-27, pero el archivo sigue en el proyecto.

### Datos, materiales y ajustes

- **ScriptableObjects:** `Data/Narrator/NivelUno_Burlas.asset` (`NarratorLines`).
- **PhysicsMaterial2D:** `Materials/Physics/PM_NoFriction.physicsMaterial2D`, que usa el jugador.
- **Materiales de render propios:** ninguno.
- **Perfil de Volume:** `Settings/Level01_PostFX.asset`. Es un asset, no está embebido en la escena, e incluye Bloom, Vignette, Chromatic Aberration y Film Grain como sub-assets.
- **Input:** `Settings/InputSystem_Actions.inputactions`.
- **URP (de la plantilla):** `Settings/UniversalRP.asset`, `Renderer2D.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `DefaultVolumeProfile.asset`, `Lit2DSceneTemplate.scenetemplate` y `Scenes/URP2DSceneTemplate.unity`.
- **TMP:** se usa la fuente por defecto, `TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` (más su Fallback). No hay assets TMP propios.

### Sprites, tiles y paletas (`Assets/Sprites/`)

- `Environment/`: `Tile_Metal.png`, `Tile_Platform.png`, `BG_Panels.png`, `Checkpoint_Flag.png`, `Portal_Datos.png`, `Grieta_Overlay.png`, `Candado.png`, `Roca_Mortal.png` y `Splat.png` (antigua mancha, ya no se usa).
- `Effects/`: `Stain_01.png`, `Stain_02.png`, `Stain_03.png` y `Stain_04.png` (manchas de sangre).
- `Environment/Tiles/`: `Tile_Metal.asset` y `Tile_Platform.asset`, que son los Tile del Tilemap.
- `Player/Novato.png`.
- `Traps/Spikes.png` y `Traps/InvisibleBlock_Revealed.png`.
- **Tile Palette:** `Environment/Tiles/Palette_Nivel.prefab` (añadida el 2026-10-27). Contiene `Tile_Metal` y `Tile_Platform` y tiene celdas de 0,5, igual que el Grid del nivel. Para usarla: Window > 2D > Tile Palette, elegir `Palette_Nivel` y pintar sobre `Level/Grid/Solid` o `Level/Grid/Platforms`.

### Carpetas vacías (reservadas)

`Animations/{Player,Traps,UI}`, `Audio/{Music,SFX}`, `Data/Levels`, `Data/Traps`, `Scripts/Save` y `Sprites/UI`.

### Fuera de la estructura o en la raíz de `Assets`

No hay nada: todo está dentro de las carpetas acordadas. No se ha movido ningún archivo.

Como residuo, `Assembly-CSharp-Editor.csproj` aún menciona `Assets/Editor/HubForceResolve.cs`, que ya no existe. Ese archivo lo genera Unity, no afecta a nada y se regenera solo.

### Objetos que solo existen en la escena `Level_01`

| Objeto | Qué es | Recomendación |
|---|---|---|
| `Level/Grid` (Tilemaps `Solid` y `Platforms`) | Geometría del nivel. | Se queda en la escena porque es propia de cada nivel. Se edita con `Palette_Nivel`. |
| `Global Light 2D` y `Level/Lights/*` (10 Light2D) | Iluminación del nivel. | Dejarlo en la escena. Opcional: hacer prefabs de "luz de peligro roja" y "luz de checkpoint" si se repiten en los niveles 2 y 3. |
| `LevelStart` | Punto de inicio. | Se queda en cada escena. |
| `PostFX_Volume` | Volume con el perfil `Settings/Level01_PostFX.asset`. | Dejarlo en la escena (el perfil ya es un asset). |
| `Main Camera` (`CameraFollow`) | Cámara con límites. | Opcional: hacer un prefab. Los límites cambian por nivel. |
| `Level/Background` | Fondo. | Dejarlo en la escena. |

**Convertidos en prefab el 2026-10-27** (ya no son solo de escena):
- `Sistema` → instancia de `Prefabs/Narrator/Sistema`.
- `NarratorTriggers/T0…T10` → 11 instancias de `Prefabs/Narrator/NarratorTrigger`, cada una con su frase como override. T9 lleva además `repeat` activo y T10 tiene un tamaño de 2 × 1,5.
- `Level/Deco/ManchaAntigua_*` (×16) → instancias de `Prefabs/Effects/OldBloodStain` (2026-11-03).
- `LevelSystems` → instancia de `Prefabs/Level/LevelSystems`. Contiene `RespawnManager`, `RunStats` y `BloodStains`, y conserva sus referencias a `Player` y `LevelStart` como override.

**Cambio de jerarquía en `Level_01`:**
- `NarratorTriggers` sale de `Sistema` y pasa a la raíz de la escena, para que el prefab `Sistema` no arrastre las frases del Nivel 1.
- `RespawnManager`, `RunStats` y `BloodStains` cuelgan ahora de `LevelSystems`.

El comportamiento no cambia. El `HUD` sigue apuntando al mismo `RunStats`.

**Verificación (2026-10-27):**
- Las 11 frases de los triggers, la de `completedLine` y las 7 burlas son idénticas a las de antes. Las posiciones y tamaños también.
- No hay referencias rotas en `Level_01` ni en `Sandbox_Player`.
- La consola no muestra errores.

**Pasada automática en Play Mode** (teletransportando al jugador):
- Habla T0.
- Morir en los pinchos: reaparece en el inicio (2; 0,51), cuenta la muerte y salta la burla de la 1.ª muerte.
- La llave se coge y sale la frase de T10.
- Morir sin checkpoint: la llave vuelve a (84,5; 5,9).
- Coger la llave, tocar el checkpoint y morir: reaparece en (101,3; 3,51) con la llave.
- El portal sin llave frena al jugador en x = 129. Con llave, el nivel se completa, sale la despedida y la llave se consume.
- La burla de la 3.ª muerte salta bien.

**No probado:** la partida con teclado.

En `Sandbox_Player`, la geometría `Level_Greybox/*` (suelos, muros y techo) y `AlturasReferencia/*` son objetos de prueba sin prefab. No hace falta convertirlos.

---

## Avisos del GDD (para el equipo)

| # | Contradicción / vacío | Dónde | Propuesta provisional |
|---|---|---|---|
| A1 | Doble salto con dos alturas, frente a "salto con coyote time" sin doble salto. | Mecánicas / Personajes vs Alcance | Se implementó el doble salto (lo confirman Mecánicas y Personajes). El coyote time queda pendiente de que el equipo confirme si se quiere. |
| A2 | Reaparición en menos de 1 s, frente a menos de 0,2 s. | Mecánicas 04 vs Alcance | Usar ~0,15 s, que cumple ambas. |
| A3 | "No incluye ítems recolectables", pero existe la llave de nivel (y el "Parche de Estabilidad"). | Alcance vs Mecánicas 06 / Objetos | Tratar la llave como objeto de progresión, no como recolectable. Dejar el Parche fuera del MVP. |
| A4 | El Nivel 1 termina en un "portal de datos"; la Mecánica 06 usa llave + puerta en todos los niveles. | Niveles vs Mecánicas 06 | Llave + puerta. |
| A5 | El Nivel 3 y el diálogo final del Sistema aún hablan de "Quedarse/Salir", pero el Registro de Decisiones fijó un final único sin elección. | Niveles, Enemigos y NPC | Seguir el Registro de Decisiones (final único). |
| A6 | La tabla de Controles está vacía. | Controles | Usar lo que dicen las mecánicas: A/D y flechas para moverse, Espacio para saltar. |

---

## Próximos pasos (orden propuesto originalmente, según el MVP de Alcance; ver "Estado general")

1. ~~Mecánica 02: salto y doble salto~~ (hecha, con la corrección de altura aplicada; queda la prueba manual con teclado).
2. Muerte (pinchos y vacío) + Mecánica 04 Reaparición instantánea + Mecánica 05 Checkpoints.
3. Trampas troll: Mecánica 03 Bloques invisibles y Mecánica 07 Plataformas que caen.
4. Mecánica 06 Llave + puerta.
5. Contador de muertes, cronómetro y burlas de Sistema.exe.
6. Nivel 1 en greybox.

---

## Historial

| Fecha | Avance |
|---|---|
| 2026-10-26 | Conexión unity-mcp verificada. Mecánica 01 implementada en `Sandbox_Player`; prueba manual con teclado pendiente. |
| 2026-10-26 | Mecánica 02 implementada (PlayerGroundCheck + PlayerJump, prefab con gravityScale 3, alturas de referencia y techo de prueba). Simulación: salto leve y doble salto OK con alturas ~5 % bajas; salto máximo y techo sin probar. |
| 2026-10-26 | Corrección +g·Δt/2 en `PlayerJump.SpeedForHeight`. Alturas medidas: 1,999 / 4,000 / 5,994–5,998 / 7,995. Corte con Techo_Prueba OK. |
| 2026-10-26 | Mecánica 03 implementada: `InvisibleBlock` + prefab, tag Player, dos bloques en Sandbox_Player (bordes a 3,5 y 5,5). Todas las pruebas simuladas OK. |
| 2026-10-26 | Mecánicas 04 y 05 implementadas: PlayerDeath, Checkpoint, RespawnManager, LethalHazard, prefabs (DeathParticles, Spikes, KillZone, Checkpoint), hueco y peligros en Sandbox_Player. Pruebas OK; riesgo R1 pendiente. |
| 2026-10-26 | R1 resuelto (OnDisable en PlayerMovement y PlayerJump). Mecánica 07 implementada (FallingPlatform + prefab, dos instancias en Sandbox_Player). Probados pisar, caer y reiniciar; contacto lateral y R1 en juego pendientes. |
| 2026-10-26 | Mecánica 07: probados el contacto lateral y la versión sin temblor. Mecánica 06 implementada (LevelKey, GoalDoor, prefabs, llave y puerta en Sandbox_Player), todas las pruebas automáticas OK. R1 pendiente de prueba manual. |
| 2026-10-26 | Mecánicas secundarias implementadas: RunStats, StatsHUD (TMP), BloodStainSpawner, prefabs HUD y BloodStain. Pruebas automáticas OK. |
| 2026-10-27 | Salto máximo a 3,2 (decisión de Jesús). Nivel 1 "El Despertar" construido (arte procedural, Tilemap, luces 2D, posprocesado, Sistema con frases, 4 trolls), validado por simulación y con pasadas automáticas. |
| 2026-10-27 | Llave en el Nivel 1 antes del checkpoint (decisión de Jesús); el portal la requiere. Pruebas automáticas OK. |
| 2026-10-27 | GoalDoor: luz del portal ligada a la llave (`unlockedOnlyEffect`) y despedida del Sistema al completar (`completedLine`). Pruebas OK. |
| 2026-11-04 | Rediseño troll de `Level_01` (x 0–331) solo con lo existente, sin scripts nuevos. Checkpoint falso tras la salida falsa. Copia en `Archive/Level_01_v2.unity`. Pruebas automáticas OK; falta teclado. |
| 2026-11-04 | Muro x 96–100 con atajo troll por pared falsa y ruta de arriba más difícil (decisión de Jesús). Pruebas automáticas OK; falta teclado. |
| 2026-11-04 | Pisos disfrazados que tapan los fosos, caída a 0,2 s, trampas en tramos vacíos y arte nuevo con parallax, partículas y luces (decisión de Jesús). Pruebas automáticas OK; falta teclado físico. |
| 2026-11-04 | Coyote time de 0,1 s (decisión de Jesús) y corrección del doble salto, que se gastaba casi sin efecto al subir. Columna en el hueco de x 131 y escalón movido a x 90–91. Pruebas con teclado simulado OK; falta teclado físico. |
| 2026-11-06 | Corrección de `Level_01`: sin checkpoint medio, falso en x 183,5, real invertido en x 289, pinchos que caen en las islas y bloques x 419–421 más altos (decisión de Jesús). Recorrido por script OK, consola limpia; falta teclado físico. Copia en `Archive/Level_01_v8.unity`. |
| 2026-11-08 | Pinchos de las islas de y 10 a y 7 y bloques S5 de y 3,4 a y 1,9 (decisión de Jesús). Pruebas con teclado simulado y recorrido por script OK, consola limpia. Copias `v9`, `v9b_antes` y `v10`. |
| 2026-11-08 | Pinchos de las islas con gravedad 5 (decisión de Jesús). Recorrido por script OK, consola limpia. Copias `v10b_antes` y `v11`. Cuadros S5: "choca y vuelve" no se consigue sin cambiar la mecánica; pendiente de Jesús. |
| 2026-11-08 | Cuadros S5 a y 1,4 (opción C de Jesús). Recorrido por script OK, consola limpia. Copia `v12`. |
| 2026-11-08 | Pared x 421–423 hasta y 5 y cuadros S5 sobre el pilar como escalón (opción 2 de Jesús). Recorrido por script OK, consola limpia. Copias `v12b_antes` y `v13`. |
| 2026-11-08 | Sección final nueva (propuesta de Jesús): salto de fe, 5 pilares (3 fijos y 2 que caen, uno con pinchos) y puerta arriba en x 502,5. Nivel de 508 u. Recorrido por script OK, consola limpia. Copias `v13b_antes` y `v14`. |
| 2026-11-08 | Sección final rehecha con 8 bloques sueltos que suben hasta la puerta (corrección de Jesús). Nivel de 525 u. Recorrido por script OK, consola limpia. Copias `v14b_antes` y `v15`. |

---

## Estado general (2026-10-26)

### Mecánicas del GDD implementadas (en `Sandbox_Player`)

| Mecánica | Estado |
|---|---|
| 01 Movimiento lateral | Hecha. Probada por simulación. |
| 02 Salto y doble salto (2/4 bloques, más la corrección +g·Δt/2) | Hecha. Probada por simulación. |
| 03 Bloques invisibles | Hecha. Probada. |
| 04 Reaparición instantánea | Hecha. Probada. |
| 05 Puntos de control | Hecha. Probada. |
| 06 Llave de nivel y puerta | Hecha. Probada (falta empujar la puerta cerrada). |
| 07 Plataformas que caen | Hecha. Probada. |
| Secundarias: cronómetro, contador dinámico y manchas | Hechas. Probadas. |

**Aún no hay nada del Nivel 1 real.** Todo vive en la escena de pruebas `Sandbox_Player`. Tampoco hay menús, pausa, guardado, Sistema.exe (burlas), audio ni arte.

### Pendiente de probar con teclado (Jesús)

1. **R1:** morir con una dirección pulsada y soltarla estando muerto; al reaparecer, el personaje no debe moverse. Morir al inicio de un salto con Espacio mantenido no debe hacerle saltar al reaparecer.
2. **Puerta cerrada:** caminar contra ella sin la llave; no debe poder atravesarse.
3. **Sensación del umbral de 0,12 s** entre salto leve y salto máximo.
4. **Sensación de la velocidad de 8 bloques/s.**
5. **Alturas reales con teclado:** líneas de referencia Ref_2, Ref_4, Ref_6 y Ref_8.

### Contradicciones del GDD por resolver (equipo)

- **A1:** doble salto (Mecánicas, Personajes) frente a coyote time (Alcance). Se implementó el doble salto; el coyote time queda pendiente.
- **A2:** reaparición en menos de 1 s (Mecánicas) frente a menos de 0,2 s (Alcance). Ahora está en 0,5 s, editable.
- **A3:** Alcance excluye los "ítems recolectables", pero existen la llave y el "Parche de Estabilidad".
- **A4:** el Nivel 1 termina en un "portal de datos"; la Mecánica 06 usa llave y puerta.
- **A5:** el Nivel 3 y el diálogo final siguen con la elección "Quedarse/Salir", pero el Registro de Decisiones fijó un final único.
- **A6:** la tabla de Controles del GDD está vacía.
- **Otras:**
  - El Nivel 2 introduce un "Dash" que no está en Mecánicas.
  - El Nivel 1 menciona "Bugs lentos" (enemigos) y un coleccionable oculto, sin especificar.
  - El flujo entre niveles (qué pasa al completar) no está definido.
