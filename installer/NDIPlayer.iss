#ifndef RuntimeFile
  #error RuntimeFile must point to the verified official NDI 6 redistributable
#endif

[Setup]
AppId={{4031E489-7E18-4ED7-A0AA-49283A04AA63}
AppName=NDIPlayer
AppVersion=0.12.2
AppPublisher=Michele Dallaglio
AppPublisherURL=https://github.com/dallagliomichele/NDIPLayer
DefaultDirName={localappdata}\Programs\NDIPlayer
DefaultGroupName=NDIPlayer
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\dist
OutputBaseFilename=NDIPlayer-0.12.2-Setup-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\WatchoutImageNDI.exe
CloseApplications=yes
RestartApplications=no
InfoBeforeFile=NOTICE.txt

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Files]
Source: "..\bin\NAudio.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\third-party\NAudio-LICENSE.txt"; DestDir: "{app}\audio"; Flags: ignoreversion
Source: "..\third-party\NAudio-NOTICE.txt"; DestDir: "{app}\audio"; Flags: ignoreversion
Source: "..\bin\WatchoutImageNDI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\AUDIO.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\COMPANION.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\video\*"; DestDir: "{app}\video"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\third-party\FFmpeg-NOTICE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "NOTICE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RuntimeFile}"; DestName: "NDI6Runtime.exe"; Flags: dontcopy

[Icons]
Name: "{group}\NDIPlayer"; Filename: "{app}\WatchoutImageNDI.exe"
Name: "{group}\Disinstalla NDIPlayer"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\WatchoutImageNDI.exe"; Description: "Avvia NDIPlayer"; Flags: nowait postinstall skipifsilent

[Code]
var RuntimeRestart: Boolean;

function RuntimeCurrentAt(Path: String): Boolean;
var MS, LS: Cardinal;
begin
  Result := GetVersionNumbers(AddBackslash(Path) + 'Processing.NDI.Lib.x64.dll', MS, LS);
  if Result then
    Result := (MS > $00060003) or ((MS = $00060003) and (LS >= $00020000));
end;

function RuntimeCurrent: Boolean;
var Dir: String;
begin
  Result := RuntimeCurrentAt(ExpandConstant('{commonpf64}\NDI\NDI 6 Runtime\v6')) or
            RuntimeCurrentAt(ExpandConstant('{commonpf64}\NDI\NDI 6 Runtime'));
  if not Result then begin
    if RegQueryStringValue(HKLM64, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'NDI_RUNTIME_DIR_V6', Dir) then
      Result := RuntimeCurrentAt(Dir);
  end;
end;

function InitializeSetup: Boolean;
var Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release);
  if Result then Result := Release >= 528040;
  if not Result then
    MsgBox('Occorre .NET Framework 4.8 o successivo. Installalo tramite Windows Update e riavvia questo programma.', mbError, MB_OK);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var ExitCode: Integer;
begin
  Result := '';
  if RuntimeCurrent then exit;
  if WizardSilent then begin
    Result := 'Il runtime NDI richiede installazione interattiva e accettazione della sua licenza. Riavvia il setup senza /SILENT.';
    exit;
  end;
  ExtractTemporaryFile('NDI6Runtime.exe');
  if not ShellExec('open', ExpandConstant('{tmp}\NDI6Runtime.exe'), '/NORESTART', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ExitCode) then begin
    Result := 'Impossibile avviare il runtime NDI. Autorizza la richiesta di Windows e riprova.';
    exit;
  end;
  if (ExitCode <> 0) and (ExitCode <> 3010) then begin
    Result := 'Installazione runtime NDI annullata o fallita (codice ' + IntToStr(ExitCode) + '). NDIPlayer non è stato installato.';
    exit;
  end;
  if not RuntimeCurrent then begin
    Result := 'Runtime NDI non rilevato dopo il setup. Riprova installando il runtime nel percorso predefinito.';
    exit;
  end;
  RuntimeRestart := ExitCode = 3010;
end;

function NeedRestart: Boolean;
begin
  Result := RuntimeRestart;
end;
