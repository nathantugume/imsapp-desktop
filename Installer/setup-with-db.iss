; IMS App Desktop - Inno Setup script (with bundled MySQL)
; Build: .\Installer\build-installer.ps1 -IncludeDatabase

#define AppName "IMS App"
#define AppExe "imsapp-desktop.exe"
#ifndef AppVersion
#define AppVersion "1.0.0"
#endif

[Setup]
AppId={{c8c726b2-270e-4d7d-8be0-9b4d7c206e93}
AppName={#AppName} (with Database)
AppVersion={#AppVersion}
AppPublisher=Nathan
AppPublisherURL=https://github.com/nathantugume/imsapp-desktop
DefaultDirName={localappdata}\IMS App
DefaultGroupName=IMS App
AllowNoIcons=yes
OutputDir=Output
OutputBaseFilename=IMSApp-Setup-WithDB-{#AppVersion}
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Published app files
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Bundled MySQL (must exist in mysql-staging before build)
Source: "mysql-staging\*"; DestDir: "{app}\mysql"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
