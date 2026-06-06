# 开发和数据说明

## 状态判断来源

程序优先根据本地会话记录自动判断状态：

- Codex 本地会话记录。
- Claude Code hooks 写入的状态事件。

如果本地会话记录不可用，程序会退回到 CPU 活动和前台窗口判断。

## 数据存放位置

程序会在 exe 所在目录下自动创建：

```text
data\status.json          当前状态
data\settings.json        皮肤、指示灯方向、颜色、声音、通知配置、窗口位置
data\claude-events.jsonl  Claude Code CLI hooks 状态事件
logs\agent-status-light.log
```

`data\settings.json` 会保存 Bark 设备 Key、PushPlus Token、Telegram Bot Token / Chat ID 等通知凭据；不要把 `data\` 目录公开分享或提交到 Git。

请把 exe 放在能写文件的位置，例如桌面、文档或个人工具目录。不要放到 `C:\Program Files\` 等需要管理员权限的目录。

## 从源码编译

需要 Windows 自带的 .NET Framework 4，无需安装 Visual Studio。

```powershell
.\build_exe.bat                 # 一键编译，产物 dist\AgentStatusLight.exe
.\scripts\Build.ps1            # 编译，产物 dist\AgentStatusLight.exe
.\scripts\Build.ps1 -Package   # 编译并打包成 dist\AgentStatusLight.zip
```

## 目录结构

```text
src\       源码（入口、UI、Services、Models 等）
assets\    图标等应用资源
scripts\   编译/打包脚本（Build.ps1）
dist\      编译产物（自动生成；运行后在 exe 同目录下生成 data\ 和 logs\）
docs\      技术文档
build_exe.bat  一键编译入口
```
