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
    Copy-Item $mainExe $directExe -Force
    Write-Host "Executavel direto criado em: $directExe" -ForegroundColor Green
}

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc)
{
    Write-Warning "Inno Setup nao encontrado. Instale o Inno Setup e execute novamente para gerar o instalador."
    Write-Host "Aplicacao publicada em: $publishDir" -ForegroundColor Yellow
    exit 0
}

Write-Host "Gerando instalador..." -ForegroundColor Cyan
& $iscc.Source $installerScript
Write-Host "Instalador gerado com sucesso." -ForegroundColor Green
