# BalsamDauer (balm duration)

On server start, sets the effect duration of the **Golden Star Balm** (Goldstern-Balsam) to a configurable value (default: 15 min).

By default the balm lasts 350 s against pain and 400 s against radiation. Both are raised to the configured duration.

**Why a separate mod:** SVM has no setting for the effect durations of individual items. And editing the database directly no longer works since SPT 4.1, because it is protected by checksums. Mods may change the same table at runtime.

**Deliberately left alone:** the three stimulator buffs (+1 energy/hydration per second, 5 s each). Stretched to 15 min, that would be +900 from a single balm.

## How this mod was made (AI disclosure)

This mod was **programmed by Claude**, an AI assistant made by Anthropic.
**Kaan was the tester**: Kaan has no programming background, played with the mod
on SPT 4.1.6, reported what went wrong, and Claude fixed it. No human programmer
has reviewed the code. It is open source so anyone can check it — if you find a
bug, please open an [issue](https://github.com/Kaanori/spt-balsamdauer/issues).

## Requirements

- SPT 4.1.6 (server).

## Installation

1. Download `BalsamDauer-1.0.0.zip` from the [Releases](https://github.com/Kaanori/spt-balsamdauer/releases) page.
2. Extract it **into your SPT game folder** (the folder that contains `EscapeFromTarkov.exe`).
   The ZIP already has the right layout; the mod ends up in `SPT_Runtime/user/mods/BalsamDauer/`.
   On Windows, make sure "Extract All" does not add an extra subfolder.
3. Restart the SPT server.

## Uninstall

Delete the mod's files from `SPT_Runtime/user/mods/BalsamDauer/` (settings: `BepInEx/config/` for client mods) and restart the SPT server.

## Settings: `balsam.json`

Lies next to the DLL. It can be changed without rebuilding and takes effect on the next server start.

## Building from source

Needs the .NET 10 SDK and an SPT 4.1.6 installation to build against.

```bash
./build.sh             # Linux: build only
./build.sh --install   # build and copy into the game folder
```
```powershell
.\build.ps1            # Windows
.\build.ps1 -Install
```

The game folder is set in `Directory.Build.props` (Windows default `C:/SPT`)
or via the environment variable `SPT_DIR`.

## Support

Bugs and questions: [GitHub issues](https://github.com/Kaanori/spt-balsamdauer/issues).

## License

MIT — see [LICENSE](LICENSE).
