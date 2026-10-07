param([Parameter(Mandatory=$true)][string]$ManagedDirectory,[string]$OutputDirectory=(Join-Path $PSScriptRoot 'build'))
$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(!(Test-Path -LiteralPath $compiler)){ $compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if(!(Test-Path -LiteralPath $compiler)){ throw 'Windows .NET Framework C# compiler not found.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$references=@('UnityEngine.CoreModule.dll','UnityEngine.InputLegacyModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.Physics2DModule.dll','UnityEngine.TextRenderingModule.dll')
$arguments=@('/nologo','/target:library',('/out:'+ (Join-Path $OutputDirectory 'QuickCheckpoint.dll')))
foreach($reference in $references){ $path=Join-Path $ManagedDirectory $reference; if(!(Test-Path -LiteralPath $path)){throw "Unsupported Unity build: missing $reference"}; $arguments+=('/reference:'+ $path) }
$arguments+=@((Join-Path $PSScriptRoot 'src\QuickCheckpoint.cs'),(Join-Path $PSScriptRoot 'src\CheckpointSettings.cs'),(Join-Path $PSScriptRoot 'src\CheckpointLocalization.cs'),(Join-Path $PSScriptRoot 'src\CheckpointInputPolicy.cs'))
& $compiler @arguments
if($LASTEXITCODE -ne 0){throw 'Mod compilation failed.'}
& $compiler /nologo (('/out:')+(Join-Path $OutputDirectory 'PatchTool.exe')) (('/reference:')+(Join-Path $PSScriptRoot 'tools\Mono.Cecil.dll')) (Join-Path $PSScriptRoot 'src\PatchTool.cs')
if($LASTEXITCODE -ne 0){throw 'Patcher compilation failed.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tools\Mono.Cecil.dll') -Destination (Join-Path $OutputDirectory 'Mono.Cecil.dll') -Force

