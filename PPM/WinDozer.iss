[Setup]
AppName=WinDozer
AppVersion=0.8.1
AppPublisher=ElioSoft
DefaultDirName={localappdata}\ElioSoft\WinDozer
DefaultGroupName=WinDozer
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
OutputBaseFilename=WinDozer-Setup
UninstallDisplayIcon={app}\WinDozer.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\WinDozer"; Filename: "{app}\WinDozer.exe"

[Run]
Filename: "{app}\WinDozer.exe"; Description: "Launch WinDozer"; Flags: nowait postinstall skipifsilent

[Code]
// WinDozer lives in the tray with its window hidden, so Inno can't close it by itself.
procedure KillApp();
var R: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/im WinDozer.exe /f', '', SW_HIDE, ewWaitUntilTerminated, R);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  KillApp();      // lets upgrades overwrite the running exe
  Result := '';
end;

function InitializeUninstall(): Boolean;
begin
  KillApp();
  Result := True;
end;