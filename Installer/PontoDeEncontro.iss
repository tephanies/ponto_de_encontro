#define MyAppName "PontoDeEncontro"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "PontoDeEncontro"
#define MyAppExeName "PontoDeEncontro.exe"
#define MyDirectExeName "PontoDeEncontroDireto.exe"
#define MyPublishDir "..\\publish\\win-x64"

[Setup]
AppId={{A53A933F-8F88-4E86-BCCF-ECAABFB64A17}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..
OutputBaseFilename=PontoDeEncontro-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Languages]
Name: "portuguesebrazil"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na area de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "desktopicondirect"; Description: "Criar atalho direto para a tela de ponto de encontro"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName} - Monitor"; Filename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\{#MyAppName} - Tela Direta"; Filename: "{app}\{#MyDirectExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{autodesktop}\{#MyAppName} - Tela Direta"; Filename: "{app}\{#MyDirectExeName}"; Tasks: desktopicondirect

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir configurador de conexao"; Flags: nowait postinstall skipifsilent
