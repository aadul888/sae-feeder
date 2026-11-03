; Script generated for Inno Setup 6
; SAE Feeder - Dapodik Data Synchronizer

#define MyAppName "SAE Feeder"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "SAE Education"
#define MyAppURL "https://sae.smakpal.sch.id"
#define MyAppExeName "SAE-Feeder.exe"

[Setup]
AppId={{D814FA79-7DF6-4E90-95B9-F2E7D8A41D32}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\SAE-Feeder
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename=SAE-Feeder-InnoSetup
SetupIconFile=sae-feeder.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\sae-feeder.ico

[Languages]
Name: "indonesian"; MessagesFile: "compiler:Languages\Indonesian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "build\staging\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Jangan menimpa konfigurasi yang sudah ada jika pengguna menginstal ulang
Source: "build\staging\dynamic_config.json"; DestDir: "{app}"; Flags: onlyifdoesntexist

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\sae-feeder.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\sae-feeder.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
