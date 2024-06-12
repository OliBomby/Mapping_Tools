// comment out dependency defines to disable installing them
//#define UseMsi45

//#define UseDotNet11
//#define UseDotNet20
//#define UseDotNet35
//#define UseDotNet40Client
//#define UseDotNet40Full
//#define UseDotNet45
//#define UseDotNet46
//#define UseDotNet47
//#define UseDotNet48

// requires netcorecheck.exe and netcorecheck_x64.exe (see download link below)
#define UseNetCoreCheck
#ifdef UseNetCoreCheck
  //#define UseNetCore31
  //#define UseNetCore31Asp
  //#define UseNetCore31Desktop
  //#define UseDotNet50
  //#define UseDotNet50Asp
  #define UseDotNet50Desktop
#endif

//#define UseVC2005
//#define UseVC2008
//#define UseVC2010
//#define UseVC2012
//#define UseVC2013
//#define UseVC2015To2019

// requires dxwebsetup.exe (see download link below)
//#define UseDirectX

//#define UseSql2008Express
//#define UseSql2012Express
//#define UseSql2014Express
//#define UseSql2016Express
//#define UseSql2017Express
//#define UseSql2019Express

#define MyAppName "Mapping Tools"
#define MyAppVersion "0.0.0"
#define MyAppPublisher "OliBomby"
#define MyAppURL "https://mappingtools.github.io/"
#define MyAppExeName "Mapping Tools.exe"
#define BuildFolderPath "Mapping_Tools\bin\x86\Release\net5.0-windows"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{50E2DF2A-2D83-4958-B5A6-EBFD651B9CA7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}    
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=no
DisableWelcomePage=no
OutputDir=.\
OutputBaseFilename=mapping_tools_installer_x86
SetupIconFile=.\Mapping_Tools\Data\mt_icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern   
LicenseFile=LICENCE

MinVersion=6.1
PrivilegesRequired=admin

// remove next line if you only deploy 32-bit binaries and dependencies
// ArchitecturesInstallIn64BitMode=x64

// dependency installation requires ready page and ready memo to be enabled (default behaviour)
DisableReadyPage=no
DisableReadyMemo=no

// shared code for installing the dependencies
[Code]
// types and variables
type
  TDependency = record
    Filename: String;
    Parameters: String;
    Title: String;
    URL: String;
    Checksum: String;
    ForceSuccess: Boolean;
    InstallClean: Boolean;
    RebootAfter: Boolean;
  end;

  InstallResult = (InstallSuccessful, InstallRebootRequired, InstallError);

var
  MemoInstallInfo: String;
  Dependencies: array of TDependency;
  DelayedReboot, ForceX86: Boolean;
  DownloadPage: TDownloadWizardPage;

procedure AddDependency(const Filename, Parameters, Title, URL, Checksum: String; const ForceSuccess, InstallClean, RebootAfter: Boolean);
var
  Dependency: TDependency;
  I: Integer;
begin
  MemoInstallInfo := MemoInstallInfo + #13#10 + '%1' + Title;

  Dependency.Filename := Filename;
  Dependency.Parameters := Parameters;
  Dependency.Title := Title;

  if FileExists(ExpandConstant('{tmp}{\}') + Filename) then begin
    Dependency.URL := '';
  end else begin
    Dependency.URL := URL;
  end;

  Dependency.Checksum := Checksum;
  Dependency.ForceSuccess := ForceSuccess;
  Dependency.InstallClean := InstallClean;
  Dependency.RebootAfter := RebootAfter;

  I := GetArrayLength(Dependencies);
  SetArrayLength(Dependencies, I + 1);
  Dependencies[I] := Dependency;
end;

function IsPendingReboot: Boolean;
var
  Value: String;
begin
  Result := RegQueryMultiStringValue(HKEY_LOCAL_MACHINE, 'SYSTEM\CurrentControlSet\Control\Session Manager', 'PendingFileRenameOperations', Value) or
    (RegQueryMultiStringValue(HKEY_LOCAL_MACHINE, 'SYSTEM\CurrentControlSet\Control\Session Manager', 'SetupExecute', Value) and (Value <> ''));
end;

function InstallProducts: InstallResult;
var
  ResultCode, I, ProductCount: Integer;
begin
  Result := InstallSuccessful;
  ProductCount := GetArrayLength(Dependencies);
  MemoInstallInfo := SetupMessage(msgReadyMemoTasks);

  if ProductCount > 0 then begin
    DownloadPage.Show;

    for I := 0 to ProductCount - 1 do begin
      if Dependencies[I].InstallClean and (DelayedReboot or IsPendingReboot) then begin
        Result := InstallRebootRequired;
        break;
      end;

      DownloadPage.SetText(Dependencies[I].Title, '');
      DownloadPage.SetProgress(I + 1, ProductCount + 1);

      while True do begin
        ResultCode := 0;
        if ShellExec('', ExpandConstant('{tmp}{\}') + Dependencies[I].Filename, Dependencies[I].Parameters, '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then begin
          if Dependencies[I].RebootAfter then begin
            // delay reboot after install if we installed the last dependency anyways
            if I = ProductCount - 1 then begin
              DelayedReboot := True;
            end else begin
              Result := InstallRebootRequired;
              MemoInstallInfo := Dependencies[I].Title;
            end;
            break;
          end else if (ResultCode = 0) or Dependencies[I].ForceSuccess then begin
            break;
          end else if ResultCode = 3010 then begin
            // Windows Installer ResultCode 3010: ERROR_SUCCESS_REBOOT_REQUIRED
            DelayedReboot := True;
            break;
          end;
        end;

        case SuppressibleMsgBox(FmtMessage(SetupMessage(msgErrorFunctionFailed), [Dependencies[I].Title, IntToStr(ResultCode)]), mbError, MB_ABORTRETRYIGNORE, IDIGNORE) of
          IDABORT: begin
            Result := InstallError;
            MemoInstallInfo := MemoInstallInfo + #13#10 + '      ' + Dependencies[I].Title;
            break;
          end;
          IDIGNORE: begin
            MemoInstallInfo := MemoInstallInfo + #13#10 + '      ' + Dependencies[I].Title;
            break;
          end;
        end;
      end;

      if Result <> InstallSuccessful then begin
        break;
      end;
    end;

    DownloadPage.Hide;
  end;
end;

// Inno Setup event functions
procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  DelayedReboot := False;

  case InstallProducts of
    InstallError: begin
      Result := MemoInstallInfo;
    end;
    InstallRebootRequired: begin
      Result := MemoInstallInfo;
      NeedsRestart := True;

      // write into the registry that the installer needs to be executed again after restart
      RegWriteStringValue(HKEY_CURRENT_USER, 'SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce', 'InstallBootstrap', ExpandConstant('{srcexe}'));
    end;
  end;
end;

function NeedRestart: Boolean;
begin
  Result := DelayedReboot;
end;

function UpdateReadyMemo(const Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := '';
  if MemoUserInfoInfo <> '' then begin
    Result := Result + MemoUserInfoInfo + Newline + NewLine;
  end;
  if MemoDirInfo <> '' then begin
    Result := Result + MemoDirInfo + Newline + NewLine;
  end;
  if MemoTypeInfo <> '' then begin
    Result := Result + MemoTypeInfo + Newline + NewLine;
  end;
  if MemoComponentsInfo <> '' then begin
    Result := Result + MemoComponentsInfo + Newline + NewLine;
  end;
  if MemoGroupInfo <> '' then begin
    Result := Result + MemoGroupInfo + Newline + NewLine;
  end;
  if MemoTasksInfo <> '' then begin
    Result := Result + MemoTasksInfo;
  end;

  if MemoInstallInfo <> '' then begin
    if MemoTasksInfo = '' then begin
      Result := Result + SetupMessage(msgReadyMemoTasks);
    end;
    Result := Result + FmtMessage(MemoInstallInfo, [Space]);
  end;
end;

function NextButtonClick(const CurPageID: Integer): Boolean;
var
  I, ProductCount: Integer;
  Retry: Boolean;
begin
  Result := True;

  if (CurPageID = wpReady) and (MemoInstallInfo <> '') then begin
    DownloadPage.Show;

    ProductCount := GetArrayLength(Dependencies);
    for I := 0 to ProductCount - 1 do begin
      if Dependencies[I].URL <> '' then begin
        DownloadPage.Clear;
        DownloadPage.Add(Dependencies[I].URL, Dependencies[I].Filename, Dependencies[I].Checksum);

        Retry := True;
        while Retry do begin
          Retry := False;

          try
            DownloadPage.Download;
          except
            if DownloadPage.AbortedByUser then begin
              Result := False;
              I := ProductCount;
            end else begin
              case SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbError, MB_ABORTRETRYIGNORE, IDIGNORE) of
                IDABORT: begin
                  Result := False;
                  I := ProductCount;
                end;
                IDRETRY: begin
                  Retry := True;
                end;
              end;
            end;
          end;
        end;
      end;
    end;

    DownloadPage.Hide;
  end;
end;

// architecture helper functions
function IsX64: Boolean;
begin
  Result := not ForceX86 and Is64BitInstallMode;
end;

function GetString(const x86, x64: String): String;
begin
  if IsX64 then begin
    Result := x64;
  end else begin
    Result := x86;
  end;
end;

function GetArchitectureSuffix: String;
begin
  Result := GetString('', '_x64');
end;

function GetArchitectureTitle: String;
begin
  Result := GetString(' (x86)', ' (x64)');
end;

#ifdef UseNetCoreCheck
// source code: https://github.com/dotnet/deployment-tools/tree/master/src/clickonce/native/projects/NetCoreCheck
function IsNetCoreInstalled(const Version: String): Boolean;
var
  ResultCode: Integer;
begin
  if not FileExists(ExpandConstant('{tmp}{\}') + 'netcorecheck' + GetArchitectureSuffix + '.exe') then begin
    ExtractTemporaryFile('netcorecheck' + GetArchitectureSuffix + '.exe');
  end;
  Result := ShellExec('', ExpandConstant('{tmp}{\}') + 'netcorecheck' + GetArchitectureSuffix + '.exe', Version, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;
#endif

[Languages]
Name: en; MessagesFile: "compiler:Default.isl"
Name: nl; MessagesFile: "compiler:Languages\Dutch.isl"
Name: de; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
#ifdef UseNetCoreCheck
// download netcorecheck.exe: https://go.microsoft.com/fwlink/?linkid=2135256
// download netcorecheck_x64.exe: https://go.microsoft.com/fwlink/?linkid=2135504
Source: "lib\netcorecheck.exe"; Flags: dontcopy noencryption
Source: "lib\netcorecheck_x64.exe"; Flags: dontcopy noencryption
#endif   

#ifdef UseDirectX
Source: "dxwebsetup.exe"; Flags: dontcopy noencryption
#endif

Source: "{#BuildFolderPath}\EditorReader.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Mapping Tools.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Mapping Tools.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Mapping Tools.dll.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Mapping Tools.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Mapping Tools.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Mapping Tools.runtimeconfig.dev.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Mapping Tools.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\MaterialDesignColors.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\MaterialDesignThemes.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Microsoft.WindowsAPICodePack.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Microsoft.WindowsAPICodePack.Shell.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\NAudio.Asio.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\NAudio.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\NAudio.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\NAudio.Midi.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\NAudio.Vorbis.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\NAudio.Wasapi.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\NAudio.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\NAudio.WinMM.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\NonInvasiveKeyboardHookLibrary.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\NVorbis.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\OggVorbisEncoder.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Onova.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\OsuMemoryDataProvider.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Overlay.NET.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Process.NET.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\ProcessMemoryDataFinder.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\SharpDX.Direct2D1.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\SharpDX.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\SharpDX.DXGI.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\SHARPDX.Mathematics.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Xceed.Wpf.Toolkit.NET5.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Xceed.Wpf.AvalonDock.NET5.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Xceed.Wpf.AvalonDock.Themes.VS2010.NET5.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Xceed.Wpf.AvalonDock.Themes.Metro.NET5.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Xceed.Wpf.AvalonDock.Themes.Aero.NET5.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\VirtualizingWrapPanel.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolderPath}\Microsoft.Xaml.Behaviors.dll"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup: Boolean;
var
  Version: String;
  PackedVersion: Int64;
begin
#ifdef UseMsi45
  // https://www.microsoft.com/en-US/download/details.aspx?id=8483
  if not GetPackedVersion(ExpandConstant('{sys}{\}msi.dll'), PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(4, 5, 0, 0)) < 0) then begin
    AddDependency('msi45' + GetArchitectureSuffix + '.msu',
      '/quiet /norestart',
      'Windows Installer 4.5',
      GetString('https://download.microsoft.com/download/2/6/1/261fca42-22c0-4f91-9451-0e0f2e08356d/Windows6.0-KB942288-v2-x86.msu', 'https://download.microsoft.com/download/2/6/1/261fca42-22c0-4f91-9451-0e0f2e08356d/Windows6.0-KB942288-v2-x64.msu'),
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet11
  // https://www.microsoft.com/en-US/download/details.aspx?id=26
  if not IsDotNetInstalled(net11, 0) then begin
    AddDependency('dotnetfx11.exe',
      '/q',
      '.NET Framework 1.1',
      'https://download.microsoft.com/download/a/a/c/aac39226-8825-44ce-90e3-bf8203e74006/dotnetfx.exe',
      '', False, False, False);
  end;

  // https://www.microsoft.com/en-US/download/details.aspx?id=33
  if not IsDotNetInstalled(net11, 1) then begin
    AddDependency('dotnetfx11sp1.exe',
      '/q',
      '.NET Framework 1.1 Service Pack 1',
      'https://download.microsoft.com/download/8/b/4/8b4addd8-e957-4dea-bdb8-c4e00af5b94b/NDP1.1sp1-KB867460-X86.exe',
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet20
  // https://www.microsoft.com/en-US/download/details.aspx?id=1639
  if not IsDotNetInstalled(net20, 2) then begin
    AddDependency('dotnetfx20' + GetArchitectureSuffix + '.exe',
      '/lang:enu /passive /norestart',
      '.NET Framework 2.0 Service Pack 2',
      GetString('https://download.microsoft.com/download/c/6/e/c6e88215-0178-4c6c-b5f3-158ff77b1f38/NetFx20SP2_x86.exe', 'https://download.microsoft.com/download/c/6/e/c6e88215-0178-4c6c-b5f3-158ff77b1f38/NetFx20SP2_x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet35
  // https://www.microsoft.com/en-US/download/details.aspx?id=22
  if not IsDotNetInstalled(net35, 1) then begin
    AddDependency('dotnetfx35.exe',
      '/lang:enu /passive /norestart',
      '.NET Framework 3.5 Service Pack 1',
      'https://download.microsoft.com/download/0/6/1/061f001c-8752-4600-a198-53214c69b51f/dotnetfx35setup.exe',
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet40Client
  // https://www.microsoft.com/en-US/download/details.aspx?id=24872
  if not IsDotNetInstalled(net4client, 0) and not IsDotNetInstalled(net4full, 0) then begin
    AddDependency('dotNetFx40_Client_setup.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.0 Client',
      'https://download.microsoft.com/download/7/B/6/7B629E05-399A-4A92-B5BC-484C74B5124B/dotNetFx40_Client_setup.exe',
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet40Full
  // https://www.microsoft.com/en-US/download/details.aspx?id=17718
  if not IsDotNetInstalled(net4full, 0) then begin
    AddDependency('dotNetFx40_Full_setup.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.0',
      'https://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe',
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet45
  // https://www.microsoft.com/en-US/download/details.aspx?id=42643
  if not IsDotNetInstalled(net452, 0) then begin
    AddDependency('dotnetfx45.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.5.2',
      'https://download.microsoft.com/download/B/4/1/B4119C11-0423-477B-80EE-7A474314B347/NDP452-KB2901954-Web.exe',
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet46
  // https://www.microsoft.com/en-US/download/details.aspx?id=53345
  if not IsDotNetInstalled(net462, 0) then begin
    AddDependency('dotnetfx46.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.6.2',
      'https://download.microsoft.com/download/D/5/C/D5C98AB0-35CC-45D9-9BA5-B18256BA2AE6/NDP462-KB3151802-Web.exe',
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet47
  // https://support.microsoft.com/en-US/help/4054531
  if not IsDotNetInstalled(net472, 0) then begin
    AddDependency('dotnetfx47.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.7.2',
      'https://download.microsoft.com/download/0/5/C/05C1EC0E-D5EE-463B-BFE3-9311376A6809/NDP472-KB4054531-Web.exe',
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet48
  // https://dotnet.microsoft.com/download/dotnet-framework/net48
  if not IsDotNetInstalled(net48, 0) then begin
    AddDependency('dotnetfx48.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.8',
      'https://download.visualstudio.microsoft.com/download/pr/7afca223-55d2-470a-8edc-6a1739ae3252/c9b8749dd99fc0d4453b2a3e4c37ba16/ndp48-web.exe',
      '', False, False, False);
  end;
#endif

#ifdef UseNetCore31
  // https://dotnet.microsoft.com/download/dotnet-core/3.1
  if not IsNetCoreInstalled('Microsoft.NETCore.App 3.1.15') then begin
    AddDependency('netcore31' + GetArchitectureSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Core Runtime 3.1.15' + GetArchitectureTitle,
      GetString('https://download.visualstudio.microsoft.com/download/pr/35b3d0cb-0c1c-44c2-8134-bfae32e9aa2a/0754bccd12a07e746bce755f58a0e74d/dotnet-runtime-3.1.15-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/175dbe99-a914-4654-90b3-a80a2d2f03c8/f1609ae26bce5fbff4ac08e0476d1a7e/dotnet-runtime-3.1.15-win-x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseNetCore31Asp
  // https://dotnet.microsoft.com/download/dotnet-core/3.1
  if not IsNetCoreInstalled('Microsoft.AspNetCore.App 3.1.15') then begin
    AddDependency('netcore31asp' + GetArchitectureSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      'ASP.NET Core Runtime 3.1.15' + GetArchitectureTitle,
      GetString('https://download.visualstudio.microsoft.com/download/pr/223a50e4-a4e1-42dc-8f97-f38a8f7518c3/642dab7b5177097ee223156ce70a6c3c/aspnetcore-runtime-3.1.15-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/ae6e6b5b-5e7c-45f9-a668-cb1899f22e46/9c917acfab934ddd64340ba46490264e/aspnetcore-runtime-3.1.15-win-x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseNetCore31Desktop
  // https://dotnet.microsoft.com/download/dotnet-core/3.1
  if not IsNetCoreInstalled('Microsoft.WindowsDesktop.App 3.1.15') then begin
    AddDependency('netcore31desktop' + GetArchitectureSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Desktop Runtime 3.1.15' + GetArchitectureTitle,
      GetString('https://download.visualstudio.microsoft.com/download/pr/8b11972e-ded9-4d3c-9714-7c7ef047cca6/76dbe20bc03e69f5c0d005452ba88a9d/windowsdesktop-runtime-3.1.15-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/d30352fe-d4f3-4203-91b9-01a3b66a802e/bb416e6573fa278fec92113abefc58b3/windowsdesktop-runtime-3.1.15-win-x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet50
  // https://dotnet.microsoft.com/download/dotnet/5.0
  if not IsNetCoreInstalled('Microsoft.NETCore.App 5.0.6') then begin
    AddDependency('dotnet50' + GetArchitectureSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Runtime 5.0.6' + GetArchitectureTitle,
      GetString('https://download.visualstudio.microsoft.com/download/pr/67839ecf-8e05-411a-977b-ac9780e18279/76f413425112f3dd1d77d48f69a76f59/dotnet-runtime-5.0.6-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/c6c04d2c-d131-4de7-b97a-c29ceca9ee8e/5a654bdbc0a61c621d59be9601e041d6/dotnet-runtime-5.0.6-win-x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet50Asp
  // https://dotnet.microsoft.com/download/dotnet/5.0
  if not IsNetCoreInstalled('Microsoft.AspNetCore.App 5.0.6') then begin
    AddDependency('dotnet50asp' + GetArchitectureSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      'ASP.NET Core Runtime 5.0.6' + GetArchitectureTitle,
      GetString('https://download.visualstudio.microsoft.com/download/pr/61284da9-728b-485c-a9e0-dfd4455f773f/facdf8e9e1509ec4d6f40fce95ff68dd/aspnetcore-runtime-5.0.6-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/275d6b51-e594-4edc-8f2f-606351e137ae/8a9e3886344599059dad377739151e37/aspnetcore-runtime-5.0.6-win-x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseDotNet50Desktop
  // https://dotnet.microsoft.com/download/dotnet/5.0
  if not IsNetCoreInstalled('Microsoft.WindowsDesktop.App 5.0.6') then begin
    AddDependency('dotnet50desktop' + GetArchitectureSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Desktop Runtime 5.0.6' + GetArchitectureTitle,
      GetString('https://download.visualstudio.microsoft.com/download/pr/315854e8-6857-4d0d-b7e0-57761e3f7d12/b31193ac2c9f1674b66cf7a65c2521de/windowsdesktop-runtime-5.0.6-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/6279dc90-f437-4481-82a5-73dd9f97da06/6519ef44735fd31115b9b1a81d6ff1e8/windowsdesktop-runtime-5.0.6-win-x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseVC2005
  // https://www.microsoft.com/en-US/download/details.aspx?id=26347
  if not IsMsiProductInstalled(GetString('{86C9D5AA-F00C-4921-B3F2-C60AF92E2844}', '{A8D19029-8E5C-4E22-8011-48070F9E796E}'), PackVersionComponents(8, 0, 61000, 0)) then begin
    AddDependency('vcredist2005' + GetArchitectureSuffix + '.exe',
      '/q',
      'Visual C++ 2005 Service Pack 1 Redistributable' + GetArchitectureTitle,
      GetString('https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x86.EXE', 'https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x64.EXE'),
      '', False, False, False);
  end;
#endif

#ifdef UseVC2008
  // https://www.microsoft.com/en-US/download/details.aspx?id=26368
  if not IsMsiProductInstalled(GetString('{DE2C306F-A067-38EF-B86C-03DE4B0312F9}', '{FDA45DDF-8E17-336F-A3ED-356B7B7C688A}'), PackVersionComponents(9, 0, 30729, 6161)) then begin
    AddDependency('vcredist2008' + GetArchitectureSuffix + '.exe',
      '/q',
      'Visual C++ 2008 Service Pack 1 Redistributable' + GetArchitectureTitle,
      GetString('https://download.microsoft.com/download/5/D/8/5D8C65CB-C849-4025-8E95-C3966CAFD8AE/vcredist_x86.exe', 'https://download.microsoft.com/download/5/D/8/5D8C65CB-C849-4025-8E95-C3966CAFD8AE/vcredist_x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseVC2010
  // https://www.microsoft.com/en-US/download/details.aspx?id=26999
  if not IsMsiProductInstalled(GetString('{1F4F1D2A-D9DA-32CF-9909-48485DA06DD5}', '{5B75F761-BAC8-33BC-A381-464DDDD813A3}'), PackVersionComponents(10, 0, 40219, 0)) then begin
    AddDependency('vcredist2010' + GetArchitectureSuffix + '.exe',
      '/passive /norestart',
      'Visual C++ 2010 Service Pack 1 Redistributable' + GetArchitectureTitle,
      GetString('https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe', 'https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseVC2012
  // https://www.microsoft.com/en-US/download/details.aspx?id=30679
  if not IsMsiProductInstalled(GetString('{4121ED58-4BD9-3E7B-A8B5-9F8BAAE045B7}', '{EFA6AFA1-738E-3E00-8101-FD03B86B29D1}'), PackVersionComponents(11, 0, 61030, 0)) then begin
    AddDependency('vcredist2012' + GetArchitectureSuffix + '.exe',
      '/passive /norestart',
      'Visual C++ 2012 Update 4 Redistributable' + GetArchitectureTitle,
      GetString('https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe', 'https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseVC2013
  //ForceX86 := True; // force 32-bit install of next dependencies
  // https://support.microsoft.com/en-US/help/4032938
  if not IsMsiProductInstalled(GetString('{B59F5BF1-67C8-3802-8E59-2CE551A39FC5}', '{20400CF0-DE7C-327E-9AE4-F0F38D9085F8}'), PackVersionComponents(12, 0, 40664, 0)) then begin
    AddDependency('vcredist2013' + GetArchitectureSuffix + '.exe',
      '/passive /norestart',
      'Visual C++ 2013 Update 5 Redistributable' + GetArchitectureTitle,
      GetString('https://download.visualstudio.microsoft.com/download/pr/10912113/5da66ddebb0ad32ebd4b922fd82e8e25/vcredist_x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/10912041/cee5d6bca2ddbcd039da727bf4acb48a/vcredist_x64.exe'),
      '', False, False, False);
  end;
  //ForceX86 := False; // disable forced 32-bit install again
#endif

#ifdef UseVC2015To2019
  // https://support.microsoft.com/en-US/help/2977003/the-latest-supported-visual-c-downloads
  if not IsMsiProductInstalled(GetString('{65E5BD06-6392-3027-8C26-853107D3CF1A}', '{36F68A90-239C-34DF-B58C-64B30153CE35}'), PackVersionComponents(14, 29, 30037, 0)) then begin
    AddDependency('vcredist2019' + GetArchitectureSuffix + '.exe',
      '/passive /norestart',
      'Visual C++ 2015-2019 Redistributable' + GetArchitectureTitle,
      GetString('https://aka.ms/vs/16/release/vc_redist.x86.exe', 'https://aka.ms/vs/16/release/vc_redist.x64.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseDirectX
  // https://www.microsoft.com/en-US/download/details.aspx?id=35
  ExtractTemporaryFile('dxwebsetup.exe');
  AddDependency('dxwebsetup.exe',
    '/q',
    'DirectX Runtime',
    'https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe',
    '', True, False, False);
#endif

#ifdef UseSql2008Express
  // https://www.microsoft.com/en-US/download/details.aspx?id=30438
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(10, 50, 4000, 0)) < 0) then begin
    AddDependency('sql2008express' + GetArchitectureSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2008 R2 Service Pack 2 Express',
      GetString('https://download.microsoft.com/download/0/4/B/04BE03CD-EAF3-4797-9D8D-2E08E316C998/SQLEXPR32_x86_ENU.exe', 'https://download.microsoft.com/download/0/4/B/04BE03CD-EAF3-4797-9D8D-2E08E316C998/SQLEXPR_x64_ENU.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseSql2012Express
  // https://www.microsoft.com/en-US/download/details.aspx?id=56042
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(11, 0, 7001, 0)) < 0) then begin
    AddDependency('sql2012express' + GetArchitectureSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2012 Service Pack 4 Express',
      GetString('https://download.microsoft.com/download/B/D/E/BDE8FAD6-33E5-44F6-B714-348F73E602B6/SQLEXPR32_x86_ENU.exe', 'https://download.microsoft.com/download/B/D/E/BDE8FAD6-33E5-44F6-B714-348F73E602B6/SQLEXPR_x64_ENU.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseSql2014Express
  // https://www.microsoft.com/en-US/download/details.aspx?id=57473
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL12.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(12, 0, 6024, 0)) < 0) then begin
    AddDependency('sql2014express' + GetArchitectureSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2014 Service Pack 3 Express',
      GetString('https://download.microsoft.com/download/3/9/F/39F968FA-DEBB-4960-8F9E-0E7BB3035959/SQLEXPR32_x86_ENU.exe', 'https://download.microsoft.com/download/3/9/F/39F968FA-DEBB-4960-8F9E-0E7BB3035959/SQLEXPR_x64_ENU.exe'),
      '', False, False, False);
  end;
#endif

#ifdef UseSql2016Express
  // https://www.microsoft.com/en-US/download/details.aspx?id=56840
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL13.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(13, 0, 5026, 0)) < 0) then begin
    AddDependency('sql2016express' + GetArchitectureSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2016 Service Pack 2 Express',
      'https://download.microsoft.com/download/3/7/6/3767D272-76A1-4F31-8849-260BD37924E4/SQLServer2016-SSEI-Expr.exe',
      '', False, False, False);
  end;
#endif

#ifdef UseSql2017Express
  // https://www.microsoft.com/en-US/download/details.aspx?id=55994
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL14.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(14, 0, 0, 0)) < 0) then begin
    AddDependency('sql2017express' + GetArchitectureSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2017 Express',
      'https://download.microsoft.com/download/5/E/9/5E9B18CC-8FD5-467E-B5BF-BADE39C51F73/SQLServer2017-SSEI-Expr.exe',
      '', False, False, False);
  end;
#endif

#ifdef UseSql2019Express
  // https://www.microsoft.com/en-US/download/details.aspx?id=101064
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(15, 0, 0, 0)) < 0) then begin
    AddDependency('sql2019express' + GetArchitectureSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2019 Express',
      'https://download.microsoft.com/download/7/f/8/7f8a9c43-8c8a-4f7c-9f92-83c18d96b681/SQL2019-SSEI-Expr.exe',
      '', False, False, False);
  end;
#endif

  Result := True;
end;