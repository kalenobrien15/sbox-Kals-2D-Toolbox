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

	// ── Wall Slide ────────────────────────────────────────────────────────────

	[Property, Group( "Wall Slide" )]
	[Title( "Enable Wall Slide" )]
	public bool AllowWallSlide { get; set; } = true;

	[Property, Group( "Wall Slide" )]
	[Range( 0f, 500f, 10f )]
	[Title( "Initial Slide Speed" )]
	/// <summary>How slow the player falls when they first touch the wall.</summary>
	public float WallSlideSpeedMin { get; set; } = 30f;

	[Property, Group( "Wall Slide" )]
	[Range( 0f, 1000f, 10f )]
	[Title( "Maximum Slide Speed" )]
	/// <summary>The fastest the player can fall while sliding — approaches this over time.</summary>
	public float WallSlideSpeedMax { get; set; } = 200f;

	[Property, Group( "Wall Slide" )]
	[Range( 0f, 5f, 0.1f )]
	[Title( "Slide Acceleration Time (s)" )]
	/// <summary>How many seconds it takes to go from min to max slide speed.</summary>
	public float WallSlideAccelTime { get; set; } = 1.5f;

	// ── Animations ────────────────────────────────────────────────────────────

	[Property, Group( "Sprite" )]
	[Title( "Jump Animation" )]
	public string JumpAnimation { get; set; } = "jump";

	[Property, Group( "Sprite" )]
	[Title( "Wall Slide Animation" )]
	public string WallSlideAnimation { get; set; } = "wallslide";

	// ── Components ────────────────────────────────────────────────────────────

	[Property, Group( "Components" )]
	public Collider2D Collider { get; set; }

	[Property, Group( "Components" )]
	public SpriteRenderer SpriteRenderer { get; set; }

	// ── Particles ────────────────────────────────────────────────────────────

	[Property, Group( "Particles" )]
	[Title( "Jump Dust Emitter" )]
	/// <summary>One-shot burst on jump.</summary>
	public ParticleSphereEmitter JumpDustEmitter { get; set; }

	[Property, Group( "Particles" )]
	[Title( "Jump Dust Effect" )]
	/// <summary>The ParticleEffect target for the jump dust emitter.</summary>
	public ParticleEffect JumpDustEffect { get; set; }

	[Property, Group( "Particles" )]
	[Range( 1, 50, 1 )]
	[Title( "Jump Dust Count" )]
	public int JumpDustCount { get; set; } = 10;

	[Property, Group( "Particles" )]
	[Title( "Move Dust Emitter" )]
	/// <summary>Trail emitter — enabled while player is moving on the ground.</summary>
	public ParticleSphereEmitter MoveDustEmitter { get; set; }

	[Property, Group( "Particles" )]
	[Title( "Wall Slide Emitter" )]
	/// <summary>Hand emitter — enabled only while wall sliding.</summary>
	public ParticleSphereEmitter WallSlideEmitter { get; set; }

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

	[Property, Group( "State" ), ReadOnly]
	public bool IsWallSliding { get; private set; }

	/// <summary>-1 = wall on left, 1 = wall on right, 0 = none</summary>
	[Property, Group( "State" ), ReadOnly]
	public int WallDirection { get; private set; }

	private bool   _wasMoving        = false;
	private bool   _isRunning        = false;
	private string _currentAnim      = "";
	private int    _jumpsUsed        = 0;

	// Wall kick timers
	private float _wallKickTimer     = 0f;
	private float _wallKickDir       = 0f;
	private float _wallJumpCooldown  = 0f;

	// Wall slide — tracks how long the player has been on the wall
	// so slide speed can ramp up from min to max over WallSlideAccelTime
	private float _wallSlideTimer    = 0f;

	// Cached emitter rates — lazy cached on first use
	private float _moveDustRate      = -1f;
	private float _wallSlideDustRate = -1f;

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
		UpdateParticles();
	}

	// ────────────────────────────────────────────────────────────────────────
	// TOP DOWN
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

		// Timers
		if ( _wallKickTimer   > 0f ) _wallKickTimer   -= Time.Delta;
		if ( _wallJumpCooldown > 0f ) _wallJumpCooldown -= Time.Delta;

		// ── Wall slide ───────────────────────────────────────────────────────
		IsWallSliding = AllowWallSlide
			&& !IsGrounded
			&& OnWall
			&& Velocity.y < 0f
			&& _wallKickTimer <= 0f;

		if ( IsWallSliding )
			_wallSlideTimer += Time.Delta;
		else
			_wallSlideTimer = 0f;

		// ── Horizontal ───────────────────────────────────────────────────────
		float horizontal = 0f;
		if ( _wallKickTimer <= 0f )
		{
			if ( Input.Down( "Left" ) )  horizontal -= 1f;
			if ( Input.Down( "Right" ) ) horizontal += 1f;
		}

		// Track running state from input directly — Velocity magnitude is too small to use
		_isRunning = horizontal != 0f;

		float smoothX = _wallKickTimer > 0f
			? _wallKickDir * WallKickForce
			: MathX.Lerp( Velocity.x, horizontal * MoveSpeed, Time.Delta * Acceleration );

		// ── Vertical ─────────────────────────────────────────────────────────
		float newY = Velocity.y;

		if ( IsGrounded && newY < 0f )
			newY = 0f;

		// ── Jump ─────────────────────────────────────────────────────────────
		if ( Input.Pressed( "Jump" ) )
		{
			if ( AllowWallJump && !IsGrounded && OnWall && _wallJumpCooldown <= 0f )
			{
				newY              = WallJumpForce;
				_jumpsUsed        = 0;
				_wallKickDir      = -WallDirection;
				_wallKickTimer    = WallKickDuration;
				_wallJumpCooldown = WallJumpCooldown;
				_wallSlideTimer   = 0f;
				smoothX           = _wallKickDir * WallKickForce;
				EmitJumpDust();
			}
			else
			{
				int maxJumps = AllowDoubleJump ? 2 : 1;
				if ( IsGrounded || _jumpsUsed < maxJumps )
				{
					newY = JumpForce;
					_jumpsUsed++;
					EmitJumpDust();
				}
			}
		}

		// ── Gravity / wall slide ──────────────────────────────────────────────
		if ( !IsGrounded )
		{
			if ( IsWallSliding )
			{
				float t          = WallSlideAccelTime > 0f
					? MathX.Clamp( _wallSlideTimer / WallSlideAccelTime, 0f, 1f )
					: 1f;
				float slideSpeed = MathX.Lerp( WallSlideSpeedMin, WallSlideSpeedMax, t );
				var fallen       = newY - Gravity * Time.Delta;
				newY             = fallen < -slideSpeed ? -slideSpeed : fallen;
			}
			else
			{
				newY -= Gravity * Time.Delta;
			}
		}

		Velocity = new Vector2( smoothX, newY );

		var delta = new Vector3( 0f, -Velocity.x, Velocity.y ) * Time.Delta;
		Move( delta );
	}

	// ────────────────────────────────────────────────────────────────────────
	// Move
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
			.Ray( WorldPosition, WorldPosition + Vector3.Down * 0.25f )
			.WithoutTags( "player", "trigger" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		IsGrounded = tr.Hit;

		if ( IsGrounded )
			_jumpsUsed = 0;
	}

	// ────────────────────────────────────────────────────────────────────────
	// Wall check
	// ────────────────────────────────────────────────────────────────────────

	private void CheckWall()
	{
		if ( IsGrounded )
		{
			OnWall        = false;
			WallDirection = 0;
			return;
		}

		var origin = WorldPosition;
		var reach  = 2f;

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
	// Particles
	// ────────────────────────────────────────────────────────────────────────

	private void EmitJumpDust()
	{
		if ( JumpDustEmitter is null || JumpDustEffect is null ) return;
		for ( int i = 0; i < JumpDustCount; i++ )
			JumpDustEmitter.Emit( JumpDustEffect );
	}

	private void UpdateParticles()
	{
		if ( MoveDustEmitter is not null )
		{
			if ( _moveDustRate < 0f )
				_moveDustRate = MoveDustEmitter.Rate.ConstantA;

			var isRunning    = IsGrounded && _isRunning;
			var pf           = MoveDustEmitter.Rate;
			pf.ConstantA     = isRunning ? _moveDustRate : 0f;
			pf.ConstantB     = isRunning ? _moveDustRate : 0f;
			MoveDustEmitter.Rate = pf;

			// Flip local Y to match sprite direction
			// Velocity.x > 0 = moving right, < 0 = moving left
			var mLocalPos = MoveDustEmitter.Transform.LocalPosition;
			var mAbsY     = mLocalPos.y < 0f ? -mLocalPos.y : mLocalPos.y;
			var mFacing   = _wallKickTimer > 0f ? _wallKickDir : Velocity.x;
			MoveDustEmitter.Transform.LocalPosition = mLocalPos.WithY( mFacing > 0f ? -mAbsY : mAbsY );
		}

		if ( WallSlideEmitter is not null )
		{
			if ( _wallSlideDustRate < 0f )
				_wallSlideDustRate = WallSlideEmitter.Rate.ConstantA;

			var wpf          = WallSlideEmitter.Rate;
			wpf.ConstantA    = IsWallSliding ? _wallSlideDustRate : 0f;
			wpf.ConstantB    = IsWallSliding ? _wallSlideDustRate : 0f;
			WallSlideEmitter.Rate = wpf;

			// Flip local Y to match the wall the player is touching
			var wLocalPos = WallSlideEmitter.Transform.LocalPosition;
			var wAbsY     = wLocalPos.y < 0f ? -wLocalPos.y : wLocalPos.y;
			WallSlideEmitter.Transform.LocalPosition = wLocalPos.WithY( WallDirection == 1 ? -wAbsY : wAbsY );
		}
	}

	// ────────────────────────────────────────────────────────────────────────
	// Animations
	// Priority: wallslide > jump/fall > move > idle
	// ────────────────────────────────────────────────────────────────────────

	private void UpdateAnimation()
	{
		if ( SpriteRenderer is null ) return;

		string target;

		if ( IsWallSliding )
			target = WallSlideAnimation;
		else if ( !IsGrounded )
			target = JumpAnimation;
		else if ( IsMoving )
			target = MoveAnimation;
		else
			target = IdleAnimation;

		if ( target != _currentAnim )
		{
			_currentAnim = target;
			PlayAnimation( target );
		}

		SpriteRenderer.PlaybackSpeed = ( IsGrounded && IsMoving )
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
	// ────────────────────────────────────────────────────────────────────────

	private void UpdateSprite()
	{
		if ( !FlipOnMove || SpriteRenderer is null ) return;

		float facingDir = _wallKickTimer > 0f ? _wallKickDir : Velocity.x;

		if ( facingDir > 0.1f )
			SpriteRenderer.FlipHorizontal = SpriteDirectionRight;
		else if ( facingDir < -0.1f )
			SpriteRenderer.FlipHorizontal = !SpriteDirectionRight;
	}
}