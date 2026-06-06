# Claude Code 状态灯配置

AgentStatusLight 可以接收 Claude Code CLI 的 hooks 事件，让 Claude 像 Codex 一样点亮状态灯。

Claude hooks 是 Claude Code 的事件通知机制。用户不需要理解它的内部细节；只要需要 Claude Code 也点亮状态灯，就可以先使用软件内的一键配置。

## 状态对应关系

- Claude 正在处理任务或调用工具：黄灯。
- Claude 等待权限确认或用户输入：红灯。
- Claude 本轮响应结束：绿灯。

## 配置位置

推荐先使用软件内的一键配置：右键悬浮灯，点击 `Claude Code` -> `启用状态灯`。程序会写入当前用户全局配置，对当前用户的所有 Claude Code 项目生效，并在写入前备份原文件。

如果不再使用 Claude Code 状态灯，点击 `Claude Code` -> `移除状态灯配置`。程序只会移除 AgentStatusLight 自己写入的状态通知入口，不会删除用户自己的其他 Claude hooks。

如果需要手动配置，可以把 hooks 写到以下任一位置：

- `.claude\settings.local.json`：仅当前项目本机生效，通常不提交到仓库。
- `%USERPROFILE%\.claude\settings.json`：所有项目生效。

Claude hooks 需要当前工作区信任后才会执行。

## 需要配置的事件

把下面这些事件都指向同一个命令：

- 工作事件（黄灯）：`UserPromptSubmit`、`UserPromptExpansion`、`PreToolUse`、`PostToolUse`、`PostToolUseFailure`、`PostToolBatch`、`PermissionDenied`、`SubagentStart`、`TaskCreated`、`MessageDisplay`、`ElicitationResult`
- 确认事件（红灯）：`PermissionRequest`、`Elicitation`、`Notification`（类型为 `permission_prompt` 或 `elicitation_dialog`）
- 完成事件（绿灯）：`Stop`、`StopFailure`（无后台任务时），或 `MessageDisplay`（`final` 为 `true`）
- 会话结束/清理：`Stop`、`StopFailure`、`SubagentStop`、`TaskCompleted`、`TeammateIdle`、`SessionEnd`、`Notification`（类型为 `idle_prompt`）

程序会读取 Claude 通过 stdin 传入的 hook JSON，并根据 `hook_event_name` 判断状态。

## 示例配置

命令请使用 `AgentStatusLight.exe` 的绝对路径，并用 `args` 传入 `--claude-hook`。这样 Claude Code 会直接启动 exe，不依赖 Windows shell 解析引号。状态灯 hook 不需要阻塞 Claude Code，因此建议设置 `"async": true`。下面示例里的路径需要替换成你自己的实际位置，其他事件按同样结构添加到同一个命令：

```json
{
  "hooks": {
    "UserPromptSubmit": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "C:\\Users\\you\\Desktop\\AgentStatusLight.exe",
            "args": ["--claude-hook"],
            "async": true
          }
        ]
      }
    ],
    "PermissionRequest": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "C:\\Users\\you\\Desktop\\AgentStatusLight.exe",
            "args": ["--claude-hook"],
            "async": true
          }
        ]
      }
    ],
    "Stop": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "C:\\Users\\you\\Desktop\\AgentStatusLight.exe",
            "args": ["--claude-hook"],
            "async": true
          }
        ]
      }
    ]
  }
}
```

配置完成后，可以在 Claude Code 里打开 `/hooks` 检查这些事件是否已经指向 `AgentStatusLight.exe --claude-hook`。
