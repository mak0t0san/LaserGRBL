; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "LaserGRBL"
#define MyAppVersion "3.8.0"
#define MyAppVersionName "Rhyhorn"
#define MyAppPublisher "LaserGRBL"
#define MyAppURL "https://lasergrbl.com"
#define MyAppExeName "LaserGRBL.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{4BF69C31-8363-4935-9804-CCDD623E7C1F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersionName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={commonpf}\{#MyAppName}
DisableDirPage=no
DisableProgramGroupPage=yes
OutputDir=.\
OutputBaseFilename=install
Compression=zip
SolidCompression=no
InternalCompressLevel=ultra64
CompressionThreads=2
RestartIfNeededByRun=False
Uninstallable=yes
UninstallFilesDir={commonpf}
SetupIconFile=.\install.ico
UninstallDisplayIcon={app}\LaserGRBL.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
;Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: ".\LaserGRBL\bin\Release\LaserGRBL.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\LaserGRBL.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\Autotrace\autotrace.exe"; DestDir: "{app}\Autotrace"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\it\LaserGRBL.resources.dll"; DestDir: "{app}\it"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\da\LaserGRBL.resources.dll"; DestDir: "{app}\da"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\de\LaserGRBL.resources.dll"; DestDir: "{app}\de"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\es\LaserGRBL.resources.dll"; DestDir: "{app}\es"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\fr\LaserGRBL.resources.dll"; DestDir: "{app}\fr"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\pt-BR\LaserGRBL.resources.dll"; DestDir: "{app}\pt-BR"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\ru\LaserGRBL.resources.dll"; DestDir: "{app}\ru"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\zh-CN\LaserGRBL.resources.dll"; DestDir: "{app}\zh-CN"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\sk-SK\LaserGRBL.resources.dll"; DestDir: "{app}\sk-SK"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\hu-HU\LaserGRBL.resources.dll"; DestDir: "{app}\hu-HU"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\cs-CZ\LaserGRBL.resources.dll"; DestDir: "{app}\cs-CZ"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\pl-PL\LaserGRBL.resources.dll"; DestDir: "{app}\pl-PL"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\zh-TW\LaserGRBL.resources.dll"; DestDir: "{app}\zh-TW"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\Driver\*"; DestDir: "{app}\Driver"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\Firmware\*"; DestDir: "{app}\Firmware"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\LaserGRBL.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\StandardMaterials.psh"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\LaserGRBL\bin\Release\Sound\*"; DestDir: "{app}\Sound"; Flags: ignoreversion
Source: ".\lasergrblfile.ico"; DestDir: "{app}"; Flags: ignoreversion

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{commonprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
;Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCR; SubKey: ".nc"; ValueType: string; ValueData: "LaserGRBL gcode file"; Flags: uninsdeletekey
Root: HKCR; SubKey: "LaserGRBL gcode file"; ValueType: string; ValueData: "GCode file for laser engraving"; Flags: uninsdeletekey
Root: HKCR; SubKey: "LaserGRBL gcode file\Shell\Open\Command"; ValueType: string; ValueData: """{app}\LaserGRBL.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCR; Subkey: "LaserGRBL gcode file\DefaultIcon"; ValueType: string; ValueData: "{app}\lasergrblfile.ico,0"; Flags: uninsdeletevalue
