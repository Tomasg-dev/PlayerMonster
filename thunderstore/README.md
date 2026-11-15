# PlayerMonster

**One of you is not coming back as a robot.**

At the start of each level, one random player's circuits fail and they turn into a **real monster from the game** — same model, animations, sounds and attacks as the original enemy — spawned on the far side of the map. The rest of the team still has to meet the quota. The monster has to stop them.

This is the first public release. It works in multiplayer, but expect rough edges. Feedback is very welcome.

> Español más abajo.

## How it plays

- **The level has no AI enemies.** The only monster is a player.
- **Your first form is random.** After that, every time you respawn you pick your next form from a side panel (left / right arrows). Stronger monsters only unlock on later levels, and each one is locked for a while after you die with it.
- **Playable monsters (6):** Animal, Reaper, Chef, Bowtie, Huntsman and Tick. More are planned.
- **The team wins** by extracting as usual. **The monster wins** if the whole team dies: everyone sees "THE MONSTER WINS" and who it was, once the arena has loaded.
- **Killing the monster** gives the team a breather (and an orb, the first times). It respawns a few seconds later, somewhere far away, as a different monster and slightly tougher.

## Who is the monster?

By default a new player is drawn every level, so everyone takes turns. You can change that with the `RoundsPerMonster` option:

- `1` (default): a different player each level.
- `3`, `5`...: the same player stays the monster for that many levels in a row.
- `0`: **infinite — the same player is the monster for the whole run.** Use this if you prefer fixed roles instead of switching between robot and monster.

## Keeping it fair

The monster is strong, so it comes with rules:

| Rule | What it does |
|---|---|
| **Rage bar** | Limits how long the monster can keep attacking. Empty it and you are exhausted for a moment. |
| **Presence bar** | Fills while the monster stays in the same room as the players. When full, the monster is forced to retreat on its own. No camping the team. |
| **Hit pause** | After landing a hit, continuous attackers cannot attack again for a moment. |
| **Senses** | Every monster sees the world badly in its own way: the Huntsman is nearly blind and cannot see players at all, only rings where something makes noise; the Reaper has tunnel vision; the Chef gets dizzy after a leap... |
| **No loot griefing** | The monster cannot break valuables. |
| **Health scaling** | Monster health starts below the original and grows with player count and level. |
| **Heartbeat** | Humans hear a heartbeat (and see a red edge up close) when the monster is near, through walls. You know it is coming, not from where. |
| **Truck rule** | The monster cannot hurt you inside the truck, unless you camp there for more than 20 seconds. |
| **Scent** | If nobody has been near the monster for a minute, it briefly sees a marker towards the closest player. Hiding forever does not work. |

The monster's voice chat comes out of the monster itself, distorted.

## Controls (as the monster)

| Key | Action |
|---|---|
| WASD / Sprint | Move / move faster |
| Grab (left click) | Attack. Huntsman: hold to aim with the crosshair, release to fire |
| V | First / third person |
| Left / Right arrows | Choose your next monster while respawning |

## Who needs the mod

- **The host: always.** The host runs the mode.
- **Everyone else: strongly recommended, same version.** Only players with the same version can be picked as the monster. Players without it always stay human: they see the monster player's robot standing still, and get no heartbeat and no truck protection.

## Installation

Use a mod manager (Gale, r2modman or Thunderstore Mod Manager) and install it like any other mod; BepInEx and REPOLib are installed automatically as dependencies.

Manual: install BepInExPack and REPOLib, then copy `PlayerMonster.dll` into `BepInEx/plugins/`.

## Configuration

Everything is configurable in `BepInEx/config/tommy777.playermonster.cfg` or in-game with REPOConfig. **The host's values decide the rules.** Useful ones:

- `General / Language`: on-screen text language (`Auto`, `Spanish`, `English`).
- `Mode / RoundsPerMonster`: how many levels in a row the same player is the monster (`0` = whole run).
- `Mode / Chance`: chance of a player-monster on each level (default 100).
- `Mode / OnlyPlayerMonster`: turn off to keep the game's own enemies as well.
- `Balance / ...`: health, rage, presence, senses, respawn and lock times.
- `Humans / ...`: heartbeat distances and volume, truck rule. These are each player's own settings.
- `Camera / ...`: first / third person and camera distance.

## Updating from a test build

If you tried an earlier test build called **JugadorMonstruo**, remove it. It is the same mod under its old name, and with both installed you get two monsters and a broken HUD. PlayerMonster disables itself and shows a warning if it finds the old one.

## Known issues

- Tested on R.E.P.O. **v0.5.1** only. Game updates may break it.
- Not tested with other mods that add or change enemies.
- The monster moves on the host, so a monster player with high ping will feel input delay.

Please report bugs with your `BepInEx/LogOutput.log` (the host's log is the most useful).

## Credits

Created by **Tommy777**, co-authored by **Dragonico**. A large part of the code was written with AI assistance.
Built on BepInEx, HarmonyX and REPOLib. It does not include any game code or assets. Source code under the MIT license.

---

# Español

Al empezar cada nivel, a un jugador elegido por sorteo le fallan los circuitos y se convierte en un **monstruo real del juego** (mismo modelo, animaciones, sonidos y ataques), que aparece en la otra punta del mapa. El resto del equipo tiene que cumplir la cuota; el monstruo, impedirlo.

- **El nivel no tiene enemigos propios:** el único monstruo es un jugador.
- **La primera forma sale al azar.** A partir de ahí, cada vez que reapareces eliges la siguiente en un panel lateral con las flechas izquierda / derecha.
- **Monstruos jugables (6):** Animal, Reaper, Chef, Bowtie, Huntsman y la garrapata.
- **Gana el equipo** si extrae; **gana el monstruo** si muere todo el equipo: sale "EL MONSTRUO GANA" con el nombre de quien era, cuando termina de cargar la arena.
- **Si matan al monstruo**, reaparece a los pocos segundos, lejos, como otro monstruo distinto y un poco más fuerte.

**¿Quién es el monstruo?** Por defecto se sortea un jugador nuevo en cada nivel. Con la opción `RoundsPerMonster` puedes hacer que el mismo jugador repita varios niveles, o poner `0` para rondas infinitas: el mismo jugador es el monstruo toda la partida, sin alternar entre robot y monstruo.

**Para que sea justo:** barra de furia (limita el ataque), barra de presencia (si se queda con los jugadores, se retira solo), pausa tras cada golpe, sentidos limitados por monstruo, no puede romper el botín, vida escalada por jugadores y nivel, latido de aviso para los humanos, protección en el camión (salvo que acampes más de 20 s) y olfato para el monstruo si lleva un minuto sin encontrar a nadie.

**Controles del monstruo:** WASD y sprint para moverse, clic de agarrar para atacar (Huntsman: mantener para apuntar con la mira, soltar para disparar), V para primera o tercera persona.

**Quién necesita el mod:** el anfitrión siempre; los demás, muy recomendado y en la misma versión. Quien no lo tenga juega siempre como humano, ve el robot del monstruo quieto y no tiene latido ni protección del camión.

Probado solo en la v0.5.1 del juego. Si encuentras un fallo, adjunta el `BepInEx/LogOutput.log` del anfitrión.

Creado por **Tommy777**, con **Dragonico** como coautor.
