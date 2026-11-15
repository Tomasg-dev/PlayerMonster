# Arquitectura de PlayerMonster

Cómo encajan las piezas del mod. Para entender cómo están hechos los enemigos del juego, lee antes [MAPA_ENEMIGOS.md](MAPA_ENEMIGOS.md).

## La idea central

En R.E.P.O. los enemigos los mueve el anfitrión: su navegación solo existe en su máquina y los clientes reciben la posición y el estado. Además, todos los mensajes de red de los enemigos se descartan si no vienen del anfitrión.

Por eso el mod separa dos papeles, que pueden estar o no en la misma máquina:

| Papel | Quién | Archivo | Qué hace |
|---|---|---|---|
| **Autoridad** | El anfitrión | `Autoridad.cs` | Crea al enemigo, le aplica los controles del piloto, decide vida, furia, presencia, muerte y reaparición |
| **Piloto** | El jugador-monstruo | `Piloto.cs` | Lee teclado y ratón, mueve su cámara, dibuja su interfaz y sus efectos de pantalla |

Si el monstruo es el anfitrión, los dos papeles hablan entre sí con llamadas normales. Si es un cliente, hablan por red (`Mensajes.cs`). El resto del código no distingue un caso del otro.

## Flujo de un frame

```
Piloto (cliente del monstruo)                 Autoridad (anfitrión)
-----------------------------                 ---------------------
EntradaMonstruo.LeerLocal()
  dirección, correr, atacar,
  mirada y punto de mira
        │  Mensajes.EnviarEntrada (25 veces/s, sin garantía de entrega)
        └────────────────────────────────────▶ Autoridad.RecibirEntrada
                                               Furia.Filtrar(entrada)
                                               Parches/<Enemigo>Patch usa la entrada
                                               en lugar de las decisiones de la IA
                                               Presencia.Tick, ProteccionObjetos.Tick
        ┌──────────────────────────────────── Mensajes.EnviarEstado (10 veces/s)
        ▼                                       fase, tipo, vida, furia, presencia...
Piloto.RecibirEstado
Camara, Hud, Sentidos, Seleccion, Olfato, Mira
```

La posición, las animaciones, los sonidos y el daño del enemigo los sincroniza el propio juego, como con cualquier enemigo.

## Los parches de enemigos (`Parches/`)

La IA de cada enemigo es un `Update()` que, en el anfitrión, ejecuta un `switch` de estados. Cada estado mezcla decidir (¿persigo?, ¿ataco?) con ejecutar (moverse, animar, golpear).

Cada parche es un prefijo de Harmony sobre ese `Update()`. Si el enemigo lo controla un piloto (`Autoridad.Controla`), el prefijo sustituye al original y:

- en los estados **libres** (quieto, andando) mueve al enemigo según la entrada del piloto;
- en los estados **bloqueados** (aviso, ataque, recuperación, aturdido) llama al método de estado **original**, para que duren y se vean igual que en el juego.

Los parches también anulan las percepciones (`OnVision`, `OnInvestigate`...) con `Autoridad.EstaPoseido`, para que el enemigo no cambie de estado por lo que ve u oye.

Hay tres plantillas, según el tipo de ataque:

| Plantilla | Ataque | Enemigos |
|---|---|---|
| `RunnerPatch` | Mantener el botón | Animal, Reaper |
| `TumblerPatch` | Secuencia con aviso y recuperación | Chef, Bowtie, Huntsman |
| `TickPatch` | Necesita una víctima concreta | Garrapata |

## Mecánicas, un archivo cada una

| Archivo | Dónde corre | Qué hace |
|---|---|---|
| `ModoJuego.cs` | Anfitrión | Decide si hay monstruo en el nivel y arranca la transformación |
| `Rotacion.cs` | Anfitrión | Sortea al piloto entre quienes tienen la misma versión del mod |
| `Furia.cs` | Anfitrión | Barra que limita el ataque; pausa tras acertar |
| `Presencia.cs` | Anfitrión | Barra que obliga a retirarse; durante la retirada corre la IA original |
| `ProteccionObjetos.cs` | Anfitrión | El monstruo no rompe objetos de valor |
| `Seleccion.cs` | Ambos | Qué monstruos se pueden elegir (anfitrión) y el panel (piloto) |
| `Sentidos.cs` | Piloto | Cómo ve el mundo cada monstruo |
| `Olfato.cs` | Piloto | Marca hacia el jugador más cercano tras mucho sin contacto |
| `Mira.cs` | Piloto | Mira del cazador |
| `Camara.cs`, `Hud.cs`, `Aviso.cs`, `VistaModelo.cs` | Piloto | Cámara e interfaz |
| `CuerpoJugador.cs` | Piloto | Oculta el robot del jugador-monstruo |
| `Cercania.cs` | Cada humano | Latido de aviso |
| `Camion.cs` | Cada humano | Protección dentro del camión; avisa al anfitrión de los golpes recibidos |
| `RedMonstruo.cs` | Todos | Publica quién es el monstruo y qué enemigo lleva; voz del monstruo |

## Red

Hay dos canales, los dos de Photon:

- **Mensajes** (`Mensajes.cs`, eventos de REPOLib): controles del piloto al anfitrión, estado del anfitrión al piloto, y avisos puntuales (elección de monstruo, mareo, ruidos, golpes). El anfitrión comprueba quién envía cada mensaje y valida su contenido.
- **Propiedades de jugador** (`RedMonstruo.cs`, `Rotacion.cs`): datos que deben conocer todos, como quién es el monstruo, qué enemigo controla y qué versión del mod tiene cada uno. Photon las reparte solo.

Reglas que conviene respetar al tocar la red:

- No fiarse de lo que llega: comprobar remitente, tipos y que los números sean finitos.
- Si cambia el formato de un mensaje, subir la versión del mod. Solo se sortea como monstruo a quien tiene exactamente la versión del anfitrión, así que versiones distintas no llegan a hablarse.

## Convenciones

- Código y comentarios en español. Los textos que ve el jugador van con `Idioma.T("español", "english")`.
- El mod usa las piezas del juego siempre que existen (retirada, resistencia al daño, efectos de pantalla, voz) en vez de reimplementarlas.
- No se copia código del juego ni de otros mods. El código descompilado del juego sirve para estudiar y no se sube al repositorio.
- Se accede a miembros privados del juego con el "publicizer" configurado en el `.csproj`.

## Origen del código

Las versiones hasta la 0.5.0 se desarrollaron en dos ramas separadas. La base actual se reconstruyó a partir de la DLL compilada de la 0.5.0, por lo que los archivos que vienen de ahí conservan su lógica, pero no los comentarios ni algunos nombres de variables originales (aparecen nombres genéricos como `num` o `flag`). Se pueden ir limpiando poco a poco.
