; StarTooth Inno Setup Script
;
; Standard wizard: Welcome / License / Directory / StartMenu / Tasks / Ready / Finish.
;
; Adapted from the MouseCross and ControlNav installers, minus everything those two
; carry for their uiAccess / assistive-technology nature: no AT registration, no
; uiAccess launch dance, no Program-Files-mandated location. StarTooth is an ordinary
; tray tool.
;
; Autostart is deliberately NOT offered here. The application owns its autostart entry
; (HKCU\...\Run) as the single source of truth, set from its own Settings dialog. A second
; writer in the installer would run under the elevated hive and could disagree with the app.
;
; Version is passed in via ISCC.exe /DMyAppVersion=X.Y.Z (build_installer.ps1 reads it from
; the .csproj). VersionInfoVersion needs a plain numeric quad, passed separately.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#ifndef MyAppNumericVersion
  #define MyAppNumericVersion "0.0.0.0"
#endif

#define MyAppName       "StarTooth"
#define MyAppPublisher  "Stefan Lohmaier"
#define MyAppURL        "https://github.com/slohmaier/StarTooth"
#define MyAppExeName    "StarTooth.exe"
#define MyAppId         "{{9C3D7E21-4B8A-4E2F-A1D6-2F7B9E5C4A30}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
VersionInfoVersion={#MyAppNumericVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
AppCopyright=Copyright (C) 2026 Stefan Lohmaier

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=no
DisableDirPage=no
DisableReadyPage=no
DisableWelcomePage=no
DisableFinishedPage=no
AllowNoIcons=yes

; Per-machine install: all-users Start Menu entry and a clean Programs & Features
; uninstall. No uiAccess here, so Program Files is a convention, not a requirement.
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; A running instance is closed by the installer in [Code] PrepareToInstall, so the user
; never sees a "please close the application" prompt.
CloseApplications=no
RestartApplications=no

Compression=lzma2/ultra64
SolidCompression=yes
OutputDir=output
OutputBaseFilename={#MyAppName}-Setup-{#MyAppVersion}
SetupIconFile=..\Resources\{#MyAppName}.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
WizardStyle=modern

LicenseFile=..\LICENSE

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Self-contained single-file build produced by build_installer.ps1 (already signed).
Source: "payload\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE";              DestDir: "{app}"; DestName: "LICENSE.txt";   Flags: ignoreversion
Source: "..\README.md";            DestDir: "{app}"; DestName: "README.txt";    Flags: ignoreversion
Source: "..\CHANGELOG.md";         DestDir: "{app}"; DestName: "CHANGELOG.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}";                       Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}";               Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; runasoriginaluser drops the installer's elevated token back to the logged-in user before
; launching, so the tray app does not run elevated.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; \
    Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallRun]
Filename: "{cmd}"; Parameters: "/C taskkill /F /IM {#MyAppExeName} 2>nul"; \
    Flags: runhidden; RunOnceId: "KillStarTooth"

[Code]
// Close any running StarTooth so its files aren't locked during install. It is a plain tray
// app with no IPC channel, so a force-kill is the only lever; nothing is lost but the tray icon.
procedure CloseRunningInstance;
var
  killRc: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM {#MyAppExeName}', '',
       SW_HIDE, ewWaitUntilTerminated, killRc);
  Sleep(400);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  CloseRunningInstance;
  Result := '';
end;

// On uninstall, drop the app-managed autostart entry if it points here, so Windows does not
// try to launch a deleted binary at the next sign-in. Best-effort: under an elevated uninstall
// this touches the elevated user's hive, which is the right one on a single-user machine.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RegDeleteValue(HKEY_CURRENT_USER,
      'Software\Microsoft\Windows\CurrentVersion\Run', '{#MyAppName}');
end;
