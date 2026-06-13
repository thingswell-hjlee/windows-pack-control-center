; Windows Pack Control Center - Inno Setup Script
; Thingswell Co., Ltd.

#define AppName "Windows Pack Control Center"
#define AppVersion "1.0.0"
#define AppPublisher "Thingswell Co., Ltd."
#define AppURL "https://thingswell.com"
#define AppExeName "ControlCenter.Gateway.exe"
#define DefaultPort "8088"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
DefaultDirName={autopf}\Thingswell\WindowsPackControlCenter
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=license.txt
OutputDir=..\..\dist\installer
OutputBaseFilename=WindowsPackControlCenter_Setup_{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
SetupLogging=yes
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Windows 시작 시 자동 실행"; GroupDescription: "시작 옵션:"

[Files]
; Main application files (from publish output)
Source: "..\..\dist\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Configuration template
Source: "..\..\gateway\src\appsettings.json"; DestDir: "{commonappdata}\Thingswell\WindowsPackControlCenter"; Flags: onlyifdoesntexist; Permissions: everyone-modify

[Dirs]
Name: "{commonappdata}\Thingswell\WindowsPackControlCenter"; Permissions: everyone-modify
Name: "{commonappdata}\Thingswell\WindowsPackControlCenter\logs"; Permissions: everyone-modify
Name: "{commonappdata}\Thingswell\WindowsPackControlCenter\data"; Permissions: everyone-modify

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{commonappdata}\Thingswell\WindowsPackControlCenter"
Name: "{group}\{#AppName} 대시보드"; Filename: "http://localhost:{#DefaultPort}"
Name: "{group}\{#AppName} 제거"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{commonappdata}\Thingswell\WindowsPackControlCenter"; Tasks: desktopicon

[Registry]
; Auto-start registry entry
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "WindowsPackControlCenter"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
; Open dashboard after installation
Filename: "http://localhost:{#DefaultPort}"; Description: "대시보드 열기"; Flags: postinstall shellexec skipifsilent nowait
; Start the service after installation
Filename: "{app}\{#AppExeName}"; Description: "Control Center 시작"; Flags: postinstall nowait skipifsilent runascurrentuser

[UninstallRun]
; Stop the application before uninstall
Filename: "taskkill"; Parameters: "/F /IM {#AppExeName}"; Flags: runhidden

[UninstallDelete]
; Only delete program files, preserve user data
Type: filesandordirs; Name: "{app}"

[Code]
// Custom code for port conflict detection and firewall rules

procedure AddFirewallException();
var
  ResultCode: Integer;
begin
  Exec('netsh', 'advfirewall firewall add rule name="Windows Pack Control Center" dir=in action=allow protocol=TCP localport={#DefaultPort}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure RemoveFirewallException();
var
  ResultCode: Integer;
begin
  Exec('netsh', 'advfirewall firewall delete rule name="Windows Pack Control Center"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    AddFirewallException();
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RemoveFirewallException();
    // Ask user if they want to delete data
    if MsgBox('사용자 데이터(데이터베이스, 로그, 설정)를 삭제하시겠습니까?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      DelTree(ExpandConstant('{commonappdata}\Thingswell\WindowsPackControlCenter'), True, True, True);
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
end;
