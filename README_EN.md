# AgentStatusLight

[中文说明](README.md)

AgentStatusLight is a small desktop status light that shows the current Codex or Claude state with three lights: red, yellow, and green.

It is designed to sit in a corner of your desktop. Yellow means the agent is working, red means it needs your input, and green means it has finished.

## Quick Start

1. Download `AgentStatusLight.exe`.
2. Put the exe in a writable location, such as your Desktop or Documents folder.
3. Double-click it to run.
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
- Switch light layout: horizontal or vertical.
- Open color settings.
- Open notification settings.
- Open sound settings.
- Manually switch to red, yellow, or green.
- Restore automatic status detection.
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

In `Notification settings`, you can configure Bark, PushPlus, and Telegram.

Each channel can send notifications for red confirmation and green completion states. By default, only green completion notifications are enabled.

Notification keys are saved in the `data` folder next to the program. Do not share this folder publicly.

## Claude Code

If you only use Codex, no extra setup is usually needed.

To make Claude Code CLI light up the status light, right-click the floating light and choose `Claude Code` -> `Enable status light`. After setup is complete, restart Claude Code so the status light configuration can take effect.

If you no longer want to use Claude Code with the status light, choose `Claude Code` -> `Remove status light configuration`. The app will remove the status notification entries it added to the Claude Code configuration.

Manual setup and event details are available in [Claude Code status light configuration](docs/claude-hooks.md).

## More Documentation

- [Claude Code status light configuration](docs/claude-hooks.md)
- [Development and data notes](docs/development.md)
