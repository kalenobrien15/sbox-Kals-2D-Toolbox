# sbox - Kal's 2D Toolbox

A set of lightweight 2D game components for [s&box](https://sbox.game/) built on Source 2. Designed for top-down games and 2D platformers using the `SpriteRenderer` component.

> Built and tested on s&box update **26.04.08**

https://github.com/user-attachments/assets/6520f729-4653-49ad-b655-1ad2c2f8680b

---

# Initial project setup

<img width="1496" height="771" alt="2D Editor setup" src="https://github.com/user-attachments/assets/ee3b992f-3329-421b-a4be-aefda884386b" />

Setup the editor by clicking the top right of the editor window and changing the view to **2D Front**.

<img width="474" height="575" alt="Camera Settings" src="https://github.com/user-attachments/assets/aa9ec8c7-3254-43b5-bc4f-327faeb8870e" />

Camera needs to be set to **Orthographic**. Local position X handles depth and rendering — if you're using 3D objects for terrain set `-50` for local X to ensure the camera renders objects correctly.

---

# Components

<img width="2215" height="1056" alt="camera and movement" src="https://github.com/user-attachments/assets/2034cd88-9231-4e1b-8fdc-d9c56a831811" />

---

## `Movement2D.cs`

The core movement controller. Add to your player GameObject alongside a `SpriteRenderer` and `Collider2D`.

**Supports two modes toggled by a single checkbox in the inspector:**

| Mode | Description |
|---|---|
| **Top-Down** (default) | WASD moves in all four directions. Diagonal movement is normalised so speed is always consistent. |
| **Platformer** | A/D move horizontally, Space/Jump to jump, gravity applied automatically. Supports wall jumping, wall sliding, double jump, and dust particles. |

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Mode | Platformer Mode | `false` | Toggle between top-down and platformer |
| Movement | Move Speed | `200` | Units per second |
| Movement | Acceleration | `10` | Lerp rate toward target velocity |
| Platformer | Gravity | `800` | Downward force when airborne |
| Platformer | Jump Force | `400` | Upward velocity on jump |
| Platformer | Allow Double Jump | `false` | Enables a second mid-air jump |
| Wall Jump | Enable Wall Jump | `true` | Allows jumping off walls |
| Wall Jump | Wall Jump Vertical Force | `400` | Upward force on wall jump |
| Wall Jump | Wall Kick Horizontal Force | `300` | Horizontal force away from wall on wall jump |
| Wall Jump | Wall Kick Duration | `0.2s` | How long player input is suppressed after wall jump so kick force carries |
| Wall Jump | Wall Jump Cooldown | `0.6s` | Minimum time between wall jumps — prevents spam |
| Wall Slide | Enable Wall Slide | `true` | Player slides down walls when airborne and pressed against one |
| Wall Slide | Initial Slide Speed | `30` | Fall speed when first touching the wall |
| Wall Slide | Maximum Slide Speed | `200` | Maximum fall speed while sliding — approached over time |
| Wall Slide | Slide Acceleration Time | `1.5s` | How long it takes to ramp from initial to maximum slide speed |
| Particles | Jump Dust Emitter | — | `ParticleSphereEmitter` for jump burst |
| Particles | Jump Dust Effect | — | `ParticleEffect` target for jump dust |
| Particles | Jump Dust Count | `10` | Number of particles per jump burst |
| Particles | Move Dust Emitter | — | `ParticleSphereEmitter` for run trail |
| Particles | Wall Slide Emitter | — | `ParticleSphereEmitter` for wall slide hand friction |
| Sprite | Flip On Move | `true` | Flips sprite horizontally based on facing direction |
| Sprite | Sprite Faces Right | `true` | Set to `false` if your sprite art faces left by default |
| Sprite | Idle Animation | `"idle"` | Animation played when still |
| Sprite | Move Animation | `"move"` | Animation played when running |
| Sprite | Jump Animation | `"jump"` | Animation played when airborne |
| Sprite | Wall Slide Animation | `"wallslide"` | Animation played when wall sliding |
| Sprite | Death Animation | `"death"` | Animation played on death |

**Animation priority:** wallslide → jump/fall → move → idle. Switches immediately when state changes — no sticky transitions.

**Particle setup:**
- **Jump dust** — one-shot burst on jump and wall jump. Loop should be **off**.
- **Move dust** — continuous trail while grounded and moving. Loop must be **on**.
- **Wall slide dust** — continuous emission while wall sliding. Loop must be **on**. Position flips automatically to match wall side.

**Wall jump tuning tips:**
- Increase `Wall Jump Cooldown` to prevent spam on the same wall
- `Wall Kick Duration` and cooldown together control whether climbing between two adjacent walls is possible as a skill move
- Wall colliders must have the **`wall`** tag for detection to work

*Public references (used by `Health2D`):**

```csharp
movement.ApplyImpulse( new Vector2( horizontal, vertical ) ); // knockback
movement.TriggerDeath();    // disables input, plays death anim
movement.TriggerRespawn();  // resets state, plays idle
movement.InputAllowed = true/false;
movement.FacingDirection;   // 1 = right, -1 = left (read-only)
```

**Input actions required:**

| Action | Top-Down | Platformer |
|---|---|---|
| `Forward` | Move up | — |
| `Backward` | Move down | — |
| `Left` | Move left | Move left |
| `Right` | Move right | Move right |
| `Jump` | — | Jump |

**TODO:**
- Coyote time

---

## `Collider2D.cs`

A custom box collider for 2D games on Source 2's axis layout. Uses physics trace sweeps instead of `CharacterController`.

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Shape | Width | `32` | Collision box width in world units |
| Shape | Height | `32` | Collision box height in world units |
| Shape | Offset | `(0,0,0)` | Offset of the box centre relative to the GameObject |
| Gizmo | Gizmo Color | Green | Colour of the editor visualisation |
| Gizmo | Gizmo Fill Opacity | `0.15` | Transparency of the filled box in the editor |

**Features:**
- Box visualised in the editor viewport when the GameObject is selected
- Slides along surfaces rather than stopping dead on collision
- Ignores objects tagged `player` and `trigger`

---

## `CameraFollow2D.cs`

Smooth camera follow with optional deadzone. Add to your Camera GameObject.

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Target | Follow Target | — | The GameObject to follow |
| Target | Offset | `(0,0,0)` | Positional offset from target |
| Target | Lock X | `true` | Keeps camera at fixed depth — always keep on for 2D |
| Target | Lock Y | `false` | Locks Y axis |
| Target | Lock Z | `false` | Locks Z axis |
| Smoothing | Follow Speed | `5` | Lerp rate — higher is snappier |
| Smoothing | Snap On Start | `true` | Teleports to target on first frame |
| Deadzone | Deadzone Radius | `0` | Camera won't move until target exits this radius |

---

## `CameraController2D.cs`

Replaces `StartAnimation.cs`. Manages the pixelate intro on scene load and gates player input. Also used by `Health2D` for the death/respawn transition. Attach to your Camera GameObject.

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| References | Pixelate Component | — | The `Pixelate` post-process on your camera |
| References | Player | — | Player GameObject containing `Movement2D` |
| Intro | Intro Unpixelate Speed | `1` | How fast the screen clears on scene start |

**Behaviour:**
- Starts fully pixelated (`Scale = 1`) and clears to `0` over time
- Sets `Movement2D.InputAllowed = true` once clear
- The same `Pixelate` component is referenced by `Health2D` for the death/respawn sequence

---

## `Health2D.cs`

https://github.com/user-attachments/assets/6505204e-4f0a-4d6e-97c6-1017e1ab9eb9

https://github.com/user-attachments/assets/4bfe848c-3c64-4906-970f-111e48da8224

Full health and damage system. Add to your player GameObject alongside `Movement2D` and `SpriteRenderer`.

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Health | Max Health | `100` | Maximum HP |
| Iframes | Invincibility Duration | `1s` | Invincible window after flat damage |
| Iframes | Flash On Damage | `true` | Flashes `OverlayColor` during iframes |
| Iframes | Flash Color | Red | Color to flash during iframes |
| Iframes | Flash Interval | `0.1s` | Flash toggle rate |
| Knockback | Enable Knockback | `true` | Applies velocity impulse on damage |
| Knockback | Knockback Horizontal Force | `300` | Horizontal push force |
| Knockback | Knockback Vertical Force | `200` | Upward push force (platformer only) |
| Ticking Damage | Ticking Damage Particle | — | Child `ParticleEffect` enabled during poison/fire |
| Death & Respawn | Camera Pixelate Component | — | `Pixelate` component for death/respawn transition |
| Death & Respawn | Death Anim Hold Duration | `1s` | Wait before pixelating in on death |
| Death & Respawn | Pixelate In Speed | `2` | Speed of pixelate in on death |
| Death & Respawn | Blackout Hold Duration | `0.5s` | Hold at full pixelation before respawning |
| Death & Respawn | Pixelate Out Speed | `2` | Speed of pixelate out after respawn |
| Regen | Enable Checkpoint Regen | `true` | Heals when checkpoint is set with a heal amount |
| Regen | Full Heal On Respawn | `true` | Restores full HP on respawn |
| Floating Numbers | Enable Damage Numbers | `true` | World-space floating damage numbers |
| Floating Numbers | Float Speed | `80` | How fast numbers float upward |
| Floating Numbers | Number Lifetime | `1s` | How long numbers are visible |
| Components | Movement | — | Reference to `Movement2D` (auto-found) |
| Components | Sprite Renderer | — | Reference to `SpriteRenderer` (auto-found) |

**Public references to do damage:**

```csharp
// Flat damage with optional attacker position for top-down knockback direction
health.TakeDamage( 10f );
health.TakeDamage( 10f, attackerWorldPosition );

// Ticking damage — restarts timer if already active
// Total ticks = floor( totalDuration / tickInterval )
// e.g. (1f, 1f, 3f) = 2 ticks at 1s and 2s, stops at 3s
health.ApplyTickingDamage( damagePerTick: 1f, tickInterval: 1f, totalDuration: 3f );

// Heal
health.Heal( 25f );

// Checkpoint — call from your own checkpoint trigger script
health.SetCheckpoint( WorldPosition );
health.SetCheckpoint( WorldPosition, healAmount: 50f );
```

**Events:**

```csharp
health.OnDamaged   += (amount) => { };
health.OnHealed    += (amount) => { };
health.OnDeath     += () => { };
health.OnRespawned += () => { };
```

**Death sequence:**
1. Input disabled, death animation plays
2. Wait `Death Anim Hold Duration`
3. Screen pixelates in at `Pixelate In Speed`
4. Hold at full pixelation for `Blackout Hold Duration`
5. Player teleports to last checkpoint, HP restored if `Full Heal On Respawn` is on
6. Screen unpixelates at `Pixelate Out Speed`
7. Input re-enabled

**Floating numbers:** (THIS ISNT WORKING RIGHT YET FORGIVE ME)
- Flat damage → **white**
- Ticking damage → **orange**
- Numbers float upward and fade over `Number Lifetime` seconds

**Knockback notes:**
- Platformer mode: knockback is opposite to `FacingDirection` with an upward component
- Top-down mode: knockback is away from the attacker position
- Sprite flip is locked during knockback — player always faces toward the source of damage
- If knocked into a wall, facing corrects automatically to the wall slide direction

---

## `HealthHUD2D.cs`

Screen-space health HUD. Add as a `PanelComponent` to a separate UI GameObject that also has a `ScreenPanel` component. `Health2D` finds it automatically — no wiring needed.

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Display | Use Heart Mode | `false` | Toggle between bar mode and heart icon mode |
| Bar | Bar Color (full) | Green | Bar fill color at high health |
| Bar | Bar Color (low) | Red | Bar fill color at or below low health threshold |
| Bar | Low Health Threshold | `0.25` | Fraction of max HP where bar turns red |
| Bar | Show HP Text | `true` | Shows `current / max` centered on the bar |
| Bar | HP Text Font | `"Roboto"` | Font family name or path to `.ttf` in your UI folder |
| Bar | HP Text Color | White | Color of the HP text |
| Hearts | Heart Filled Texture | — | `Texture` asset for filled heart |
| Hearts | Heart Empty Texture | — | `Texture` asset for empty heart |
| Hearts | Heart Size | `32px` | Width and height of each heart |
| Layout | Position | `(20, 20)` | Offset from top-left of screen in pixels |
| Layout | Bar Width | `200px` | Width of the health bar |
| Layout | Bar Height | `20px` | Height of the health bar |

**Heart mode notes:**
- Heart count is dynamic — one heart per point of `MaxHealth`
- `ImageRendering` is set to `Point` automatically — pixel art hearts stay sharp
- Rebuilds on every `UpdateHealth` call

**Scene setup:**
1. Create an empty GameObject
2. Add `ScreenPanel` component
3. Add `HealthHUD2D` component
4. Configure in inspector

---

## `HealthDebug.cs`

Test component for verifying the health system. **Remove before shipping.**

Add to your player GameObject alongside `Health2D`. Set up `damage`, `poison`, and `heal` as input actions in **Project Settings → Input**.

| Input Action | Effect |
|---|---|
| `damage` | `TakeDamage(1f)` |
| `poison` | `ApplyTickingDamage(1f, 0.5f, 3f)` |
| `heal` | `Heal(1f)` |

---

# Full player setup

**Player GameObject:**
- `SpriteRenderer` — assign your `.sprite` asset
- `Collider2D` — tune width, height, offset to match your sprite
- `Movement2D` — auto-discovers collider and sprite renderer, set animation names and particle references
- `Health2D` — drag in `Movement2D`, `SpriteRenderer`, `Pixelate` component, and ticking particle if needed

**Camera GameObject:**
- `CameraFollow2D` — drag player into Follow Target, set Lock X = true
- `CameraController2D` — drag `Pixelate` component and Player in
- `Pixelate` post-process component

**UI GameObject (separate, not a child of player):**
- `ScreenPanel`
- `HealthHUD2D`

**Checkpoint script (write your own):**
```csharp
void OnTriggerEnter( GameObject other )
{
    var health = other.GetComponent<Health2D>();
    if ( health is null ) return;
    health.SetCheckpoint( WorldPosition, healAmount: 0f );
}
```

---

# Axis layout

S&box (Source 2) uses a different axis convention to Unity/Godot:

| Axis | 2D meaning |
|---|---|
| **X** | Depth (camera distance) — kept fixed |
| **Y** | Left / Right |
| **Z** | Up / Down |

Set your camera rotation to `(0, 90, 0)` and position it back on the negative X axis facing into the scene.

---

# Known limitations

- Platformer ground detection uses a short downward raycast (`0.25` units) — works best with flat horizontal surfaces
- Wall detection requires wall colliders to have the **`wall`** tag
- `Collider2D` ignores objects tagged `player` and `trigger` by default
- The `[Range]` attribute with three arguments shows obsolete warnings — use `[Range(min, max), Step(step)]`
- `Health2D` floating numbers use `Gizmo.Draw.Text` which renders in world space — may not be visible depending on gizmo settings in your project

---

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
2. The tool appears in the **Apps sidebar** and under **Apps** in the menu bar automatically

> The file must be inside a folder named `Editor/` so s&box compiles it as editor-only code.

---

## Usage

### Opening the tool

Click **Sprite Atlas Slicer** in the Apps sidebar, or go to **Apps → Sprite Atlas Slicer**.

### Configuring the slice

| Field | Description |
|---|---|
| **Columns** | Number of frames across the sheet horizontally |
| **Rows** | Number of frames down the sheet vertically |
| **Frame Width (px)** | Explicit frame width. Set to `0` to calculate from Columns |
| **Frame Height (px)** | Explicit frame height. Set to `0` to calculate from Rows |
| **Padding (px)** | Gap in pixels between frames |

Click **Export All Frames** to slice, save PNGs, and generate the `.sprite` asset.

---

## Known issues

### `AssetSystem.RegisterFile` corrupts the Asset Browser

**Severity:** High

Calling `AssetSystem.RegisterFile(path)` causes the Asset Browser to stop showing all previously tracked assets. Persists after editor restart.

**Workaround:** Delete the `.sbox` cache folder in your project root.

**Status:** Reported to Facepunch. This call has been removed from the current version — the `.sprite` file appears in the asset browser naturally when the editor's file watcher picks it up.
