param([Parameter(Mandatory=$true)][string]$ManagedDirectory,[Parameter(Mandatory=$true)][string]$TestDirectory)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path $TestDirectory -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo (('/out:')+(Join-Path $TestDirectory 'SettingsTests.exe')) (Join-Path $project 'src\CheckpointSettings.cs') (Join-Path $project 'src\CheckpointLocalization.cs') (Join-Path $project 'src\CheckpointInputPolicy.cs') (Join-Path $PSScriptRoot 'SettingsTests.cs')
if($LASTEXITCODE -ne 0){throw 'Settings test build failed'}
& (Join-Path $TestDirectory 'SettingsTests.exe') $TestDirectory
if($LASTEXITCODE -ne 0){throw 'Settings tests failed'}
$fixture=Join-Path $TestDirectory 'fixture'
$managed=Join-Path $fixture 'GettingOverIt_Data\Managed'
New-Item -ItemType Directory -Path $managed -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $fixture 'GettingOverIt.exe'),'fixture only - not runnable')
foreach($name in @('Assembly-CSharp.dll','UnityEngine.CoreModule.dll','UnityEngine.InputLegacyModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.Physics2DModule.dll','UnityEngine.TextRenderingModule.dll')) {Copy-Item -LiteralPath (Join-Path $ManagedDirectory $name) -Destination (Join-Path $managed $name) -Force}
& (Join-Path $project 'Install.ps1') -GameDirectory $fixture
$backup=Join-Path $fixture 'QuickCheckpointMod\Assembly-CSharp.original.dll'
$firstHash=(Get-FileHash -LiteralPath $backup).Hash
& (Join-Path $project 'Install.ps1') -GameDirectory $fixture
if((Get-FileHash -LiteralPath $backup).Hash -ne $firstHash){throw 'Repeated install changed backup'}
Write-Host 'PASS upgrade/reinstall preserves backup'
$target=Join-Path $managed 'Assembly-CSharp.dll'
$patchedBytes=[IO.File]::ReadAllBytes($target)
[IO.File]::AppendAllText($target,'changed-after-install')
$rejected=$false
try {& (Join-Path $project 'Uninstall.ps1') -GameDirectory $fixture} catch {$rejected=$true}
if(!$rejected){throw 'Uninstall should reject changed game'}
if((Get-FileHash -LiteralPath $target).Hash -eq $firstHash){throw 'Uninstall replaced changed game'}
Write-Host 'PASS uninstall refuses game files changed by updates or other mods'
[IO.File]::WriteAllBytes($target,$patchedBytes)
& (Join-Path $project 'Uninstall.ps1') -GameDirectory $fixture
if((Get-FileHash -LiteralPath $target).Hash -ne $firstHash){throw 'Original restore mismatch'}
if(Test-Path -LiteralPath (Join-Path $managed 'QuickCheckpoint.dll')){throw 'Mod dll still installed'}
$tool=Join-Path $fixture 'QuickCheckpointMod\staging\PatchTool.exe'
$result=& $tool inspect $target
if($LASTEXITCODE -ne 0 -or $result -ne 'CLEAN'){throw 'Restored game is not clean'}
Write-Host 'PASS uninstall restores a game without checkpoint hook'
@{Settings='PASS';InstallUpgradeUninstall='PASS';ChangedGameProtection='PASS';ExecutedAt=(Get-Date).ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $TestDirectory 'test-results.json') -Encoding UTF8

