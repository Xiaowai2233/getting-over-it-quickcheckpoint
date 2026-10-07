# QuickCheckpoint 2.1 — Getting Over It

[简体中文](README.md) · [Download latest release](https://github.com/Xiaowai2233/getting-over-it-quickcheckpoint/releases/latest) · [Report an issue](https://github.com/Xiaowai2233/getting-over-it-quickcheckpoint/issues)

**Players: download `GettingOverIt-QuickCheckpoint-v2.1.0.zip` from Releases, extract it, and double-click `Install.cmd`.**

A Windows Mono mod with one persistent checkpoint, instant restore, save confirmation, an optional status bar and configurable keyboard shortcuts.

## Install

Exit the game, extract the entire ZIP, and double-click **Install.cmd**. Steam libraries are detected automatically. If detection fails, select the folder containing GettingOverIt.exe. Start the game normally afterward. On first entry to gameplay, choose a language and click Continue. This choice is saved; upgrades from 2.0 ask once while retaining existing settings.

No mod loader, Visual Studio, .NET SDK or internet connection is required. The installer compiles against your game's Unity assemblies using the .NET Framework C# compiler included with Windows. It validates the native save API before patching. Distribution does not include game binaries or player saves.

## Controls

- **F5:** capture a checkpoint and show a confirmation dialog. The game pauses; Click Save or release and press your configured save key again to commit the state captured on the FIRST press. Holding the key does not confirm. Cancel retains your previous checkpoint.
- **F9:** restore the checkpoint immediately.
- **F10:** open/close settings. Toggle save confirmation and the persistent status bar, rebind the save/load keys, or use the Language button to change UI language. Short operation notifications remain visible when the bar is hidden.

Keyboard bindings are single keys. F10 and Escape are reserved; save/load cannot use the same key. Settings persist after closing the settings window. Checkpoints persist across game restarts. Save while stationary when possible.

## Languages

18 languages: English, Simplified Chinese, Traditional Chinese, French, Spanish, Portuguese, German, Italian, Dutch, Polish, Russian, Ukrainian, Turkish, Japanese, Korean, Indonesian, Vietnamese and Arabic. Native-name buttons make the initial language picker recognizable without knowing English. Language can be changed later with F10, and resetting defaults retains the chosen language.

Fonts vary by script. Arabic shaping and right-to-left text reuse the game's ArabicSupport.dll; builds missing this module cannot provide equivalent Arabic rendering. Translation completeness and format placeholders are tested; native-speaker review of every language has not been performed.

## Backup / uninstall

Run this version's **Uninstall.cmd** with the game closed. Checkpoints and settings are retained. Local game/registry backups are stored under the game's QuickCheckpointMod directory. Do not redistribute that directory.

Files normally live under `%USERPROFILE%\AppData\LocalLow\Bennett Foddy\Getting Over It`: QuickCheckpoint.xml, its .bak, and QuickCheckpoint.settings.xml. This version retains existing checkpoints from v1.

Steam updates or Verify Files can remove the hook. Reinstall afterward. The uninstaller refuses to overwrite game assemblies changed after installation. Windows Mono only; no IL2CPP/mobile/macOS support. Built against local version 1.7; other builds must retain the same native save API and Unity modules. Compatibility with unrelated mods is not comprehensively tested.

## Source / build

See src/, Build.ps1, and tests/. Build with:

```powershell
.\Build.ps1 -ManagedDirectory 'X:\SteamLibrary\steamapps\common\Getting Over It\GettingOverIt_Data\Managed'
```

Lifecycle tests use copies of game assemblies; set TestDirectory to a scratch directory:

```powershell
.\tests\Test.ps1 -ManagedDirectory 'X:\SteamLibrary\steamapps\common\Getting Over It\GettingOverIt_Data\Managed' -TestDirectory 'D:\Temp\QuickCheckpoint-tests'
```

Tests cover configuration roundtrips, interrupted writes, backups, legacy upgrade/reinstall and protected uninstall. In-game GUI and physics need gameplay testing.

Project license: MIT. Bundled Mono.Cecil 0.11.6 is MIT licensed; see tools/Mono.Cecil-LICENSE.txt and https://github.com/jbevain/cecil.


