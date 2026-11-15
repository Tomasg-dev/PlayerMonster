# Mapa de enemigos de R.E.P.O. (v0.5.1, Unity 6000.3.17f1)

Sacado de leer `Assembly-CSharp.dll` descompilado (carpeta `_decompilado/`, solo estudio, no se redistribuye).
Los nombres son los internos del código; el nombre visible (`EnemyParent.enemyName`) vive en los prefabs, no en el código.

## 1. Cómo se define un enemigo

Un enemigo es un prefab con esta jerarquía de componentes:

| Pieza | Clase | Qué hace |
|---|---|---|
| Raíz | `EnemyParent` | Nombre, dificultad (1-3), ciclo spawn/despawn con temporizadores, suelta el orbe al morir. Tiene `PhotonView`. |
| Núcleo | `Enemy` | Estado genérico (`EnemyState`), objetivo (`TargetPlayerAvatar`), y referencias a los módulos opcionales (`HasVision`, `HasNavMeshAgent`, `HasRigidbody`, `HasHealth`, `HasJump`...). Tiene `PhotonView`. |
| Movimiento lógico | `EnemyNavMeshAgent` | Envoltorio del `NavMeshAgent` de Unity: `SetDestination`, `OverrideAgent(vel, acel, t)`, `Warp`, `Stop`, `ResetPath`. **Solo está activo en el host.** |
| Cuerpo físico | `EnemyRigidbody` | Rigidbody que persigue a `followTarget` (el transform que mueve el NavMeshAgent). Es agarrable (`PhysGrabObject`). |
| Sentidos | `EnemyVision`, `EnemyStateInvestigate`, `EnemyPlayerDistance`, `EnemyOnScreen` | Lanzan `UnityEvent` (`onVisionTriggered`, investigar ruido...). |
| Vida | `EnemyHealth` | `Hurt(daño, dir)`, eventos `onHurt` / `onDeath`, `health` configurable. |
| **Cerebro** | `Enemy<Nombre>` (p. ej. `EnemyRunner`) | Máquina de estados propia: `enum State`, `Update()` con `switch(currentState)` que llama a `StateIdle()`, `StateRoam()`, `StateAttack...()`. |
| Presentación | `Enemy<Nombre>Anim` | Lee `currentState` y mueve Animator, sonidos y partículas. Corre en todos los clientes. |
| Daño | `HurtCollider` | Collider de daño que el cerebro o la animación activa durante el ataque. |

`EnemySetup` (ScriptableObject) agrupa los prefabs a instanciar (`spawnObjects`) y su rareza.

## 2. Cómo se spawnean

1. `EnemyDirector.AmountSetup()` decide cuántos por dificultad (curvas `amountCurve*`) y `GetEnemy()` elige de `enemiesDifficulty1/2/3`.
2. `LevelGenerator.EnemySetup()` (corrutina, solo host) llama a `EnemySpawnObject`, que usa `PhotonNetwork.InstantiateRoomObject(resourcePath, ...)` (o `Instantiate` en un jugador).
3. `EnemyParent.Setup()` lo registra en `EnemyDirector.instance.enemiesSpawned` y arranca `Logic()`: alterna entre spawneado/despawneado con temporizadores (`SpawnRPC` / `DespawnRPC`). "Despawn" no destruye el objeto, solo lo desactiva.

Para spawnear a mitad de partida: `LevelGenerator.Instance.EnemySpawn(EnemySetup, posición)` es público. REPOLib además expone `Enemies.SpawnEnemy`, `TryGetEnemyByName`, etc. (útil para Palabra Prohibida).

## 3. Cómo se controla la IA

Patrón que se repite en todos los cerebros:

```
Update():
    (efectos locales para todos)
    if (singleplayer || PhotonNetwork.IsMasterClient):
        switch (currentState): StateX() ...

StateX():
    if (stateImpulse) { inicialización del estado }
    enemy.NavMeshAgent.SetDestination(...)      <- EJECUCIÓN
    if (condición) UpdateState(State.Y)         <- DECISIÓN
```

- Decisión y ejecución están **mezcladas dentro de cada `StateX()`**; no hay una capa "decisor" separable de forma genérica.
- Las percepciones entran por callbacks públicos: `OnVision()`, `OnInvestigate()`, `OnGrabbed()`, `OnHurt()`, `OnDeath()`, `OnSpawn()`.
- `UpdateState(next)` solo actúa en el host y replica con `UpdateStateRPC`.

Consecuencia para "poseer": hay que hacerlo **por enemigo**, parcheando su `Update` (prefijo de Harmony que, si está poseído, sustituye el `switch` por: mover según input del jugador y disparar el estado de ataque original al pulsar un botón). Los estados de ataque, animaciones y sonidos se reutilizan tal cual.

## 4. Cómo se sincroniza por red

Confirmado: **Photon PUN 2** (`PhotonUnityNetworking.dll`) + **Photon Voice** para el chat de voz.

- **Autoridad total del host.** El `NavMeshAgent` solo existe activo en el host; los clientes interpolan.
- `Enemy.OnPhotonSerializeView` envía: posición, `CurrentState`, `TargetPlayerViewID`, `Stunned`. La rotación/cuerpo van por `PhotonTransformView`.
- Cada cerebro replica su `currentState` por RPC, y el `...Anim` de cada cliente lo reproduce.
- **Todos los RPC de enemigos pasan por `SemiFunc.MasterOnlyRPC(info)`**: si el remitente no es el host, se ignoran. Un cliente no puede mandar a un enemigo con los RPC del juego.
- **El daño al jugador lo decide el cliente de la víctima**: `HurtCollider.PlayerHurt` sale si `!player.photonView.IsMine`. Es decir, si el estado/animación de ataque llega sincronizado, el golpe funciona solo en cada cliente.

Consecuencia para el cliente-monstruo: necesita un canal propio cliente -> host (evento de Photon vía `REPOLib` `NetworkedEvent`) con sus inputs (dirección, mirada, botón de ataque). El host los aplica al enemigo; el resto de la sincronización ya la hace el juego. El coste es la latencia del input (ida al host + vuelta de posición).

## 5. Enemigos por complejidad (líneas del cerebro / nº de RPC)

| Cerebro | Líneas | RPC | Ataque | Nota |
|---|---|---|---|---|
| `EnemyAnimal` | 450 | 2 | Corre por la sala empujando | El más simple; ideal como primera prueba |
| `EnemyBowtie` | 568 | 2 | Grito que empuja (`Yell`) | Un solo ataque, usa HurtCollider |
| `EnemyUpscream` | 656 | 3 | Agarra y lanza | Ataque dirigido a un jugador |
| `EnemyTumbler` | 784 | 4 | Salto rodando (`Tell` -> `Tumble`) | HurtCollider, ataque con "aviso" |
| `EnemyRunner` | 894 | 2 | Cuerpo a cuerpo persiguiendo | HurtCollider; el arquetipo de "asesino" |
| `EnemyRobe` | 906 | 6 | Cuerpo a cuerpo + mirar bajo muebles | Algo más de estados |
| `EnemyHunter` | 1036 | 4 | Apuntar y disparar | Ataque a distancia, ciego |
| `EnemyBeamer` | 852 | 4 | Rayo + melee | Dos ataques |
| Evitar al principio | | | | `EnemySnatcher` (16122 líneas, 36 RPC), `EnemyOogly`, `EnemyHeartHugger`, `EnemyShadow`, `EnemyBirthdayBoy`, `EnemyTricycle`, `EnemyElsa`, y los de enjambre (`EnemyGnome`, `EnemyBang`, que dependen de un `...Director`) |

## 6. Voz (para Palabra Prohibida)

- `PlayerVoiceChat` usa el `Recorder` de Photon Voice con `MicType.Unity`, o sea `UnityEngine.Microphone` sobre el dispositivo elegido en el menú.
- Ya calcula `clipLoudness` leyendo muestras del micrófono (línea ~1172-1293). Ese mismo búfer es el punto natural para alimentar a Vosk sin abrir el micrófono por segunda vez. Falta comprobar frecuencia de muestreo y si el clip es accesible.
