# Changelog

## 1.0.4 — first public release

Versions 1.0.0 to 1.0.3 were private test builds.

- One random player becomes a real game monster each level; the level has no AI enemies.
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
