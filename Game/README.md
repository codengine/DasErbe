# Das Erbe

## Run the game

Extract the release archive and start the game from the directory that contains the executable and the `Game` directory.

On Windows, double-click `Game.Desktop.exe` or run:

```powershell
.\Game.Desktop.exe
```

On Linux, run:

```shell
./Game.Desktop
```

Pass command-line options after the executable name. For example, start the game with the English translation overlay:

```powershell
.\Game.Desktop.exe --language en
```

```shell
./Game.Desktop --language en
```

## Command-line options

| Option | Meaning |
| --- | --- |
| `--asset-root <path>` | Use this absolute path as the game asset directory instead of the `Game` directory beside the executable. |
| `--log-level <level>` | Set the minimum log level. Accepted values are `trace`, `debug`, `info`, `warn`, and `error`; the default is `info`. |
| `--log-file <path>` | Write logs to this path. The default is `logs/game.log` below the current working directory. |
| `--no-console-log` | Disable console logging. File logging remains enabled. |
| `--language <name>` | Load `<name>.txt` from the game asset directory as a translation overlay. Supply the name without `.txt`, for example `--language en`. |
| `--use-classic-interactions` | Preserve the original double-confirmation interaction flow. Without this option, the game uses the modernized interaction flow. |
| `--dksn-mode` | Show varied German, command-specific fallback responses for scene-hotspot actions that have no implementation. |
| `--mute-log-channels <names>` | Mute a comma-separated list of logging channels. The option may be repeated; `all` mutes every channel. |
| `--unmute-log-channels <names>` | Unmute a comma-separated list of logging channels. The option may be repeated; `all` enables every channel. Unmute selections are applied after mute selections. |

Log channel names are case-insensitive: `Logging`, `Program`, `Runtime`, `Input`, and `Files`.
