# sbox - Kal's 2D Toolbox

A set of lightweight 2D game components for [s&box](https://sbox.game/) built on Source 2. Designed for top-down games and 2D platformers using the `SpriteRenderer` component.

> Built and tested on s&box update **26.04.08**

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
| Audio | Jump Sound | — | `SoundEvent` played on jump |
| Audio | Wall Jump Sound | — | `SoundEvent` played on wall jump |
| Audio | Land Sound | — | `SoundEvent` played on landing |
| Audio | Heavy Land Sound | — | `SoundEvent` played when landing after a large fall |
| Audio | Heavy Land Speed Threshold | `600` | Downward velocity required to trigger the heavy land sound |
| Audio | Wall Slide Sound | — | `SoundEvent` played while wall sliding — loops and stops automatically |
| Particles | Jump Dust Emitter | — | `ParticleSphereEmitter` for jump burst |
| Particles | Jump Dust Effect | — | `ParticleEffect` target for jump dust |
| Particles | Jump Dust Count | `10` | Number of particles per jump burst |
| Particles | Move Dust Emitter | — | `ParticleSphereEmitter` for run trail |
| Particles | Wall Slide Emitter | — | `ParticleSphereEmitter` for wall slide friction |
| Sprite | Flip On Move | `true` | Flips sprite horizontally based on facing direction |
| Sprite | Sprite Faces Right | `true` | Set to `false` if your sprite art faces left by default |
| Sprite | Idle Animation | `"idle"` | Animation played when still |
| Sprite | Move Animation | `"move"` | Animation played when running |
| Sprite | Jump Animation | `"jump"` | Animation played when airborne |
| Sprite | Wall Slide Animation | `"wallslide"` | Animation played when wall sliding |
| Sprite | Death Animation | `"death"` | Animation played while airborne during death knockback |
| Sprite | Death Ground Animation | `"deathground"` | Animation played when landing after death |

**Animation priority:** wallslide → jump/fall → move → idle. Switches immediately when state changes — no sticky transitions. Death animations are locked and cannot be overridden by the normal animation system.

**Footsteps** are driven entirely by the sprite animation system — add a `PlaySound` broadcast event on the footstep frames in your walk animation in the sprite editor. No code needed.

**Input enable behaviour:**
- If `CameraController2D` is present and enabled in the scene, input is gated by it until the intro completes
- If `CameraController2D` is absent or disabled, input is enabled automatically on start — no setup needed for basic testing

**Knockback behaviour:**
- Sprite flip is locked for `0.4s` after a hit — player always faces toward the source of damage
- If knocked into a wall during knockback, facing corrects automatically to face away from the wall
- When knockback timer expires the player faces whichever direction they last moved

**Death behaviour:**
- On death, input is blocked but physics and gravity keep running so knockback carries naturally
- `TriggerDeathAir()` is called immediately — disables input, keeps physics running, no anim change
- When the player lands, `TriggerDeath()` fires — freezes the player and plays `Death Ground Animation`
- The pixelate death sequence begins after landing
- If the player is already grounded when death triggers, knockback still launches them before waiting to land

**Particle setup:**
- **Jump dust** — one-shot burst on jump and wall jump. Loop should be **off**.
- **Move dust** — continuous trail while grounded and moving. Loop must be **on**.
- **Wall slide dust** — continuous emission while wall sliding. Loop must be **on**. Position flips automatically to match wall side.

**Wall jump tuning tips:**
- Increase `Wall Jump Cooldown` to prevent spam on the same wall
- Wall colliders must have the **`wall`** tag for detection to work

**Public API (used by `Health2D`):**

```csharp
movement.ApplyImpulse( new Vector2( horizontal, vertical ) ); // knockback
movement.TriggerDeathAir();  // blocks input, keeps physics running
movement.TriggerDeath();     // freezes player, plays ground death anim
movement.TriggerRespawn();   // resets all state, plays idle
movement.InputAllowed = true/false;
movement.FacingDirection;    // 1 = right, -1 = left (read-only)
movement.IsGrounded;         // read-only, used by Health2D death sequence
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

> Note: `Collider2D` uses trace sweeps, not physics — `ITriggerListener` will not fire for this collider. `DamageZone2D` uses `BoxCollider.OnTriggerEnter` directly instead, and `Checkpoint2D` uses position overlap detection.

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

Manages the pixelate intro on scene load and gates player input. Also used by `Health2D` for the death/respawn transition. Attach to your Camera GameObject.

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
- If this component is absent or disabled, `Movement2D` enables input automatically on start

---

## `Health2D.cs`

Full health and damage system. Add to your player GameObject alongside `Movement2D` and `SpriteRenderer`.

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Health | Max Health | `100` | Maximum HP |
| Iframes | Invincibility Duration | `1s` | Invincible window after flat damage |
| Iframes | Flash On Damage | `true` | Flashes sprite `OverlayColor` during iframes |
| Iframes | Flash Color | Red | Color to flash during iframes |
| Iframes | Flash Interval | `0.1s` | Flash toggle rate |
| Knockback | Enable Knockback | `true` | Applies velocity impulse on damage |
| Knockback | Knockback Horizontal Force | `300` | Horizontal push force |
| Knockback | Knockback Vertical Force | `200` | Upward push force (platformer only) |
| Ticking Damage | Ticking Damage Particle | — | Child `ParticleEffect` enabled during poison/fire |
| Death & Respawn | Camera Pixelate Component | — | `Pixelate` component for death/respawn transition |
| Death & Respawn | Death Anim Hold Duration | `1s` | Wait after ground death anim before pixelating in |
| Death & Respawn | Pixelate In Speed | `2` | Speed of pixelate in on death |
| Death & Respawn | Blackout Hold Duration | `0.5s` | Hold at full pixelation before respawning |
| Death & Respawn | Pixelate Out Speed | `2` | Speed of pixelate out after respawn |
| Regen | Enable Checkpoint Regen | `true` | Heals when checkpoint is triggered with a heal amount |
| Regen | Full Heal On Respawn | `true` | Restores full HP on respawn |
| Floating Numbers | Enable Damage Numbers | `true` | World-space floating damage numbers via `TextRenderer` |
| Floating Numbers | Float Speed | `80` | How fast numbers float upward |
| Floating Numbers | Number Lifetime | `1s` | How long numbers are visible |
| Floating Numbers | Number Spawn Point | — | Child GameObject to use as spawn origin for numbers |
| Floating Numbers | Number Font | `""` | Font family name for damage numbers |
| Floating Numbers | Number Font Size | `18` | Font size for damage numbers |
| Audio | Hurt Sound | — | `SoundEvent` played on flat damage and first tick of ticking damage |
| Audio | Ticking Sound | — | `SoundEvent` played on every subsequent damage tick |
| Components | Movement | — | Reference to `Movement2D` (auto-found) |
| Components | Sprite Renderer | — | Reference to `SpriteRenderer` (must be assigned) |

**Public API:**

```csharp
// Flat damage with optional attacker position for knockback direction
health.TakeDamage( 10f );
health.TakeDamage( 10f, attackerWorldPosition );

// Ticking damage — restarts timer if already active
// Total ticks = floor( totalDuration / tickInterval )
// e.g. (1f, 1f, 3f) = 2 ticks at 1s and 2s, stops at 3s
health.ApplyTickingDamage( damagePerTick: 1f, tickInterval: 1f, totalDuration: 3f );

// Heal
health.Heal( 25f );

// Checkpoint
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
1. Flash cleared, iframe cancelled
2. Knockback applied — on fatal hits knockback fires before the death sequence starts so velocity carries
3. Input blocked, physics keeps running — player flies through the air with normal gravity
4. Player lands — ground death animation plays
5. Wait `Death Anim Hold Duration`
6. Screen pixelates in at `Pixelate In Speed`
7. Hold at full pixelation for `Blackout Hold Duration`
8. Player teleports to last checkpoint, HP restored if `Full Heal On Respawn` is on
9. Screen unpixelates at `Pixelate Out Speed`
10. Input re-enabled

**Floating numbers:**
- Flat damage → **white**
- Ticking damage → **orange**
- Numbers float upward and fade using `TextRenderer` — always visible, no gizmos required
- Spawn position controlled by `Number Spawn Point` child reference

**Knockback notes:**
- Knockback fires before death is triggered on fatal hits so velocity carries
- Sprite flip locked during knockback — player faces toward the damage source
- If knocked into a wall, facing corrects to the wall slide direction

---

## `HealthHUD2D.cs` + `HealthBar.razor`

Screen-space health HUD using Razor panels. Requires both components on the same UI GameObject.

**Scene setup:**
1. Create an empty GameObject
2. Add `ScreenPanel` component
3. Add `HealthHUD2D` component
4. Add `HealthBar` Razor PanelComponent
5. Place `HealthBar.razor` and `HealthBar.razor.scss` in your project's `ui/` folder

`Health2D` finds the HUD automatically via the scene — no wiring needed.

**Inspector properties (`HealthHUD2D`):**

| Group | Property | Default | Description |
|---|---|---|---|
| Display | Use Heart Mode | `false` | Toggle between bar mode and heart icon mode |
| Bar | Bar Background Color | `rgba(0,0,0,0.5)` | Background color of the bar — supports alpha transparency |
| Bar | Bar Color (full) | Green | Bar fill color at high health — supports alpha transparency |
| Bar | Bar Color (low) | Red | Bar fill color at or below low health threshold — supports alpha transparency |
| Bar | Low Health Threshold | `0.25` | Fraction of max HP where bar turns red |
| Bar | Show HP Text | `true` | Shows `current / max` centered on the bar |
| Bar | HP Text Font | `""` | Font family name for HP text |
| Bar | HP Text Font Size | `14` | Font size for HP text |
| Bar | HP Text Color | White | Color of the HP text |
| Bar | Text Padding Top | `0` | Nudge text vertically to center for pixel fonts |
| Bar | Text Padding Left | `0` | Left padding on HP text |
| Bar | Text Padding Right | `0` | Right padding on HP text |
| Hearts | Heart Filled Texture | — | PNG file picker — drag your filled heart image in |
| Hearts | Heart Empty Texture | — | PNG file picker — drag your empty heart image in |
| Hearts | Heart Size | `32px` | Width and height of each heart |
| Hearts | Heart Spacing | `4px` | Gap between hearts — negative values overlap |
| Layout | Center At Bottom | `true` | Centers the HUD horizontally at the bottom of the screen — resolution independent |
| Layout | Bottom Padding | `20px` | Distance from the bottom of the screen when `Center At Bottom` is enabled |
| Layout | Position | `(20, 20)` | Offset from top-left — only used when `Center At Bottom` is disabled |
| Layout | Bar Width | `200px` | Width of the health bar |
| Layout | Bar Height | `20px` | Height of the health bar |

**Heart mode notes:**
- Heart count is dynamic — one heart per point of `MaxHealth`
- Heart textures are PNG files dragged directly from the asset browser
- `image-rendering: pixelated` applied automatically — pixel art hearts stay sharp
- Negative `Heart Spacing` lets you overlap hearts for a tight packed look

**Color notes:**
- Bar background, full color, and low color all support alpha transparency via the color picker
- Set bar background to fully transparent for a flat fill-only look
- Set fill colors to semi-transparent for a stylised HUD effect

---

## `Checkpoint2D.cs`

Position overlap checkpoint — no physics collider required.

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Checkpoint | Heal Amount On Touch | `0` | HP restored when player enters the zone. `0` = no healing |
| Shape | Width | `32` | Zone width in world units |
| Shape | Height | `32` | Zone height in world units |
| Shape | Offset | `(0,0,0)` | Offset of the zone centre |

**Behaviour:**
- Yellow gizmo shows detection area in the editor
- `_triggered` flag resets when the player leaves — can be activated again if the player returns
- Calls `Health2D.SetCheckpoint( WorldPosition, HealAmount )` on touch

---

## `DamageZone2D.cs`

Trigger-based damage zone using `BoxCollider.OnTriggerEnter`. Add alongside a `BoxCollider` set to **Is Trigger**.

**Inspector properties:**

| Group | Property | Default | Description |
|---|---|---|---|
| Damage | Damage | `10` | Damage amount per hit |
| Damage | Is Ticking Damage | `false` | Deals ticking damage instead of flat |
| Damage | Tick Interval | `0.5s` | Seconds between each tick |
| Damage | Tick Duration | `3s` | Total duration of ticking effect |

**Setup:**
1. Add a `BoxCollider` to your damage object — check **Is Trigger**
2. Add `DamageZone2D` to the same GameObject
3. Make sure your player GameObject is tagged **`player`**
4. Shape the collider visually in the inspector to match your sprite

**For irregular shapes** — add multiple child GameObjects, each with their own `BoxCollider` (Is Trigger) — `DamageZone2D` automatically subscribes to all `BoxCollider` components on itself and its children so they all feed into the same damage logic.

- Flat damage zones rely on `Health2D` iframes to prevent spam damage
- Ticking damage restarts the timer if the player re-enters before it expires

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
- `BoxCollider` — for damage zone detection, tag the GameObject **`player`**
- `Movement2D` — auto-discovers collider and sprite renderer, set animation names, audio, and particle references
- `Health2D` — drag in `Movement2D`, `SpriteRenderer`, `Pixelate` component. Assign ticking particle and number spawn point if needed

**Camera GameObject:**
- `CameraFollow2D` — drag player into Follow Target, set Lock X = true
- `CameraController2D` — drag `Pixelate` component and Player in (optional — omit for instant input enable)
- `Pixelate` post-process component

**UI GameObject (separate, not a child of player):**
- `ScreenPanel`
- `HealthHUD2D`
- `HealthBar` (Razor PanelComponent)

**Razor files — place in `ui/` folder:**
- `HealthBar.razor`
- `HealthBar.razor.scss`
- `DamageNumber.razor`
- `DamageNumber.razor.scss`

**Fonts — place in `Assets/fonts/` folder:**
- Font files must be named exactly as you type them in the font field
- e.g. `m6x11.ttf` is referenced as `m6x11`
- s&box resolves fonts by filename — the internal font name embedded in the TTF file does not matter

**Checkpoint — add `Checkpoint2D` to any GameObject:**
- Size the yellow zone to cover the trigger area
- Set `Heal Amount` if you want HP restored on touch

**Damage zones — add `DamageZone2D` alongside a `BoxCollider` (Is Trigger):**
- Shape the collider visually in the inspector
- For complex shapes use multiple child GameObjects each with their own `BoxCollider` and `DamageZone2D`
- Player must be tagged **`player`** for detection to work

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
- `Collider2D` uses trace sweeps — `ITriggerListener` will not fire. Use `DamageZone2D` with a `BoxCollider` trigger for damage detection instead
- The `[Range]` attribute with three arguments shows obsolete warnings — use `[Range(min, max), Step(step)]`
- Floating damage numbers use `TextRenderer` — font is set via the `Number Font` field using the filename of a font in `Assets/fonts/`
- Custom fonts must have their filename match exactly what you type in the font field — s&box resolves by filename not internal font name
- `DamageZone2D` requires the player GameObject to be tagged **`player`** — detection will silently fail without this

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
