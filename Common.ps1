$ErrorActionPreference='Stop'
function Resolve-CheckpointGame([string]$Directory) {
 if($Directory) {
  $Directory=$Directory.Trim().Trim('"')
  if(Test-Path -LiteralPath $Directory -PathType Leaf){$Directory=Split-Path -Parent $Directory}
  if(!(Test-Path -LiteralPath (Join-Path $Directory 'GettingOverIt.exe')) -or !(Test-Path -LiteralPath (Join-Path $Directory 'GettingOverIt_Data\Managed\Assembly-CSharp.dll'))){throw 'Select the folder containing GettingOverIt.exe (a Windows Mono build).'}
  return (Resolve-Path -LiteralPath $Directory).Path
 }
 $roots=@()
 foreach($registry in @('HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam')) {
  $entry=Get-ItemProperty -LiteralPath $registry -ErrorAction SilentlyContinue
  foreach($value in @($entry.SteamPath,$entry.InstallPath)){if($value){$roots+=$value}}
 }
 foreach($root in @($roots)) {
  $libraries=Join-Path $root 'steamapps\libraryfolders.vdf'
  if(Test-Path -LiteralPath $libraries){foreach($match in [regex]::Matches((Get-Content -LiteralPath $libraries -Raw),'"path"\s+"([^"]+)"')){$roots+=$match.Groups[1].Value.Replace('\\','\')}}
 }
 $found=@()
 foreach($root in ($roots | Select-Object -Unique)) {
  $manifest=Join-Path $root 'steamapps\appmanifest_240720.acf'
  if(Test-Path -LiteralPath $manifest){$match=[regex]::Match((Get-Content -LiteralPath $manifest -Raw),'"installdir"\s+"([^"]+)"'); if($match.Success){$candidate=Join-Path $root ('steamapps\common\'+$match.Groups[1].Value); if(Test-Path -LiteralPath (Join-Path $candidate 'GettingOverIt.exe')){$found+=$candidate}}}
 }
 if($found.Count -eq 1){return Resolve-CheckpointGame $found[0]}
 Add-Type -AssemblyName System.Windows.Forms
 $dialog=New-Object System.Windows.Forms.FolderBrowserDialog
 $dialog.Description='Select Getting Over It folder (contains GettingOverIt.exe)'; $dialog.ShowNewFolderButton=$false
 if($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK){throw 'Folder selection cancelled.'}
 return Resolve-CheckpointGame $dialog.SelectedPath
}
function Invoke-CheckpointPatch([string]$Tool,[string[]]$Arguments) {
 $result=& $Tool @Arguments
 if($LASTEXITCODE -ne 0){throw 'Assembly patch validation failed; game files have not been accepted.'}
 return $result
}
function Assert-CheckpointGameClosed {
 if(Get-Process GettingOverIt -ErrorAction SilentlyContinue){throw 'Please exit Getting Over It normally, then run this again.'}
}
