param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot ("publish\" + $Runtime)
$installerScript = Join-Path $projectRoot "Installer\PontoDeEncontro.iss"
$mainExe = Join-Path $publishDir "PontoDeEncontro.exe"
$directExe = Join-Path $publishDir "PontoDeEncontroDireto.exe"
$configExe = Join-Path $publishDir "TrilobitConfiguracao.exe"

Write-Host "Publicando aplicacao self-contained..." -ForegroundColor Cyan
dotnet publish (Join-Path $projectRoot "PontoDeEncontro.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=false `
    /p:PublishReadyToRun=true `
    -o $publishDir

if (Test-Path $mainExe)
{
    # Copiar executaveis
    Copy-Item $mainExe $directExe -Force
    Copy-Item $mainExe $configExe -Force

    # Copiar deps.json e runtimeconfig.json para cada exe auxiliar
    $depsJson = Join-Path $publishDir "PontoDeEncontro.deps.json"
    $runtimeConfig = Join-Path $publishDir "PontoDeEncontro.runtimeconfig.json"

    Copy-Item $depsJson (Join-Path $publishDir "PontoDeEncontroDireto.deps.json") -Force
    Copy-Item $runtimeConfig (Join-Path $publishDir "PontoDeEncontroDireto.runtimeconfig.json") -Force
    Copy-Item $depsJson (Join-Path $publishDir "TrilobitConfiguracao.deps.json") -Force
    Copy-Item $runtimeConfig (Join-Path $publishDir "TrilobitConfiguracao.runtimeconfig.json") -Force

    Write-Host "Executaveis auxiliares criados com sucesso." -ForegroundColor Green
}

$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if (-not (Test-Path $isccPath))
{
    Write-Warning "Inno Setup nao encontrado. Instale o Inno Setup e execute novamente para gerar o instalador."
    Write-Host "Aplicacao publicada em: $publishDir" -ForegroundColor Yellow
    exit 0
}

Write-Host "Gerando instalador..." -ForegroundColor Cyan
& $isccPath $installerScript
Write-Host "Instalador gerado com sucesso." -ForegroundColor Green
