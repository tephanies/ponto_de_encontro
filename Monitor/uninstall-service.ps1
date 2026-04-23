param(
    [string] $ServiceName = 'PontoMonitor',
    [string] $TargetFolder = 'C:\Services\PontoMonitor'
)

Write-Host "Stopping and removing service $ServiceName if it exists..."
if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    sc stop $ServiceName | Out-Null
    Start-Sleep -Seconds 1
    sc delete $ServiceName | Out-Null
    Write-Host "Service $ServiceName removed."
} else {
    Write-Host "Service $ServiceName not found."
}

if (Test-Path $TargetFolder) {
    Write-Host "Removing folder $TargetFolder"
    Remove-Item -Recurse -Force $TargetFolder
}

Write-Host "Uninstall complete."
