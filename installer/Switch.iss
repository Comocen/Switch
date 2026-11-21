#define AppName "Switch"
#define AppVersion "1.0.0"
#define AppPublisher "Comocen"
#define AppExeName "Switch.exe"

[Setup]
AppId={{A2D5B1A4-6D2E-4C31-9D18-9E6B54B3F220}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\Switch
DefaultGroupName=Switch
OutputDir=..\artifacts
OutputBaseFilename=Switch-Setup-x64
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#AppExeName}

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Switch"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\Switch"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные задачи:"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Запустить Switch"; Flags: nowait postinstall skipifsilent
