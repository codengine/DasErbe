# Das Erbe
<p align="center">
  <img width="642" height="512" alt="grafik" src="https://github.com/user-attachments/assets/43248d5c-3680-4d1c-bb8e-0b1f61906c09" />
</p>

<p align="center">
  <img width="642" height="512" alt="grafik" src="https://github.com/user-attachments/assets/9dff10c4-9428-45d8-aa48-6ece60860a3a" />
</p>


This repository contains an unofficial C#/.NET desktop reimplementation of **Das Erbe**. The runtime reads data and text from the original DOS executable, loads the original `ERBE.DAT`, `ERBE.GAM`, and `GRAFIK` resources, and presents the game through MonoGame DesktopGL.

The repository is organized as follows:

- `src/Game.Core` contains the game runtime and reimplemented game behavior.
- `src/Game.Shared` contains shared resource, rendering, input, executable-image, and logging support.
- `src/Game.Desktop` is the MonoGame desktop host.
- `Game` is the default original-asset root used when running from the repository root.
- `tools/DumpStrings` extracts the original executable's strings to create translation overlays.

## The Port

This is a reverse engineered source port of the original game. It tries to be close to the source, even the savegame format is compatible with the DOS Version.
Small convenience changes have been added which make allows you to play the game slightly "modernized", like hovering changes the status bar text.

## The game

Information about the game can be found here:
- [Mobygames](https://www.mobygames.com/game/12352/das-erbe/)
- [adventurecorner.de](https://www.adventurecorner.de/game/2558/das-erbe)
- [Werbespiel Blog](https://werbespiel.blogspot.com/2010/07/das-erbe.html)

## Added features

- Translation support (`--language`)
- Mouse-over hovering and Click-to-action (hotspots hints will be shown immediately instead of via clicking, and interactions don't need a separate confirmation click, can be turned of with `--use-classic-interactions`)
- Hotspot highlighting (Spacebar)
- DKSN-mode (will show some fallback responses instead of...nothing, `--dksn-mode`)
- Intro music from the amiga version
- You are able to use the ingame keypad using your keyboard/numpad

## Bugfixes
- Fixed soft lock if you went to the city, came back, went upstairs and were not able to leave your room anymore
- Fixed a visual glitch that the brochure would reappear when leaving the laundromat while it has already been taken
- Fixed that saving and loading inside the laundromat caused visual corruption
- Fixed an exception / edge case while entering the laundromat
- Fixed inconsistent image overlay drawing
- Fixed that the "guy" was selectable at the start of the game

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A supported set of the original Das Erbe game assets

At startup the engine requires `ERBE.EXE` in the asset root and validates the supported executable:

- Size: `50096` bytes
- MD5: `6ceee7395fd9965853e9fcfc0b227480`

The remaining game files, including `ERBE.DAT`, `ERBE.GAM`, and the `GRAFIK` directory, must retain their original names and directory layout.

## Build

Run the following from the repository root:

```shell
dotnet build src/Game.sln
```

The translation utility is not part of `Game.sln`; build it separately when needed:

```shell
dotnet build tools/DumpStrings/DumpStrings.csproj
```

Add `-c Release` to either command for a release build.

## Run

From the repository root, run the desktop host with:

```shell
dotnet run --project src/Game.Desktop/Game.Desktop.csproj
```

This uses `Game` below the current working directory as the asset root. Asset roots are resolved in this order:

1. `--asset-root`
2. The `GAME_ASSET_ROOT` environment variable
3. `<current working directory>/Game`

Values supplied through `--asset-root` or `GAME_ASSET_ROOT` must be absolute paths. To pass options through `dotnet run`, place them after `--`:

```shell
dotnet run --project src/Game.Desktop/Game.Desktop.csproj -- --asset-root "C:\Games\Das Erbe" --language en
```

### Run a built game

After a Debug build, run the built game from the repository root with:

```shell
dotnet src/Game.Desktop/bin/Debug/net10.0/Game.Desktop.dll
```

For a Release build, use the corresponding Release output:

```shell
dotnet src/Game.Desktop/bin/Release/net10.0/Game.Desktop.dll
```

On Windows, the generated app host can be started directly instead:

```powershell
.\src\Game.Desktop\bin\Debug\net10.0\Game.Desktop.exe
```

Pass game options directly to a built DLL or executable; the `--` separator is only needed by `dotnet run`:

```shell
dotnet src/Game.Desktop/bin/Debug/net10.0/Game.Desktop.dll --language en
```

Running these commands from the repository root continues to use `Game` as the default asset directory. When starting the built game from another working directory, supply an absolute `--asset-root` path or set `GAME_ASSET_ROOT` to an absolute path.

## Desktop command-line options

Usage: `Game.Desktop [options]`

| Option | Meaning |
| --- | --- |
| `--asset-root <path>` | Use the absolute path as the original-game asset root. This takes precedence over `GAME_ASSET_ROOT` and the default `Game` directory. |
| `--log-level <level>` | Set the minimum log level. Accepted values are `trace`, `debug`, `info`, `warn`, and `error`; the default is `info`. |
| `--log-file <path>` | Write logs to this path. The default is `logs/game.log` below the current working directory. |
| `--no-console-log` | Disable console logging. File logging remains enabled. |
| `--language <name>` | Load `<name>.txt` from the asset root as a translation overlay. Supply the name without `.txt`, for example `--language en`. |
| `--integer-scaling <bool>` | Restrict scaling to whole-number factors when `true` (the default). Use crisp, continuously fitted 4:3 scaling when `false`. |
| `--use-classic-interactions` | Preserve the original double-confirmation interaction flow. Without this option, the host uses the modernized interaction flow. |
| `--dksn-mode` | For scene-hotspot actions that have no implementation, show varied German, command-specific fallback responses. |
| `--mute-log-channels <names>` | Mute a comma-separated list of logging channels. The option may be repeated; `all` mutes every channel. |
| `--unmute-log-channels <names>` | Unmute a comma-separated list of logging channels. The option may be repeated; `all` enables every channel. Unmute selections are applied after mute selections. |

Log channel names are case-insensitive: `Logging`, `Program`, `Runtime`, `Input`, and `Files`.

The initial log configuration can also be supplied through `LOG_MIN_LEVEL`, `LOG_CHANNELS`, and `LOG_CONSOLE`. Explicit command-line logging options modify or override those values. `LOG_CHANNELS` accepts comma-separated channel names, `all`, and names prefixed with `-` to disable them. `LOG_CONSOLE` accepts `1`, `true`, `yes`, or `on` and `0`, `false`, `no`, or `off`.

## Translations

Translations are UTF-8 text overlays stored at the top level of the asset root. Each non-empty line maps a stable string identifier to a quoted value:

```text
Shared_ConfirmationPrompt="Please click Yes or No!"
Intro_Page1="The Environment Agency\npresents\n\nD A S   E R B E"
```

An overlay may contain only the strings that need translation. Missing entries continue to use the original text extracted from `ERBE.EXE`. Keys are case-insensitive. Values support `\\`, `\"`, `\n`, `\r`, and `\t` escapes. Keep translated characters representable in CP437; the engine logs a warning for values that cannot round-trip through that character set.

### DumpStrings translation tool

`tools/DumpStrings` extracts all known original strings from `ERBE.EXE`, decodes them from CP437, and writes a ready-to-edit overlay. Use the executable that matches the supported game version because the extractor reads known offsets from its DOS MZ image.

```shell
dotnet run --project tools/DumpStrings/DumpStrings.csproj -- --exe Game/ERBE.EXE --out Game/my-language.txt
```

| Option | Meaning |
| --- | --- |
| `-h`, `--help` | Show the utility's command-line help. |
| `--exe <path>` | Required. Read original strings from this game executable. The file must exist. |
| `--out <path>` | Write the UTF-8 overlay to this file. When omitted, the generated overlay is written to standard output. |

To create a translation:

1. Dump `ERBE.EXE` to a new `.txt` file in the asset root.
2. Translate the quoted values while preserving the identifiers and overlay syntax.
3. Start the game with the file name, without its extension: `--language my-language`.

If an overlay file is missing, the game uses the original text. Invalid lines and unknown identifiers are skipped and reported on the `Files` log channel.

## License

See [LICENSE](LICENSE) for the GNU General Public License, version 3.
