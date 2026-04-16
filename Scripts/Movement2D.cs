using Sandbox;

/// <summary>
/// 2D movement controller supporting both Top-Down and Platformer modes.
/// Delegates collision to a Collider2D component on the same GameObject.
/// Axes: X = depth (fixed), Y = left/right, Z = up/down.
/// </summary>
[Title( "2D Movement Controller" )]
[Category( "2D" )]
[Icon( "directions_run" )]
public sealed class Movement2D : Component
{
	// ── Mode ──────────────────────────────────────────────────────────────────

	[Property, Group( "Mode" )]
	[Title( "Platformer Mode" )]
	public bool PlatformerMode { get; set; } = false;

	// ── Movement ─────────────────────────────────────────────────────────────

	[Property, Group( "Movement" )]
	[Range( 0f, 1000f, 10f )]
	public float MoveSpeed { get; set; } = 200f;

	[Property, Group( "Movement" )]
	[Range( 0f, 20f, 0.5f )]
	[Title( "Acceleration (lerp)" )]
	public float Acceleration { get; set; } = 10f;

	// ── Platformer ────────────────────────────────────────────────────────────

	[Property, Group( "Platformer" )]
	[Range( 0f, 5000f, 50f )]
	public float Gravity { get; set; } = 800f;

	[Property, Group( "Platformer" )]
	[Range( 0f, 1000f, 25f )]
	public float JumpForce { get; set; } = 400f;

	[Property, Group( "Platformer" )]
	public bool AllowDoubleJump { get; set; } = false;

	// ── Wall Jump ─────────────────────────────────────────────────────────────

	[Property, Group( "Wall Jump" )]
	[Title( "Enable Wall Jump" )]
	public bool AllowWallJump { get; set; } = true;

	[Property, Group( "Wall Jump" )]
	[Range( 0f, 1000f, 25f )]
	[Title( "Wall Jump Vertical Force" )]
	public float WallJumpForce { get; set; } = 400f;

	[Property, Group( "Wall Jump" )]
	[Range( 0f, 1000f, 25f )]
	[Title( "Wall Kick Horizontal Force" )]
	public float WallKickForce { get; set; } = 300f;

	[Property, Group( "Wall Jump" )]
	[Range( 0f, 1f, 0.05f )]
	[Title( "Wall Kick Duration (s)" )]
	public float WallKickDuration { get; set; } = 0.2f;

	[Property, Group( "Wall Jump" )]
	[Range( 0f, 2f, 0.1f )]
	[Title( "Wall Jump Cooldown (s)" )]
	public float WallJumpCooldown { get; set; } = 0.6f;

	// ── Components ────────────────────────────────────────────────────────────

	[Property, Group( "Components" )]
	public Collider2D Collider { get; set; }

	[Property, Group( "Components" )]
	public SpriteRenderer SpriteRenderer { get; set; }

	// ── Sprite ────────────────────────────────────────────────────────────────

	[Property, Group( "Sprite" )]
	public bool FlipOnMove { get; set; } = true;

	[Property, Group( "Sprite" )]
	[Title( "Sprite Faces Right" )]
	public bool SpriteDirectionRight { get; set; } = true;

	[Property, Group( "Sprite" )]
	[Title( "Idle Animation" )]
	public string IdleAnimation { get; set; } = "idle";

	[Property, Group( "Sprite" )]
	[Title( "Move Animation" )]
	public string MoveAnimation { get; set; } = "move";

	// ── State ─────────────────────────────────────────────────────────────────

	[Property, Group( "State" ), ReadOnly]
	public Vector2 Velocity { get; private set; }

	[Property, Group( "State" ), ReadOnly]
	public bool IsMoving => Velocity.LengthSquared > 1f;

	[Property, Group( "State" ), ReadOnly]
	public bool IsGrounded { get; private set; }

	[Property, Group( "State" ), ReadOnly]
	public bool OnWall { get; private set; }

	/// <summary>-1 = wall on left, 1 = wall on right, 0 = none</summary>
	[Property, Group( "State" ), ReadOnly]
	public int WallDirection { get; private set; }

	private bool  _wasMoving     = false;
	private int   _jumpsUsed     = 0;

	// Wall kick — overrides player horizontal input for WallKickDuration seconds
	// so the kick force carries through instead of being immediately cancelled
	private float _wallKickTimer    = 0f;
	private float _wallKickDir      = 0f;  // -1 = kick left, 1 = kick right

	// Wall jump cooldown — prevents chaining wall jumps by holding into the wall
	private float _wallJumpCooldown = 0f;

	// ─────────────────────────────────────────────────────────────────────────

	protected override void OnStart()
	{
		Collider       ??= Components.Get<Collider2D>();
		SpriteRenderer ??= Components.Get<SpriteRenderer>();
		PlayAnimation( IdleAnimation );
	}

	protected override void OnUpdate()
	{
		if ( PlatformerMode )
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

		if ( Input.Down( "Forward" ) )  input.y += 1f;
		if ( Input.Down( "Backward" ) ) input.y -= 1f;
		if ( Input.Down( "Left" ) )     input.x -= 1f;
		if ( Input.Down( "Right" ) )    input.x += 1f;

		if ( input.LengthSquared > 1f )
			input = input.Normal;

		Velocity = Vector2.Lerp( Velocity, input * MoveSpeed, Time.Delta * Acceleration );

		var delta = new Vector3( 0f, -Velocity.x, Velocity.y ) * Time.Delta;
		Move( delta );
	}

	// ────────────────────────────────────────────────────────────────────────
	// PLATFORMER
	// ────────────────────────────────────────────────────────────────────────

	private void UpdatePlatformer()
	{
		CheckGrounded();
		CheckWall();

		// Count down timers
		if ( _wallKickTimer    > 0f ) _wallKickTimer    -= Time.Delta;
		if ( _wallJumpCooldown > 0f ) _wallJumpCooldown -= Time.Delta;

		// ── Horizontal ───────────────────────────────────────────────────────
		// Player input is blocked during wall kick so the kick carries through
		float horizontal = 0f;
		if ( _wallKickTimer <= 0f )
		{
			if ( Input.Down( "Left" ) )  horizontal -= 1f;
			if ( Input.Down( "Right" ) ) horizontal += 1f;
		}

		float smoothX;
		if ( _wallKickTimer > 0f )
			// Hold kick velocity — no lerp, just maintain the kick speed directly
			smoothX = _wallKickDir * WallKickForce;
		else
			smoothX = MathX.Lerp( Velocity.x, horizontal * MoveSpeed, Time.Delta * Acceleration );

		// ── Vertical ─────────────────────────────────────────────────────────
		float newY = Velocity.y;

		if ( IsGrounded && newY < 0f )
			newY = 0f;

		// ── Jump ─────────────────────────────────────────────────────────────
		if ( Input.Pressed( "Jump" ) )
		{
			if ( AllowWallJump && !IsGrounded && OnWall && _wallJumpCooldown <= 0f )
			{
				// Wall jump — kick up and away from the wall independent of input
				newY               = WallJumpForce;
				_jumpsUsed         = 0;
				_wallKickDir       = -WallDirection;       // away from wall
				_wallKickTimer     = WallKickDuration;
				_wallJumpCooldown  = WallJumpCooldown;     // block next wall jump

				// Apply horizontal kick instantly so it isn't lerp-delayed
				smoothX = _wallKickDir * WallKickForce;
			}
			else
			{
				// Normal or double jump
				int maxJumps = AllowDoubleJump ? 2 : 1;
				if ( IsGrounded || _jumpsUsed < maxJumps )
				{
					newY = JumpForce;
					_jumpsUsed++;
				}
			}
		}

		// ── Gravity ───────────────────────────────────────────────────────────
		if ( !IsGrounded )
			newY -= Gravity * Time.Delta;

		Velocity = new Vector2( smoothX, newY );

		var delta = new Vector3( 0f, -Velocity.x, Velocity.y ) * Time.Delta;
		Move( delta );
	}

	// ────────────────────────────────────────────────────────────────────────
	// Shared move
	// ────────────────────────────────────────────────────────────────────────

	private void Move( Vector3 delta )
	{
		if ( delta.LengthSquared < 0.0001f ) return;

		if ( Collider is null )
		{
			WorldPosition += delta;
			return;
		}

		var moved = Collider.SweepMove( delta );

		if ( !moved )
		{
			var movedY = Collider.SweepMove( new Vector3( 0f, delta.y, 0f ) );
			var movedZ = Collider.SweepMove( new Vector3( 0f, 0f, delta.z ) );

			if ( !movedY ) Velocity = Velocity.WithX( 0f );
			if ( !movedZ ) Velocity = Velocity.WithY( 0f );
		}
	}

	// ────────────────────────────────────────────────────────────────────────
	// Ground check
	// ────────────────────────────────────────────────────────────────────────

	private void CheckGrounded()
	{
		var tr = Scene.Trace
			.Ray( WorldPosition, WorldPosition + Vector3.Down * 0.5f )
			.WithoutTags( "player", "trigger" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		IsGrounded = tr.Hit;

		if ( IsGrounded )
			_jumpsUsed = 0;
	}

	// ────────────────────────────────────────────────────────────────────────
	// Wall check — traces both left and right in world Y axis
	// Requires wall colliders to have the "wall" tag
	// ────────────────────────────────────────────────────────────────────────

	private void CheckWall()
	{
		// Clear wall state when grounded
		if ( IsGrounded )
		{
			OnWall        = false;
			WallDirection = 0;
			return;
		}

		var origin = WorldPosition;
		var reach  = 2f;

		// S&box axis: Velocity.x maps to -WorldY (see Move delta calculation)
		// So "right" in gameplay = -Y in world space, "left" = +Y in world space
		var trRight = Scene.Trace
			.Ray( origin, origin + new Vector3( 0f, -reach, 0f ) )
			.WithoutTags( "player", "trigger" )
			.WithTag( "wall" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		var trLeft = Scene.Trace
			.Ray( origin, origin + new Vector3( 0f,  reach, 0f ) )
			.WithoutTags( "player", "trigger" )
			.WithTag( "wall" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( trRight.Hit )     { OnWall = true;  WallDirection =  1; }
		else if ( trLeft.Hit ) { OnWall = true;  WallDirection = -1; }
		else                   { OnWall = false; WallDirection =  0; }
	}

	// ────────────────────────────────────────────────────────────────────────
	// Animations
	// ────────────────────────────────────────────────────────────────────────

	private void UpdateAnimation()
	{
		if ( SpriteRenderer is null ) return;

		bool moving = IsMoving;

		if ( moving != _wasMoving )
		{
			_wasMoving = moving;
			PlayAnimation( moving ? MoveAnimation : IdleAnimation );
		}

		SpriteRenderer.PlaybackSpeed = moving
			? MathX.Clamp( Velocity.Length / MoveSpeed, 0.1f, 1f )
			: 1f;
	}

	private void PlayAnimation( string name )
	{
		if ( SpriteRenderer is null || string.IsNullOrEmpty( name ) ) return;
		SpriteRenderer.PlayAnimation( name );
	}

	// ────────────────────────────────────────────────────────────────────────
	// Sprite flip
	// SpriteDirectionRight: true  = art faces right (default, most common)
	//                       false = art faces left, flip logic is inverted
	// ────────────────────────────────────────────────────────────────────────

	private void UpdateSprite()
	{
		if ( !FlipOnMove || SpriteRenderer is null ) return;

		// During wall kick use kick direction for sprite, otherwise use velocity
		float facingDir = _wallKickTimer > 0f ? _wallKickDir : Velocity.x;

		if ( facingDir > 0.1f )
			SpriteRenderer.FlipHorizontal = SpriteDirectionRight;
		else if ( facingDir < -0.1f )
			SpriteRenderer.FlipHorizontal = !SpriteDirectionRight;
	}
}