; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "ZScreen"     
#define ExePath "ZScreen\bin\x86\Debug\ZScreen.exe"
#define MyAppVersion GetStringFileInfo(ExePath, "Assembly Version")
#define MyAppPublisher "ZScreen Developers"
#define MyAppURL "http://code.google.com/p/zscreen"

[Setup]
AllowNoIcons=true
AppMutex=Global\0167D1A0-6054-42f5-BA2A-243648899A6B
AppId={#MyAppName}
AppName={#MyAppName}
AppPublisher={#MyAppName}
AppPublisherURL=http://code.google.com/p/zscreen
AppSupportURL=http://code.google.com/p/zscreen/issues/list
AppUpdatesURL=http://code.google.com/p/zscreen/downloads/list
AppVerName={#MyAppName} {#MyAppVersion}
AppVersion={#MyAppVersion}
ArchitecturesAllowed=x86 x64 ia64
;ArchitecturesInstallIn64BitMode=x64 ia64
Compression=lzma/ultra64
CreateAppDir=true
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DirExistsWarning=no
InfoAfterFile=ZScreenLib\Documents\license.txt
InfoBeforeFile=ZScreenLib\Documents\VersionHistory.txt
InternalCompressLevel=ultra64
LanguageDetectionMethod=uilanguage
MinVersion=4.90.3000,5.0.2195sp3
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-dev-setup
OutputDir=..\Output\
PrivilegesRequired=none
;SetupIconFile=ZScreen\Resources\zss-main.ico
ShowLanguageDialog=auto
ShowUndisplayableLanguages=false
SignedUninstaller=false
SolidCompression=true
Uninstallable=true
UninstallDisplayIcon={app}\{#MyAppName}.exe
UsePreviousAppDir=yes
UsePreviousGroup=yes
VersionInfoCompany={#MyAppName}
VersionInfoTextVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: ZScreen\bin\x86\Debug\*.exe; Excludes: *.vshost.exe; DestDir: {app}; Flags: ignoreversion
Source: ZScreen\bin\x86\Debug\*.pdb; Excludes: *.vshost.exe; DestDir: {app}; Flags: ignoreversion
Source: ZScreen\bin\x86\Debug\*.dll; DestDir: {app}; Flags: ignoreversion recursesubdirs
Source: ZScreen\bin\x86\Debug\*.xml; DestDir: {app}; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppName}.exe"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppName}.exe"; Tasks: desktopicon

[InstallDelete] 
Type: files; Name: "{group}\ZUploader.lnk";

[Run]
Filename: {app}\{#MyAppName}.exe.; Description: {cm:LaunchProgram,ZScreen}; Flags: nowait postinstall skipifsilent
