# PuntoSwitch

A small Windows tray utility inspired by Punto Switcher. It detects a mistyped word at a word boundary and replaces it when the converted spelling is present in the opposite-language dictionary.

## Features
- Global low-level keyboard monitoring (Windows).
- Automatic English/Russian keyboard-layout typo correction.
- Case preservation for ALL-CAPS and Title Case words.
- Editable word lists in `%APPDATA%\\PuntoSwitch\\ru.txt` and `en.txt`.
- System tray menu and optional Windows startup.

## Requirements
- Windows 10/11
- .NET 8 SDK (to build)

## Build
```powershell
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
The executable is produced under `bin/Release/net8.0-windows/win-x64/publish/`.

## Run
Launch `PuntoSwitch.exe`. It starts monitoring immediately and stays in the notification area. Right-click the tray icon to open settings, toggle correction, or exit. Add words to the two dictionary files to improve recognition; each word must be on its own line.

## Notes and limitations
This is an initial community-sized implementation, not a drop-in replacement for every Punto Switcher feature. Correction is dictionary-based: a candidate is changed only when exactly one converted form is found. Add domain-specific words to avoid unwanted corrections. The keyboard hook runs locally and does not transmit keystrokes. The utility does not store typed text.

## License
MIT
