# AgentStatusLight

[中文说明](README.md)

<p align="center">
  <img src="assets/icons/AgentStatusLight.png" alt="AgentStatusLight icon" width="128">
</p>

AgentStatusLight is a small desktop status light that shows the current Codex or Claude state with three lights: red, yellow, and green.

It is designed to sit in a corner of your desktop. Yellow means the agent is working, red means it needs your input, and green means it has finished.

## The Problem

When Codex or Claude Code handles a longer task, the terminal often keeps running in the background. You may need to switch back repeatedly just to check whether it is still working, already finished, or waiting for your confirmation.

AgentStatusLight puts that state on the desktop with three simple lights: red means it needs your confirmation, yellow means work is in progress, and green means the task has just finished. This way, you do not have to keep watching the terminal, but you can still see when you need to step in.

## Quick Start

1. Download `AgentStatusLight.zip`.
2. Extract it to a writable location, such as your Desktop or Documents folder.
3. Double-click `AgentStatusLight.exe` to run it.
4. Drag the floating light to your preferred position.

On first launch, the floating light appears in the center of the screen and stays on top of other windows. After you move it, the app will remember that position the next time it starts.

> Avoid placing it under `C:\Program Files\`, because that location may require administrator permissions and can prevent the app from saving settings or status data.

## Status Lights

- Red: the agent needs your confirmation or input.
- Yellow: the agent is working.
- Green: the agent has just finished.
- Dimmed three lights: the agent is idle.

By default, the lights are shown horizontally in red, yellow, green order. You can switch to a vertical layout from the right-click menu.

## Right-Click Menu

Right-click the floating light to:

- Switch theme: system, dark, light, or transparent.
- Open color settings.
- Open notification settings.
- Open sound settings.
- Toggle the breathing light effect.
- Switch light layout: horizontal or vertical.
- Open Claude Code status light setup.
- Manually switch to red, yellow, or green.
- Restore automatic status detection.
- View app information and update hints.
- Exit the app.

After manually switching the light color, automatic detection is paused. Click `Restore automatic status detection` to enable it again.

## Colors And Sounds

In `Color settings`, you can customize the colors for four states:

- Confirmation color: red light.
- Working color: yellow light.
- Completion color: green light.
- Idle color: dimmed idle state.

In `Sound settings`, you can enable sound alerts. Red and green states can each use a custom WAV file. The yellow working state does not play a sound.

## Notifications

In `Notification settings`, you can configure Windows notifications, Bark, PushPlus, and Telegram.

Each channel can send notifications for red confirmation and green completion states. By default, only green completion notifications are enabled.

In `Notification template`, you can customize the notification title and body, and preview the result. Templates support `{state}`, `{message}`, and `{count}`.

Notification keys are saved in the `data` folder next to the program. Do not share this folder publicly.

## About And Updates

Right-click the floating light and choose `About software` to view the current version and project link.

When a newer version is detected, the `About software` menu item shows an update hint, and the dialog shows the latest version number.

## Claude Code

If you only use Codex, no extra setup is usually needed.

To make Claude Code CLI light up the status light, right-click the floating light and choose `Claude Code` -> `Enable status light`. After setup is complete, restart Claude Code so the status light configuration can take effect.

If you no longer want to use Claude Code with the status light, choose `Claude Code` -> `Remove status light configuration`. The app will remove the status notification entries it added to the Claude Code configuration.

Manual setup and event details are available in [Claude Code status light configuration](docs/claude-hooks.md).

## More Documentation

- [Claude Code status light configuration](docs/claude-hooks.md)
- [Development and data notes](docs/development.md)

## Acknowledgements

Thanks to [Linux.do](https://linux.do/) for community support.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
