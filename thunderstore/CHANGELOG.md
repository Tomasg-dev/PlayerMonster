# Changelog

## 1.1.0 — three new monsters

- New playable monsters: **Robe**, **Trudge** and **Clown** (9 in total).
- Robe: walks, runs and uses its original slash.
- Trudge: slow and heavy, with its original mace slam.
- Clown: laser beam aimed with a crosshair (the sweep is the game's own), and its kick when a player is right next to it.
- Each new monster has its own senses and rage timing.
- All players need the same version: 1.0.0 and 1.1.0 do not mix.

## 1.0.0 — first public release

- One random player becomes a real game monster each level, replacing the game's regular enemies.
- Works in multiplayer: any player with the mod (same version as the host) can be the monster.
- Rotation between levels, including fixed roles for the whole run (`RoundsPerMonster = 0`).
- 6 playable monsters: Animal, Reaper, Chef, Bowtie, Huntsman, Tick.
- First form is random; on respawn, a selection panel with 3D preview, level gating and per-monster lockout.
- Respawn as a different, slightly tougher monster.
- Huntsman crosshair: the shot goes exactly where the crosshair points.
- Monster victory: when the whole team dies, everyone sees who the monster was once the arena has loaded.
- Balance: rage bar, presence bar with forced retreat, hit pause, per-monster senses, health scaling, no breaking valuables.
- For humans: proximity heartbeat, truck protection (with anti-camping), monster voice distorted and coming from the monster.
- Monster "scent" after a minute without contact.
- First / third person camera.
- On-screen text in English and Spanish; configuration in English.
- Disables itself with a warning if an old `JugadorMonstruo` test build is also installed.
