# Changelog

## 1.0.2

- Huntsman: fixed the shot missing the crosshair when aiming at something close.
- Leaving the game as the Huntsman no longer leaves the screen dark in the main menu.
- Presence bar now takes 75 seconds to fill instead of 30 (new option name: `PresenceSeconds`).

## 1.0.1

- Huntsman: the shot now lands exactly on the crosshair (it used to land below it).
- The mod now disables itself and shows a warning if an old `JugadorMonstruo` build is also installed. Running both at once caused two monsters, overlapping HUD text and ignored monster choices.
- Monster choice on respawn is re-sent while the panel is open, and the host logs what was requested and what was picked.
- A monster player who stops receiving updates from the host goes back to being a robot instead of getting stuck with the monster's view.

## 1.0.0 — first public release

- One random player becomes a real game monster each level; the level has no AI enemies.
- Works in multiplayer: any player with the mod (same version as the host) can be the monster.
- Rotation between levels, including fixed roles for the whole run (`RoundsPerMonster = 0`).
- 6 playable monsters: Animal, Reaper, Chef, Bowtie, Huntsman, Tick.
- First form is random; on respawn, a selection panel with 3D preview, level gating and per-monster lockout.
- Respawn as a different, slightly tougher monster.
- Huntsman crosshair: the shot goes exactly where the crosshair points.
- Balance: rage bar, presence bar with forced retreat, hit pause, per-monster senses, health scaling, no breaking valuables.
- For humans: proximity heartbeat, truck protection (with anti-camping), monster voice distorted and coming from the monster.
- Monster "scent" after a minute without contact.
- First / third person camera.
- On-screen text in English and Spanish; configuration in English.
