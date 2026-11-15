# PlayerMonster

A mod for [R.E.P.O.](https://store.steampowered.com/app/3241660/REPO/) that turns the game into an asymmetric match: each level, one random player becomes a **real monster from the game** and hunts the rest of the team, who still have to meet the quota.

- **Players:** what the mod does, how to install it and how to configure it is in the [mod page README](thunderstore/README.md).
- **Developers:** this file explains how the repository is organised and how to build it.

Created by **Tommy777**, co-authored by **Dragonico**. A large part of the code was written with AI assistance.

> The code, its comments and the internal docs are in Spanish. On-screen text is in English and Spanish.

## Repository layout

```
PlayerMonster/
├── src/PlayerMonster/        The mod (C#, BepInEx 5 plugin)
│   ├── Plugin.cs             Entry point: configuration and the per-frame loop
│   ├── Autoridad.cs          Host side: owns the monster, applies the pilot's input
│   ├── Piloto.cs             Monster player's side: reads input, camera, screen effects
│   ├── Mensajes.cs           Network messages between host and pilot
│   ├── Rotacion.cs           Who is the monster on each level
│   ├── Parches/              One Harmony patch per playable enemy
│   └── ...                   One file per mechanic (Furia, Presencia, Sentidos, Camion...)
├── thunderstore/             What gets published: manifest, icon, README, CHANGELOG
├── docs/                     Architecture and notes on how the game's enemies work
├── scripts/package.sh        Builds the mod and creates the Thunderstore zip
└── Directory.Build.props     Paths to your game and BepInEx install
```

See [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md) for how the pieces fit together, and [docs/MAPA_ENEMIGOS.md](docs/MAPA_ENEMIGOS.md) for how the game's enemies are built.

## Building

You need:

- The .NET SDK (8 or newer).
- R.E.P.O. installed, with BepInEx 5 and [REPOLib](https://thunderstore.io/c/repo/p/Zehs/REPOLib/) in a mod manager profile.

The mod compiles against the DLLs of your own install. **This repository contains no game files, and none must be added.**

1. Tell the build where your game and BepInEx folder are. The defaults are Steam on Linux and Gale's `Default` profile. For anything else, create `Directory.Build.props.user` next to `Directory.Build.props` (it is ignored by git); the comment at the top of [Directory.Build.props](Directory.Build.props) has an example.
2. Build:

   ```bash
   dotnet build src/PlayerMonster -c Release
   ```

   This also copies the DLL into `BepInEx/plugins/Tommy777-PlayerMonster` of your profile so you can test straight away. Add `-p:DeployToProfile=false` to skip that.

3. To create the zip for Thunderstore:

   ```bash
   scripts/package.sh
   ```

   The zip is written to `dist/`.

## Releasing a new version

1. Bump the version in **three** places: `Version` in `src/PlayerMonster/Plugin.cs`, `<Version>` in `src/PlayerMonster/PlayerMonster.csproj` and `version_number` in `thunderstore/manifest.json`.
2. Add an entry to `thunderstore/CHANGELOG.md`.
3. Run `scripts/package.sh` and upload the zip.

The version matters at runtime: only players with exactly the host's version can be picked as the monster. Bump it whenever network messages change.

## Adding a monster

Each playable enemy is a Harmony patch in `src/PlayerMonster/Parches/` that replaces the *decision* part of the enemy's AI with the pilot's input and keeps the original code for movement, attacks, animation and networking. The short version:

1. Add a value to `TipoMonstruo` and map it to the enemy's brain class in `Autoridad.Cerebros`.
2. Write `Parches/<Enemy>Patch.cs`, using an existing one with a similar attack as a template (`RunnerPatch` for hold-to-attack, `TumblerPatch` for an attack sequence, `TickPatch` for attacks that need a victim).
3. Give it a rage profile in `Furia.Para`, a resting rule in `Presencia.PonerEnReposo` and, optionally, a sense in `Sentidos.Tick`.

[docs/ARQUITECTURA.md](docs/ARQUITECTURA.md) explains each step.

## Reporting bugs

Open an issue and attach the **host's** `BepInEx/LogOutput.log`.

## Legal

PlayerMonster is an unofficial fan mod. R.E.P.O. is a trademark of semiwork; this project is not affiliated with or endorsed by them. The mod only patches the game at runtime and ships no game code or assets.
