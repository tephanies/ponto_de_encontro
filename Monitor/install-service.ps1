param(
    [Parameter(Mandatory=$true)] [string] $ConnectionString,
    [Parameter(Mandatory=$true)] [string] $DirectExePath,
    [string] $PublishConfiguration = 'Release',
    [string] $TargetFolder = 'C:\Services\PontoMonitor',
    [string] $ServiceName = 'PontoMonitor',
    [string] $DisplayName = 'Ponto Monitor',
    [string] $ServiceUser = '',
    [string] $ServicePassword = '',
    [int] $PollSeconds = 3,
    [int] $BatchSize = 50
)

Write-Host "Publishing Monitor project (Configuration=$PublishConfiguration)..."
$proj = Join-Path $PSScriptRoot 'Monitor.csproj'
if (-not (Test-Path $proj)) { $proj = Join-Path $PSScriptRoot 'Monitor\Monitor.csproj' }
if (-not (Test-Path $proj)) { Write-Error "Monitor.csproj not found in script folder."; exit 1 }

$publishDir = Join-Path $env:TEMP "ponto-monitor-publish"
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $publishDir
dotnet publish $proj -c $PublishConfiguration -r win-x64 --self-contained false -o $publishDir
if ($LASTEXITCODE -ne 0) { Write-Error 'dotnet publish failed'; exit 2 }

Write-Host "Copying published files to $TargetFolder..."
if (Test-Path $TargetFolder) { Remove-Item -Recurse -Force $TargetFolder }
New-Item -ItemType Directory -Path $TargetFolder | Out-Null
Copy-Item -Path (Join-Path $publishDir '*') -Destination $TargetFolder -Recurse

Write-Host "Preparing service..."
if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Service $ServiceName already exists — stopping and removing..."
    sc stop $ServiceName | Out-Null
    Start-Sleep -Seconds 1
    sc delete $ServiceName | Out-Null
}

$exePath = Join-Path $TargetFolder 'Monitor.exe'
if (-not (Test-Path $exePath)) { Write-Error "Monitor.exe not found in $TargetFolder"; exit 3 }

# Build binPath with quoted args
$escapedConn = $ConnectionString.Replace('"','\"')
$binPath = "\"$exePath\" \"$escapedConn\" \"$DirectExePath\" $PollSeconds $BatchSize"

Write-Host "Creating service $ServiceName..."
if ([string]::IsNullOrEmpty($ServiceUser)) {
    sc create $ServiceName binPath= "$binPath" start= auto DisplayName= "$DisplayName" | Out-Null
} else {
    sc create $ServiceName binPath= "$binPath" start= auto DisplayName= "$DisplayName" obj= "$ServiceUser" password= "$ServicePassword" | Out-Null
}

Write-Host "Service created. Starting $ServiceName..."
sc start $ServiceName | Out-Null

Write-Host "Installation complete. Service: $ServiceName — TargetFolder: $TargetFolder"
