# Getting Over It · QuickCheckpoint 2.1

[English](README.en.md) · [下载最新安装包](https://github.com/Xiaowai2233/getting-over-it-quickcheckpoint/releases/latest) · [反馈问题](https://github.com/Xiaowai2233/getting-over-it-quickcheckpoint/issues)

**普通玩家：请从 Releases 下载 `GettingOverIt-QuickCheckpoint-v2.1.0.zip`，解压后双击 `Install.cmd`。**

Windows 版《Getting Over It》快捷存档模组。按键保存检查点、即时回档，并提供游戏内确认窗口和设置。

## 安装

1. 正常退出游戏。
2. 把整个压缩包解压到一个文件夹。不要直接从 ZIP 中运行。
3. 双击 `Install.cmd`。安装器自动查找 Steam 的游戏目录；找不到或有多个安装目录时，会弹出目录选择窗口。选择包含 `GettingOverIt.exe` 的文件夹。
4. 看到 `Installed QuickCheckpoint v2.1.0` 后，正常启动游戏并继续游玩。首次进入游戏时会暂停并弹出语言选择窗口；选择后点击“继续”。

不需要额外安装 Mod 加载器、Visual Studio 或 .NET SDK。安装器使用 Windows 自带的 .NET Framework 编译器，按玩家安装的 Unity 程序集编译模组，再给游戏的保存组件添加调用入口。整个安装包可离线使用。

## 使用

| 默认按键 | 功能 |
| --- | --- |
| F5 | 捕获当前位置，弹出确认存档窗口 |
| F9 | 回到最后确认保存的检查点 |
| F10 | 打开或关闭设置窗口 |

确认窗口打开后游戏暂停。点击“确认存档”或**松开后再按一次存档键**，保存**第一次按下快捷键时的位置**；点击“取消”不覆盖已有检查点。长按不会自动确认。建议站稳后保存。

在 F10 设置窗口中可以：

- 关闭或开启存档确认。
- 隐藏或显示左上角常驻状态条；隐藏后仍会出现几秒钟的操作结果提示。
- 点击存档/读档按键按钮，然后按新按键。存档和读档不能使用相同按键，F10 和 Esc 保留给窗口操作。只支持单个键盘按键，不支持组合键和鼠标按键。
- 点击“语言”更改界面语言，恢复默认设置不会清除语言选择。

点击“保存并关闭”后设置写入磁盘，下次启动继续生效。F10 始终可用于打开设置，即使状态条已隐藏。

## 多语言与首次启动

支持 18 种语言：英语、简体中文、繁体中文、法语、西班牙语、葡萄牙语、德语、意大利语、荷兰语、波兰语、俄语、乌克兰语、土耳其语、日语、韩语、印度尼西亚语、越南语、阿拉伯语。

第一次进入游戏会显示语言选择弹窗。按钮用各语言的本地名称标注；点击语言预览，再点击“继续”。选择会持久保存，以后不重复弹出。首次从 2.0 升级时也会询问一次，但保留旧按键、存档和状态条设置。F10 设置中的“语言”按钮可重新打开选择窗口。

界面选择合适的 Windows 字体，并为长文本使用可滚动窗口。阿拉伯语使用游戏自带 ArabicSupport.dll 进行连接字形和从右到左排版；未带此模块的其他游戏构建无法提供同等阿拉伯文显示。各语言文本已经过结构与占位符检查，未逐一进行母语审校。

## 存档和备份

保持单一检查点，兼容旧版工具创建的 `QuickCheckpoint.xml`。重复存档时，上一份保存为 `.bak`。

通常位于：

```
%USERPROFILE%\AppData\LocalLow\Bennett Foddy\Getting Over It\
  QuickCheckpoint.xml
  QuickCheckpoint.xml.bak
  QuickCheckpoint.settings.xml
```

实际位置使用 Unity 的 `Application.persistentDataPath`。回档会让游戏自身的自动存档记录恢复后的位置。

安装备份位于游戏目录的 `QuickCheckpointMod` 文件夹，包含原程序集、安装记录和安装当时的注册表进度备份。这些是玩家本机私有文件，不要随分发包分享。

## 升级、卸载和游戏更新

- 升级旧版工具：退出游戏后运行本版 `Install.cmd`，已有检查点保留。
- 卸载：退出游戏后运行**本版** `Uninstall.cmd`。存档、设置和备份保留。
- Steam 更新或验证文件可能移除模组，之后需要重新运行安装器。
- 卸载器会检查程序集哈希。如果安装后游戏文件又被修改，它会停止，避免用旧文件覆盖新的游戏版本。
- 安装器会检查原生保存接口和相关字段。缺少这些接口的版本会拒绝安装。对其他 Mod 的兼容性未做全面验证。

目前面向 Windows Mono 构建，已在本机游戏 1.7 上编译并测试安装流程。不是 Android、iOS、macOS 或 IL2CPP 安装包；其他游戏版本需要保留相同保存接口和 Unity 模块。

## 开发项目

```
src/QuickCheckpoint.cs      游戏内窗口、快捷键、原生回档逻辑
src/CheckpointSettings.cs   设置与文件写入
src/CheckpointLocalization.cs 18 种语言资源
src/CheckpointInputPolicy.cs 二次按键确认规则
src/PatchTool.cs            程序集接口检查、挂钩与移除
tools/Mono.Cecil.dll        开源程序集编辑依赖
Build.ps1                  针对目标游戏编译
Install.ps1 / Common.ps1    发现游戏、备份、安装
Uninstall.ps1              校验并卸载
tests/                     设置持久化和安装生命周期测试
```

开发编译示例：

```powershell
.\Build.ps1 -ManagedDirectory 'X:\SteamLibrary\steamapps\common\Getting Over It\GettingOverIt_Data\Managed'
```

运行自动测试（必须退出游戏）：

```powershell
.\tests\Test.ps1 -ManagedDirectory 'X:\SteamLibrary\steamapps\common\Getting Over It\GettingOverIt_Data\Managed' -TestDirectory 'D:\Temp\QuickCheckpoint-tests'
```

测试使用目标游戏程序集的独立副本，验证设置持久化、写入失败不破坏存档、备份、升级和卸载。它不会代替实际游戏内的窗口和物理回档验证。

分享时直接发送本项目压缩包即可；不包含游戏程序集、玩家存档、路径配置或 Steam 账号信息。包根目录的空白文件“安装请双击 Install.cmd.txt”用于提示安装入口。源码采用 MIT 许可；Mono.Cecil 的 MIT 许可见 `tools/Mono.Cecil-LICENSE.txt`。


