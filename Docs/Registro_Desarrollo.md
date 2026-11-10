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
