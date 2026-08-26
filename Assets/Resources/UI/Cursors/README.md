# Unified Light Fantasy Cursor Set

All sprites are 64x64 PNG files with genuine alpha transparency. Directional artwork points toward the upper-left to match a standard left mouse cursor.

| Runtime state | Sprite | Suggested hotspot |
|---|---|---|
| Default | `cursor_default.png` | `(17, 16)` arrow tip |
| Attack | `cursor_attack.png` | `(15, 15)` sword tip |
| Talk | `cursor_talk.png` | `(16, 22)` speech-tail tip |
| Blocked / Out of range | `cursor_blocked.png` | `(16, 16)` upper-left indicator tip |
| Interact / Open | `cursor_interact.png` | `(16, 17)` index fingertip |
| Mining | `cursor_mining.png` | `(15, 18)` upper-left pickaxe tip |
| Chopping | `cursor_chopping.png` | `(15, 25)` upper-left cutting edge |
| Gathering | `cursor_gathering.png` | `(17, 19)` upper-left fingertips |

`GameCursorManager` loads these textures from `Resources/UI/Cursors`, applies the hotspots above and resolves hovered 2D world targets without taking ownership of their gameplay interaction.
