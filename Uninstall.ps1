param([string]$GameDirectory)
. (Join-Path $PSScriptRoot 'Common.ps1')
Assert-CheckpointGameClosed
$game=Resolve-CheckpointGame $GameDirectory
$managed=Join-Path $game 'GettingOverIt_Data\Managed'
$storage=Join-Path $game 'QuickCheckpointMod'
$metadata=Get-Content -LiteralPath (Join-Path $storage 'install.json') -Raw | ConvertFrom-Json
$target=Join-Path $managed 'Assembly-CSharp.dll'
$backup=Join-Path $storage 'Assembly-CSharp.original.dll'
if((Get-FileHash -LiteralPath $target).Hash -ne $metadata.PatchedHash){throw 'Game files changed after installation. Uninstall stopped to avoid restoring an old game version. Use Steam Verify Files, or reinstall this mod before uninstalling.'}
if((Get-FileHash -LiteralPath $backup).Hash -ne $metadata.OriginalHash){throw 'Backup hash mismatch. Uninstall stopped.'}
Copy-Item -LiteralPath $backup -Destination $target -Force
if((Get-FileHash -LiteralPath $target).Hash -ne $metadata.OriginalHash){throw 'Restore verification failed.'}
$mod=Join-Path $managed 'QuickCheckpoint.dll'
if((Test-Path -LiteralPath $mod) -and (Get-FileHash -LiteralPath $mod).Hash -eq $metadata.ModHash){Remove-Item -LiteralPath $mod}
Write-Host 'Mod uninstalled. Checkpoints, settings and progress backups kept.' -ForegroundColor Green
