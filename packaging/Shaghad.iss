#ifndef ReleaseVersion
  #error ReleaseVersion must come from Version.props via scripts/release.ps1
#endif
[Setup]
AppId={{B3E753DC-D5C1-4C02-9132-5F9925AA0B17}
AppName=SHAGHAD
AppVersion={#ReleaseVersion}
AppPublisher=SHAGHAD
AppPublisherURL=https://github.com/Shaghad/shaghad-windows
DefaultDirName={localappdata}\Programs\SHAGHAD
DefaultGroupName=SHAGHAD
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\branding\icons\shaghad.ico
LicenseFile=..\LICENSE
OutputDir={#OutputDirectory}
OutputBaseFilename=SHAGHAD-Setup-v{#ReleaseVersion}-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
UninstallDisplayIcon={app}\Shaghad.exe
[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; Flags: unchecked
[Files]
Source: "{#PublishDirectory}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\SHAGHAD"; Filename: "{app}\Shaghad.exe"
Name: "{autodesktop}\SHAGHAD"; Filename: "{app}\Shaghad.exe"; Tasks: desktopicon
; No launch, autostart, drivers, firewall, DNS or user-data deletion actions.
