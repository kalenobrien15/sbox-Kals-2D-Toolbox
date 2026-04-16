using Sandbox;

/// <summary>
/// 2D movement controller supporting both Top-Down and Platformer modes.
/// Delegates collision to a Collider2D component on the same GameObject.
/// Axes: X = depth (fixed), Y = left/right, Z = up/down.

[Title("2D Movement Controller")]
[Category("2D")]
[Icon("directions_run")]
public sealed class Movement2D : Component
{
    // ── Mode

    [Property, Group("Mode")]
    [Title("Platformer Mode")]
    public bool PlatformerMode { get; set; } = false;

    // ── Movement 

    [Property, Group("Movement")]
    [Range(0f, 1000f, 10f)]
    public float MoveSpeed { get; set; } = 200f;

    [Property, Group("Movement")]
    [Range(0f, 20f, 0.5f)]
    [Title("Acceleration (lerp)")]
    public float Acceleration { get; set; } = 10f;

    // ── Platformer 

    [Property, Group("Platformer")]
    [Range(0f, 5000f, 50f)]
    public float Gravity { get; set; } = 800f;

    [Property, Group("Platformer")]
    [Range(0f, 1000f, 25f)]
    public float JumpForce { get; set; } = 400f;

    [Property, Group("Platformer")]
    public bool AllowDoubleJump { get; set; } = false;

     
    // ── Components 

    [Property, Group("Components")]
    public Collider2D Collider { get; set; }

    [Property, Group("Components")]
    public SpriteRenderer SpriteRenderer { get; set; }

    // ── Sprite

    [Property, Group("Sprite")]
    public bool FlipOnMove { get; set; } = true;
    public bool SpriteDirectionRight { get; set; } = true;
    [Property, Group("Sprite")]
    [Title("Idle Animation")]
    public string IdleAnimation { get; set; } = "idle";

    [Property, Group("Sprite")]
    [Title("Move Animation")]
    public string MoveAnimation { get; set; } = "move";

    // ── State 

    [Property, Group("State"), ReadOnly]
    public Vector2 Velocity { get; private set; }

    [Property, Group("State"), ReadOnly]
    public bool IsMoving => Velocity.LengthSquared > 1f;

    [Property, Group("State"), ReadOnly]
    public bool IsGrounded { get; private set; }

    private bool _wasMoving = false;
    private int _jumpsUsed = 0;

    // ─────────────────────────────────────────────────────────────────────────

    protected override void OnStart()
    {
        Collider ??= Components.Get<Collider2D>();
        SpriteRenderer ??= Components.Get<SpriteRenderer>();
        PlayAnimation(IdleAnimation);
    }

    protected override void OnUpdate()
    {
        if (PlatformerMode)
            UpdatePlatformer();
        else
            UpdateTopDown();

        UpdateSprite();
        UpdateAnimation();
    }

    // ────────────────────────────────────────────────────────────────────────
    // TOP DOWN — WASD moves in all four directions
    // ────────────────────────────────────────────────────────────────────────

    private void UpdateTopDown()
    {
        var input = Vector2.Zero;

        if (Input.Down("Forward")) input.y += 1f;
        if (Input.Down("Backward")) input.y -= 1f;
        if (Input.Down("Left")) input.x -= 1f;
        if (Input.Down("Right")) input.x += 1f;

        if (input.LengthSquared > 1f)
            input = input.Normal;

        Velocity = Vector2.Lerp(Velocity, input * MoveSpeed, Time.Delta * Acceleration);

        var delta = new Vector3(0f, -Velocity.x, Velocity.y) * Time.Delta;
        Move(delta);
    }

    // ────────────────────────────────────────────────────────────────────────
    // PLATFORMER — A/D move horizontally, W/Space to jump, gravity on Z
    // ────────────────────────────────────────────────────────────────────────

    private void UpdatePlatformer()
    {
        // Horizontal input
        float horizontal = 0f;
        if (Input.Down("Left")) horizontal -= 1f;
        if (Input.Down("Right")) horizontal += 1f;

        float targetX = horizontal * MoveSpeed;
        float smoothX = MathX.Lerp(Velocity.x, targetX, Time.Delta * Acceleration);

        // Gravity on Y (maps to world Z)
        float newY = Velocity.y;

        if (IsGrounded && newY < 0f)
        {
            newY = 0f;
            _jumpsUsed = 0;
        }

        // Jump
        if (Input.Pressed("Jump"))
        {
            int maxJumps = AllowDoubleJump ? 2 : 1;
            if (IsGrounded || _jumpsUsed < maxJumps)
            {
                newY = JumpForce;
                _jumpsUsed++;
            }
        }

        // Apply gravity when airborne
        if (!IsGrounded)
            newY -= Gravity * Time.Delta;

        Velocity = new Vector2(smoothX, newY);

        // Move — X drives left/right (world Y), Y drives up/down (world Z)
        var delta = new Vector3(0f, -Velocity.x, Velocity.y) * Time.Delta;

        // Check grounded before moving so IsGrounded is accurate next frame
        CheckGrounded();
        Move(delta);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Shared move — delegates to Collider2D sweep, falls back to direct move.
    // ────────────────────────────────────────────────────────────────────────

    private void Move(Vector3 delta)
    {
        if (delta.LengthSquared < 0.0001f) return;

        if (Collider is null)
        {
            WorldPosition += delta;
            return;
        }

        var moved = Collider.SweepMove(delta);

        if (!moved)
        {
            var movedY = Collider.SweepMove(new Vector3(0f, delta.y, 0f));
            var movedZ = Collider.SweepMove(new Vector3(0f, 0f, delta.z));

            if (!movedY) Velocity = Velocity.WithX(0f);
            if (!movedZ) Velocity = Velocity.WithY(0f);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Ground check for platformer — short downward trace below the collider
    // ────────────────────────────────────────────────────────────────────────

    private void CheckGrounded()
    {
        var origin = WorldPosition;
        var end = origin + Vector3.Down * 4f;

        var tr = Scene.Trace
            .Ray(origin, end)
            .WithoutTags("player", "trigger")
            .IgnoreGameObjectHierarchy(GameObject)
            .Run();

        IsGrounded = tr.Hit;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Animations
    // ────────────────────────────────────────────────────────────────────────

    private void UpdateAnimation()
    {
        if (SpriteRenderer is null) return;

        bool moving = IsMoving;

        if (moving != _wasMoving)
        {
            _wasMoving = moving;
            PlayAnimation(moving ? MoveAnimation : IdleAnimation);
        }

        if (moving)
        {
            float speedFraction = Velocity.Length / MoveSpeed;
            SpriteRenderer.PlaybackSpeed = MathX.Clamp(speedFraction, 0.1f, 1f);
        }
        else
        {
            SpriteRenderer.PlaybackSpeed = 1f;
        }
    }

    private void PlayAnimation(string name)
    {
        if (SpriteRenderer is null || string.IsNullOrEmpty(name)) return;
        SpriteRenderer.PlayAnimation(name);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Sprite flip
	// I setup my sprites facing right if you are using sprites facing left swap false and true bools in Fliphorizontal =X
    // ────────────────────────────────────────────────────────────────────────

   
    private void UpdateSprite()
    {
        if(!SpriteDirectionRight){
        if (!FlipOnMove || SpriteRenderer is null) return;

        if (Velocity.x > 0.1f)
            SpriteRenderer.FlipHorizontal = false;
        else if (Velocity.x < -0.1f)
            SpriteRenderer.FlipHorizontal = true;
        }
        else if ( SpriteDirectionRight )
        {
              if (!FlipOnMove || SpriteRenderer is null) return;

        if (Velocity.x > 0.1f)
            SpriteRenderer.FlipHorizontal = true;
        else if (Velocity.x < -0.1f)
            SpriteRenderer.FlipHorizontal = false;
        }
    }
}
