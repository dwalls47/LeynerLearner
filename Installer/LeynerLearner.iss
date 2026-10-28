; Inno Setup script for Leyner Learner. Build it with build.ps1 (in this folder), which publishes the app into
; publish\ and then compiles this script; or, after publishing, with:  ISCC /DAppVersion=1.0.0 LeynerLearner.iss

#define AppName "Leyner Learner"
#define AppExe "LeynerLearner.exe"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
; AppId identifies the app to Windows for upgrades and uninstall: never change it.
AppId={{25CCF60A-AD5D-445E-B706-E779F111601F}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=dwalls47
AppPublisherURL=https://github.com/dwalls47/LeynerLearner
AppSupportURL=https://github.com/dwalls47/LeynerLearner
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Installs for the current user without administrator rights, or for all users if chosen.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; The app is published self-contained for 64-bit Windows 10 or later.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
LicenseFile=..\LICENSE
SetupIconFile=..\LeynerLearner\LeynerLearner.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
OutputDir=output
OutputBaseFilename=LeynerLearner-Setup-{#AppVersion}
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; The published app: the program, the .NET runtime and the Data folder (text, schedules, fonts and their licenses).
Source: "publish\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

; Downloaded data updates live in %LOCALAPPDATA%\LeynerLearner; they are kept on uninstall, like other user data.
