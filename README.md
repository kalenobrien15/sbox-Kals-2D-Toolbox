# sbox- Kal's 2D Toolbox

A set of lightweight 2D game components for [s&box](https://sbox.game/) built on Source 2. Designed for top-down games and 2D platformers using the `SpriteRenderer` component.

> Built and tested on s&box update **26.04.08**

# Initial project setup

<img width="1496" height="771" alt="2D Editor setup" src="https://github.com/user-attachments/assets/ee3b992f-3329-421b-a4be-aefda884386b" />
Setup Editor by clicking top right of editor window and change view to 2D Front

<img width="474" height="575" alt="Camera Settings" src="https://github.com/user-attachments/assets/aa9ec8c7-3254-43b5-bc4f-327faeb8870e" />

Camera needs to be set to Orthographic. Local position for x handles depth and rendering, if youre using 3D objects for terrain set -50 for local x position to ensure camera renders objects.

---

## Components
<img width="2215" height="1056" alt="camera and movement" src="https://github.com/user-attachments/assets/2034cd88-9231-4e1b-8fdc-d9c56a831811" />

### `Movement2D.cs`

The core movement controller. Add this to your player GameObject alongside a `SpriteRenderer` and `Collider2D`.

**Supports two modes toggled by a single checkbox in the inspector:**

| Mode | Description |
|---|---|
| **Top-Down** (default) | WASD moves in all four directions. Diagonal movement is normalised so speed is always consistent. |
| **Platformer** | A/D move horizontally, Space/Jump to jump, gravity applied automatically. Wall jumping, Particles for dust |

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Mode | Platformer Mode | `false` | Toggle between top-down and platformer |
| Movement | Move Speed | `200` | Units per second |
| Movement | Acceleration | `10` | Lerp rate toward target velocity |
| Platformer | Gravity | `800` | Downward force when airborne |
| Platformer | Jump Force | `400` | Upward velocity on jump |
| Platformer | Allow Double Jump | `false` | Enables a second jump in the air |
| Components | Collider | — | Reference to `Collider2D` (auto-found) |
| Components | Sprite Renderer | — | Reference to `SpriteRenderer` (auto-found) |
| Sprite | Flip On Move | `true` | Flips sprite horizontally based on direction |
| Sprite | Idle Animation | `"idle"` | Animation name to play when still |
| Sprite | Move Animation | `"move"` | Animation name to play when moving |
| Sprite | Jump Animation | `"Jump"` | Animation name to play when moving |
| Sprite | Jump Animation | `"WallSlide"` | Animation name to play when moving |

**Animation behaviour:**
- Switches between idle and move animations only when the state changes — won't restart the animation every frame
- Playback speed scales with velocity, so the walk cycle slows naturally as the character decelerates
- Collision with walls plays wall slide
- Jump is self explantory

**TODO:**
- Coyote Time.

**Input actions required** (set up in Project Settings → Input):

| Action | Top-Down | Platformer |
|---|---|---|
| `Forward` | Move up | — |
| `Backward` | Move down | — |
| `Left` | Move left | Move left |
| `Right` | Move right | Move right |
| `Jump` | — | Jump |

---

### `Collider2D.cs`

A custom box collider designed for 2D games on Source 2's axis layout. Uses physics trace sweeps instead of `CharacterController`, which avoids the ground-detection issues that break top-down movement.

Add this to the same GameObject as `Movement2D`. It is automatically discovered on start.

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Shape | Width | `32` | Collision box width in world units |
| Shape | Height | `32` | Collision box height in world units |
| Shape | Offset | `(0,0,0)` | Moves the centre of the box relative to the GameObject |
| Gizmo | Gizmo Color | Green | Colour of the editor visualisation |
| Gizmo | Gizmo Fill Opacity | `0.15` | Transparency of the filled box in the editor |

**Features:**
- Box is visualised in the editor viewport when the GameObject is selected — adjust offset and size visually in real time
- Clicking the gizmo box in the viewport selects the GameObject
- Slides along surfaces rather than stopping dead on collision
- Ignores objects tagged `player` and `trigger`

---

### `CameraFollow2D.cs`

A smooth camera follow script for your Camera GameObject. Follows a target with configurable lag and an optional deadzone.

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Target | Follow Target | — | The GameObject to follow |
| Target | Offset | `(0,0,0)` | Positional offset from the target |
| Target | Lock X | `true` | Prevents the camera moving on the X axis (depth) — keep this on for 2D |
| Target | Lock Y | `false` | Locks the Y axis |
| Target | Lock Z | `false` | Locks the Z axis |
| Smoothing | Follow Speed | `5` | Lerp rate — higher is snappier, lower is more delayed |
| Smoothing | Snap On Start | `true` | Teleports to target on first frame instead of sliding in |
| Deadzone | Deadzone Radius | `0` | Camera won't move until the target is this far away. Set to 0 to disable. |

**Gizmo:**
- Yellow circle shows the deadzone radius
- Cyan line shows the connection from camera to target

---

## Axis layout

S&box (Source 2) uses a different axis convention to Unity/Godot. These components are built around:

| Axis | 2D meaning |
|---|---|
| **X** | Depth (camera distance) — kept fixed |
| **Y** | Left / Right |
| **Z** | Up / Down |

Set your camera rotation to `(0, 90, 0)` and position it back on the negative X axis (e.g. `X = -500`) facing into the scene.

---

## Setup

1. Copy `Movement2D.cs`, `Collider2D.cs`, and `CameraFollow2D.cs` into your project's `Code/` folder
2. On your **player GameObject**, add:
   - `SpriteRenderer` — assign your `.sprite` asset
   - `Collider2D` — tune Width, Height, and Offset to match your sprite
   - `Movement2D` — components are auto-discovered, set your animation names
3. On your **Camera GameObject**, add:
   - `CameraFollow2D` — drag your player into the Follow Target field
   - Set **Lock X = true** to keep the camera at its depth position
4. In **Project Settings → Input**, ensure `Forward`, `Backward`, `Left`, `Right`, and `Jump` actions are bound

---

## Known limitations

- Platformer mode ground detection uses a short downward raycast — works best with flat horizontal surfaces
- `Collider2D` ignores objects tagged `player` and `trigger` by default — ensure your walls/obstacles have colliders but not these tags
- The `[Range]` attribute on properties will show obsolete warnings in s&box 26.04.08 — replace `[Range(min, max, step)]` with `[Range(min, max), Step(step)]` to resolve

# Sprite Atlas Slicer (BUGGED RN)
<img width="870" height="700" alt="atlasslicer" src="https://github.com/user-attachments/assets/5b8164a0-da2d-4550-ba35-0e17e36a31c0" />

An s&box editor tool for slicing spritesheets into individual frame PNGs and automatically creating a `.sprite` asset ready to use with `SpriteRenderer`.

> Built and tested on s&box update **26.04.08**

---

## Features

- Load any PNG/JPG/TGA spritesheet
- Configure slice dimensions via columns × rows or explicit frame size
- Configurable padding for sheets with gaps between frames
- Live preview showing the grid overlay on the spritesheet
- Export all frames as individual PNGs
- Automatically creates a `.sprite` asset from the exported frames
- Defaults the output folder to your project's Assets directory
- Available directly from the Editor Apps sidebar and Apps menu

---

## Installation

1. Copy `SpriteAtlasSlicer.cs` into your project's `Editor/` folder
2. The tool will appear in the **Apps sidebar** and under **Apps** in the menu bar automatically — no further setup needed

> The file must be inside a folder named `Editor/` so s&box compiles it as editor-only code and excludes it from game builds.

---

## Usage

### Opening the tool

Click **Sprite Atlas Slicer** in the Apps sidebar, or go to **Apps → Sprite Atlas Slicer** in the menu bar.

### Loading a spritesheet

Click **Load Spritesheet...** and select your image file. The preview panel on the right will display the image.

### Configuring the slice

All five fields are always editable:

| Field | Description |
|---|---|
| **Columns** | Number of frames across the sheet horizontally |
| **Rows** | Number of frames down the sheet vertically |
| **Frame Width (px)** | Explicit frame width. Set to `0` to calculate automatically from Columns |
| **Frame Height (px)** | Explicit frame height. Set to `0` to calculate automatically from Rows |
| **Padding (px)** | Gap in pixels between frames, if your sheet has spacing |

**Mode logic:**
- If Frame Width and Frame Height are both `0` → frame size is calculated from Columns × Rows
- If Frame Width and Frame Height are both `> 0` → those values are used and Columns/Rows are derived automatically

The info bar below the fields shows the calculated frame size, total frame count, and sheet dimensions in real time.

### Frame Rate

Set the **Frame Rate (fps)** field to control the playback speed of the generated animation. Defaults to `10`.

### Output settings

| Field | Description |
|---|---|
| **Output name** | Base name for exported files. Frames are named `name_000.png`, `name_001.png` etc. |
| **Output folder** | Where files are saved. Defaults to your project's Assets folder. |

Click **Browse Output Folder...** to pick a folder — the dialog opens in your project's Assets folder by default.

### Exporting

Click **Export All Frames** to:

1. Slice and save each frame as an individual PNG
2. Create a `.sprite` asset containing all frames as a single `default` animation
3. The `.sprite` file appears in your asset browser automatically

---

## Preview

The right panel shows a live preview of the spritesheet with a green grid overlay indicating where each frame will be sliced. The preview updates whenever you change any slice setting.

The background is dark grey to make transparency visible.

---

## Known issues & bugs

### `AssetSystem.RegisterFile` corrupts the Asset Browser

**Severity:** High

Calling `AssetSystem.RegisterFile(path)` on a newly created file causes the Asset Browser to stop showing all previously tracked assets and folders — only the registered file remains visible. This persists after restarting the editor.

**Workaround:** Delete the `.sbox` cache folder in your project root to restore the Asset Browser.

**Status:** Reported to Facepunch — [sbox-issues]

This call has been removed from the current version of the tool. The `.sprite` file will appear in the asset browser naturally when the editor's file watcher picks it up.

---

## File structure

```
Editor/
└── SpriteAtlasSlicer.cs    ← the entire tool, single file
```

---

## Requirements

- s&box editor (tested on **26.04.08**)
- Project must have an `Editor/` folder for the file to compile correctly
- Output folder should be inside your project's `Assets/` directory for exported files to be picked up by the asset system.
