param([string]$GameDirectory)
. (Join-Path $PSScriptRoot 'Common.ps1')
Assert-CheckpointGameClosed
$game=Resolve-CheckpointGame $GameDirectory
$managed=Join-Path $game 'GettingOverIt_Data\Managed'
$target=Join-Path $managed 'Assembly-CSharp.dll'
$modTarget=Join-Path $managed 'QuickCheckpoint.dll'
$storage=Join-Path $game 'QuickCheckpointMod'
$stage=Join-Path $storage 'staging'
New-Item -ItemType Directory -Path $stage -Force | Out-Null
& (Join-Path $PSScriptRoot 'Build.ps1') -ManagedDirectory $managed -OutputDirectory $stage
$tool=Join-Path $stage 'PatchTool.exe'
$kind=Invoke-CheckpointPatch $tool @('inspect',$target)
$manifestPath=Join-Path $storage 'install.json'
$backup=Join-Path $storage 'Assembly-CSharp.original.dll'
$currentHash=(Get-FileHash -LiteralPath $target).Hash
$reuse=$false
if(Test-Path -LiteralPath $manifestPath) {
 $previous=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
 if($kind -eq 'PATCHED' -and $previous.PatchedHash -eq $currentHash -and (Test-Path -LiteralPath $backup) -and (Get-FileHash -LiteralPath $backup).Hash -eq $previous.OriginalHash){$reuse=$true}
}
Invoke-CheckpointPatch $tool @('patch',$target,(Join-Path $stage 'QuickCheckpoint.dll'),$managed,(Join-Path $stage 'Assembly-CSharp.patched.dll')) | Out-Host
if(!$reuse) {
 $cleanStage=Join-Path $stage 'Assembly-CSharp.original.dll'
 if($kind -eq 'CLEAN'){Copy-Item -LiteralPath $target -Destination $cleanStage -Force}
 else {Invoke-CheckpointPatch $tool @('clean',$target,$cleanStage) | Out-Host}
 if(Test-Path -LiteralPath $backup){Copy-Item -LiteralPath $backup -Destination (Join-Path $storage ('Assembly-CSharp.previous-'+[guid]::NewGuid().ToString('N')+'.dll'))}
 Copy-Item -LiteralPath $cleanStage -Destination $backup -Force
}
$rollbackGame=Join-Path $stage 'rollback-game.dll'
$rollbackMod=Join-Path $stage 'rollback-mod.dll'
Copy-Item -LiteralPath $target -Destination $rollbackGame -Force
$hadMod=Test-Path -LiteralPath $modTarget
if($hadMod){Copy-Item -LiteralPath $modTarget -Destination $rollbackMod -Force}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
if(Test-Path -LiteralPath 'HKCU:\Software\Bennett Foddy\Getting Over It') {
 & reg.exe export 'HKCU\Software\Bennett Foddy\Getting Over It' (Join-Path $storage "progress-$stamp.reg") /y | Out-Null
 if($LASTEXITCODE -ne 0){throw 'Progress backup failed; installation stopped.'}
}
try {
 Copy-Item -LiteralPath (Join-Path $stage 'QuickCheckpoint.dll') -Destination $modTarget -Force
 Copy-Item -LiteralPath (Join-Path $stage 'Assembly-CSharp.patched.dll') -Destination $target -Force
 $patchedHash=(Get-FileHash -LiteralPath (Join-Path $stage 'Assembly-CSharp.patched.dll')).Hash
 $modHash=(Get-FileHash -LiteralPath (Join-Path $stage 'QuickCheckpoint.dll')).Hash
 if((Get-FileHash -LiteralPath $target).Hash -ne $patchedHash -or (Get-FileHash -LiteralPath $modTarget).Hash -ne $modHash){throw 'Installed hash verification failed.'}
 $metadata=@{Version='1.0';GameDirectory=$game;OriginalHash=(Get-FileHash -LiteralPath $backup).Hash;PatchedHash=$patchedHash;ModHash=$modHash;InstalledAt=(Get-Date).ToString('o')}
 $metadata | ConvertTo-Json | Set-Content -LiteralPath ($manifestPath+'.tmp') -Encoding UTF8
 Move-Item -LiteralPath ($manifestPath+'.tmp') -Destination $manifestPath -Force
} catch {
 Copy-Item -LiteralPath $rollbackGame -Destination $target -Force
 if($hadMod){Copy-Item -LiteralPath $rollbackMod -Destination $modTarget -Force} elseif(Test-Path -LiteralPath $modTarget){Remove-Item -LiteralPath $modTarget}
 throw
}
Write-Host "Installed QuickCheckpoint v1.0 into: $game" -ForegroundColor Green
Write-Host 'First launch: choose language | F5 twice: Save | F9: Load | F10: Settings'
Write-Host 'Existing checkpoints kept. Start the game normally.'


