# Switch

A small Windows tray utility inspired by Punto Switcher. It detects a mistyped word at a word boundary and replaces it when the converted spelling is present in the opposite-language dictionary.

## Features
- Global low-level keyboard monitoring (Windows).
- Automatic English/Russian mistyped-word correction at word boundaries.
- Case preservation for ALL-CAPS and Title Case words.
- Editable word lists in `%APPDATA%\\Switch\\ru.txt` and `en.txt`.
- System tray menu and optional Windows startup.

## Requirements
- Windows 10/11
- .NET 8 SDK (to build)

## Build
```powershell
dotnet build Switch.csproj -c Release
dotnet publish Switch.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
The executable is produced under `bin/Release/net8.0-windows/win-x64/publish/`.

## Run
Launch `Switch.exe`. It starts monitoring immediately and stays in the notification area. Right-click the tray icon to open settings, toggle correction, or exit. Add words to the two dictionary files to improve recognition; each word must be on its own line.

## Notes and limitations
This is an early prototype, not a drop-in replacement for Punto Switcher. It replaces a mistyped word when exactly one converted candidate exists in the built-in or user dictionary; it does not yet change the active Windows layout, provide manual hotkeys, or include a full settings editor. The low-level keyboard hook runs locally and the utility does not transmit or persist typed text. Review and test it before relying on it for important work.

## License
MIT
