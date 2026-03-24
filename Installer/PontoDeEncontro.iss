#define MyAppName "Trilobit - Ponto de encontro"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Trilobit"
#define MyAppExeName "PontoDeEncontro.exe"
#define MyDirectExeName "PontoDeEncontroDireto.exe"
#define MyConfigExeName "TrilobitConfiguracao.exe"
#define MyPublishDir "..\\publish\\win-x64"

[Setup]
AppId={{A53A933F-8F88-4E86-BCCF-ECAABFB64A17}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName=C:\Trilobit\PontoDeEncontro
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..
OutputBaseFilename=Trilobit-PontoDeEncontro-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Languages]
Name: "portuguesebrazil"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na area de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "desktopiconconfig"; Description: "Criar atalho para configuracao do banco"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName} - Monitor"; Filename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyDirectExeName}"
Name: "{autoprograms}\{#MyAppName} - Configuracao do banco"; Filename: "{app}\{#MyConfigExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyDirectExeName}"; Tasks: desktopicon
Name: "{autodesktop}\{#MyAppName} - Configuracao do banco"; Filename: "{app}\{#MyConfigExeName}"; Tasks: desktopiconconfig

[Run]
Filename: "{app}\{#MyConfigExeName}"; Description: "Abrir configuracao do banco"; Flags: nowait postinstall skipifsilent
