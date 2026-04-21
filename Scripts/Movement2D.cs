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
	[Range( 0f, 1000f ), Step( 10f )]
	public float MoveSpeed { get; set; } = 200f;

	[Property, Group( "Movement" )]
	[Range( 0f, 20f ), Step( 0.5f )]
	[Title( "Acceleration (lerp)" )]
	public float Acceleration { get; set; } = 10f;

	// ── Platformer ────────────────────────────────────────────────────────────

	[Property, Group( "Platformer" )]
	[Range( 0f, 5000f ), Step( 50f )]
	public float Gravity { get; set; } = 800f;

	[Property, Group( "Platformer" )]
	[Range( 0f, 1000f ), Step( 25f )]
	public float JumpForce { get; set; } = 400f;

	[Property, Group( "Platformer" )]
	public bool AllowDoubleJump { get; set; } = false;

	// ── Wall Jump ─────────────────────────────────────────────────────────────

	[Property, Group( "Wall Jump" )]
	[Title( "Enable Wall Jump" )]
	public bool AllowWallJump { get; set; } = true;

	[Property, Group( "Wall Jump" )]
	[Range( 0f, 1000f ), Step( 25f )]
	[Title( "Wall Jump Vertical Force" )]
	public float WallJumpForce { get; set; } = 400f;

	[Property, Group( "Wall Jump" )]
	[Range( 0f, 1000f ), Step( 25f )]
	[Title( "Wall Kick Horizontal Force" )]
	public float WallKickForce { get; set; } = 300f;

	[Property, Group( "Wall Jump" )]
	[Range( 0f, 1f ), Step( 0.05f )]
	[Title( "Wall Kick Duration (s)" )]
	public float WallKickDuration { get; set; } = 0.2f;

	[Property, Group( "Wall Jump" )]
	[Range( 0f, 2f ), Step( 0.1f )]
	[Title( "Wall Jump Cooldown (s)" )]
	public float WallJumpCooldown { get; set; } = 0.6f;

	// ── Wall Slide ────────────────────────────────────────────────────────────

	[Property, Group( "Wall Slide" )]
	[Title( "Enable Wall Slide" )]
	public bool AllowWallSlide { get; set; } = true;

	[Property, Group( "Wall Slide" )]
	[Range( 0f, 500f ), Step( 10f )]
	[Title( "Initial Slide Speed" )]
	public float WallSlideSpeedMin { get; set; } = 30f;

	[Property, Group( "Wall Slide" )]
	[Range( 0f, 1000f ), Step( 10f )]
	[Title( "Maximum Slide Speed" )]
	public float WallSlideSpeedMax { get; set; } = 200f;

	[Property, Group( "Wall Slide" )]
	[Range( 0f, 5f ), Step( 0.1f )]
	[Title( "Slide Acceleration Time (s)" )]
	public float WallSlideAccelTime { get; set; } = 1.5f;

	// ── Animations ────────────────────────────────────────────────────────────

	[Property, Group( "Sprite" )]
	[Title( "Jump Animation" )]
	public string JumpAnimation { get; set; } = "jump";

	[Property, Group( "Sprite" )]
	[Title( "Wall Slide Animation" )]
	public string WallSlideAnimation { get; set; } = "wallslide";

	[Property, Group( "Sprite" )]
	[Title( "Death Animation" )]
	public string DeathAnimation { get; set; } = "death";

	[Property, Group( "Sprite" )]
	[Title( "Death Ground Animation" )]
	public string DeathGroundAnimation { get; set; } = "deathground";

	// ── Components ────────────────────────────────────────────────────────────

	[Property, Group( "Components" )]
	public Collider2D Collider { get; set; }

	[Property, Group( "Components" )]
	public SpriteRenderer SpriteRenderer { get; set; }

	// ── Particles ────────────────────────────────────────────────────────────

	[Property, Group( "Particles" )]
	[Title( "Jump Dust Emitter" )]
	public ParticleSphereEmitter JumpDustEmitter { get; set; }

	[Property, Group( "Particles" )]
	[Title( "Jump Dust Effect" )]
	public ParticleEffect JumpDustEffect { get; set; }

	[Property, Group( "Particles" )]
	[Range( 1, 50 ), Step( 1 )]
	[Title( "Jump Dust Count" )]
	public int JumpDustCount { get; set; } = 10;

	[Property, Group( "Particles" )]
	[Title( "Move Dust Emitter" )]
	public ParticleSphereEmitter MoveDustEmitter { get; set; }

	[Property, Group( "Particles" )]
	[Title( "Wall Slide Emitter" )]
	public ParticleSphereEmitter WallSlideEmitter { get; set; }

	// ── Audio ────────────────────────────────────────────────────────────────

	[Property, Group( "Audio" )]
	[Title( "Jump Sound" )]
	public SoundEvent JumpSound { get; set; }

	[Property, Group( "Audio" )]
	[Title( "Wall Jump Sound" )]
	public SoundEvent WallJumpSound { get; set; }

	[Property, Group( "Audio" )]
	[Title( "Land Sound" )]
	public SoundEvent LandSound { get; set; }

	[Property, Group( "Audio" )]
	[Title( "Heavy Land Sound" )]
	/// <summary>Played when landing after a large fall. Triggered by Fall Speed Threshold.</summary>
	public SoundEvent HeavyLandSound { get; set; }

	[Property, Group( "Audio" )]
	[Range( 0f, 2000f ), Step( 50f )]
	[Title( "Heavy Land Speed Threshold" )]
	/// <summary>Downward velocity required to trigger the heavy land sound.</summary>
	public float HeavyLandThreshold { get; set; } = 600f;

	[Property, Group( "Audio" )]
	[Title( "Wall Slide Sound" )]
	public SoundEvent WallSlideSound { get; set; }

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

	[Property, Group( "State" ), ReadOnly]
	public bool IsDead { get; private set; }

	/// <summary>-1 = wall on left, 1 = wall on right, 0 = none</summary>
	[Property, Group( "State" ), ReadOnly]
	public int WallDirection { get; private set; }

	/// <summary>Set to true by CameraController2D once the intro completes.</summary>
	public bool InputAllowed { get; set; } = false;

	/// <summary>Current facing direction — 1 = right, -1 = left.</summary>
	public float FacingDirection { get; private set; } = 1f;

	private bool   _isRunning        = false;
	private bool   _isDyingInAir    = false;
	private string _currentAnim      = "";
	private int    _jumpsUsed        = 0;

	private float  _wallKickTimer    = 0f;
	private float  _wallKickDir      = 0f;
	private float  _wallJumpCooldown = 0f;
	private float  _wallSlideTimer   = 0f;

	// Knockback — sprite flip is locked for this duration after a hit
	private float  _knockbackTimer   = 0f;

	// Audio state
	private bool   _wasGrounded         = false;
	private float  _peakFallSpeed       = 0f;
	private bool   _isWallSliding_Audio = false;
	private bool   _jumpedThisFrame     = false; // prevents land sound on same frame as jump
	private int    _airborneFrames      = 0;     // must be airborne for N frames before land sound fires
	private SoundHandle _wallSlideSoundHandle;

	private float  _moveDustRate      = -1f;
	private float  _wallSlideDustRate = -1f;

	// ─────────────────────────────────────────────────────────────────────────

	protected override void OnStart()
	{
		InputAllowed   = false;
		Collider       ??= Components.Get<Collider2D>();
		SpriteRenderer ??= Components.Get<SpriteRenderer>();
		PlayAnimation( IdleAnimation );


	}



	protected override void OnUpdate()
	{
		if ( IsDead ) return;

		if ( PlatformerMode && InputAllowed )
			UpdatePlatformer();
		else if ( PlatformerMode && _isDyingInAir )
			UpdatePlatformerDead(); // physics only, no input
		else if ( InputAllowed )
			UpdateTopDown();

		UpdateSprite();
		UpdateAnimation();
		UpdateParticles();
		UpdateAudio();
	}

	// ────────────────────────────────────────────────────────────────────────
	// PLATFORMER DEAD — physics only, no input
	// ────────────────────────────────────────────────────────────────────────

	private void UpdatePlatformerDead()
	{
		CheckGrounded();

		float newY = Velocity.y;

		if ( IsGrounded && newY < 0f )
			newY = 0f;

		if ( !IsGrounded )
			newY -= Gravity * Time.Delta;

		Velocity = new Vector2( Velocity.x, newY );

		var delta = new Vector3( 0f, -Velocity.x, Velocity.y ) * Time.Delta;
		Move( delta );
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

		if ( _wallKickTimer    > 0f ) _wallKickTimer    -= Time.Delta;
		if ( _wallJumpCooldown > 0f ) _wallJumpCooldown -= Time.Delta;

		if ( _knockbackTimer > 0f )
			_knockbackTimer -= Time.Delta;

		IsWallSliding = AllowWallSlide
			&& !IsGrounded
			&& OnWall
			&& Velocity.y < 0f
			&& _wallKickTimer <= 0f;

		if ( IsWallSliding )
		{
			_wallSlideTimer += Time.Delta;

			// If knocked into a wall, correct facing to look away from the wall
			// WallDirection 1 = right wall, player should face left (-1)
			// WallDirection -1 = left wall, player should face right (1)
			if ( _knockbackTimer > 0f )
			{
				_knockbackTimer = 0f; // clear knockback lock
				FacingDirection = -WallDirection;
				if ( SpriteRenderer is not null )
				{
					// WallDirection 1 = right wall = face left = FlipHorizontal matches facing left in UpdateSprite
					// WallDirection -1 = left wall = face right = FlipHorizontal matches facing right in UpdateSprite
					SpriteRenderer.FlipHorizontal = WallDirection == 1
						? SpriteDirectionRight   // right wall = face left = FlipHorizontal true
						: !SpriteDirectionRight; // left wall = face right = FlipHorizontal false
				}
			}
		}
		else
			_wallSlideTimer = 0f;

		float horizontal = 0f;
		if ( _wallKickTimer <= 0f )
		{
			if ( Input.Down( "Left" ) )  horizontal -= 1f;
			if ( Input.Down( "Right" ) ) horizontal += 1f;
		}

		_isRunning = horizontal != 0f;

		float smoothX = _wallKickTimer > 0f
			? _wallKickDir * WallKickForce
			: MathX.Lerp( Velocity.x, horizontal * MoveSpeed, Time.Delta * Acceleration );

		float newY = Velocity.y;

		if ( IsGrounded && newY < 0f )
			newY = 0f;

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
				_jumpedThisFrame = true;
				EmitJumpDust();
				if ( WallJumpSound is not null )
					Sound.Play( WallJumpSound, WorldPosition, 0f );
			}
			else
			{
				int maxJumps = AllowDoubleJump ? 2 : 1;
				if ( IsGrounded || _jumpsUsed < maxJumps )
				{
					newY = JumpForce;
					_jumpsUsed++;
					_jumpedThisFrame = true;
					EmitJumpDust();
					if ( JumpSound is not null )
						Sound.Play( JumpSound, WorldPosition, 0f );
				}
			}
		}

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
	// Public API — called by Health2D
	// ────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Apply a velocity impulse — used for knockback.
	/// Locks sprite flip direction until the player lands.
	/// </summary>
	public void ApplyImpulse( Vector2 impulse )
	{
		Velocity        = impulse;
		_knockbackTimer = 0.4f;

		// Face toward the attacker (opposite of knockback direction)
		if ( impulse.x > 0.1f )
		{
			FacingDirection = -1f;
			if ( SpriteRenderer is not null )
				SpriteRenderer.FlipHorizontal = !SpriteDirectionRight;
		}
		else if ( impulse.x < -0.1f )
		{
			FacingDirection = 1f;
			if ( SpriteRenderer is not null )
				SpriteRenderer.FlipHorizontal = SpriteDirectionRight;
		}
	}

	/// <summary>
	/// Trigger the death state — disables input and plays death animation.
	/// </summary>
	public void TriggerDeathAir()
	{
		// Block ALL input including movement, keep physics/gravity running
		InputAllowed  = false;
		_isDyingInAir = true;
		// Don't play death anim yet — let knockback play out naturally
		// Ground death anim plays when they land
	}

	public void TriggerDeath()
	{
		IsDead          = true;
		_isDyingInAir   = false;
		InputAllowed    = false;
		_knockbackTimer = 0f;
		// Don't zero velocity here — let gravity settle naturally
		_currentAnim    = "";
		PlayAnimation( DeathGroundAnimation );
	}

	/// <summary>
	/// Reset the movement state for respawn.
	/// </summary>
	public void TriggerRespawn()
	{
		IsDead          = false;
		_isDyingInAir   = false;
		_knockbackTimer = 0f;
		Velocity       = Vector2.Zero;
		_jumpsUsed     = 0;
		_currentAnim   = "";
		PlayAnimation( IdleAnimation );
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
	// Ground / Wall checks
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

			var isRunning = IsGrounded && _isRunning;
			var pf        = MoveDustEmitter.Rate;
			pf.ConstantA  = isRunning ? _moveDustRate : 0f;
			pf.ConstantB  = isRunning ? _moveDustRate : 0f;
			MoveDustEmitter.Rate = pf;

			var mLocalPos = MoveDustEmitter.LocalPosition;
			var mAbsY     = mLocalPos.y < 0f ? -mLocalPos.y : mLocalPos.y;
			var mFacing   = _wallKickTimer > 0f ? _wallKickDir : Velocity.x;
			MoveDustEmitter.LocalPosition = mLocalPos.WithY( mFacing > 0f ? -mAbsY : mAbsY );
		}

		if ( WallSlideEmitter is not null )
		{
			if ( _wallSlideDustRate < 0f )
				_wallSlideDustRate = WallSlideEmitter.Rate.ConstantA;

			var wpf       = WallSlideEmitter.Rate;
			wpf.ConstantA = IsWallSliding ? _wallSlideDustRate : 0f;
			wpf.ConstantB = IsWallSliding ? _wallSlideDustRate : 0f;
			WallSlideEmitter.Rate = wpf;

			var wLocalPos = WallSlideEmitter.LocalPosition;
			var wAbsY     = wLocalPos.y < 0f ? -wLocalPos.y : wLocalPos.y;
			WallSlideEmitter.LocalPosition = wLocalPos.WithY( WallDirection == 1 ? -wAbsY : wAbsY );
		}
	}

	// ────────────────────────────────────────────────────────────────────────
	// Audio
	// ────────────────────────────────────────────────────────────────────────

	private void UpdateAudio()
	{
		// Track peak fall speed while airborne for heavy land detection
		if ( !IsGrounded )
		{
			var downSpeed = -Velocity.y; // positive = falling
			if ( downSpeed > _peakFallSpeed )
				_peakFallSpeed = downSpeed;
		}

		// Count how many consecutive frames the player has been airborne
		if ( !IsGrounded )
			_airborneFrames++;

		// Landing detection — check BEFORE resetting airborne counter
		if ( IsGrounded && !_wasGrounded && !_jumpedThisFrame && _airborneFrames >= 3 )
		{
			if ( _peakFallSpeed >= HeavyLandThreshold && HeavyLandSound is not null )
				Sound.Play( HeavyLandSound, WorldPosition, 0f );
			else if ( LandSound is not null )
				Sound.Play( LandSound, WorldPosition, 0f );

			_peakFallSpeed = 0f;
		}

		// Reset counters after landing check
		if ( IsGrounded )
			_airborneFrames = 0;

		_jumpedThisFrame = false;

		// Wall slide sound — start/stop based on IsWallSliding
		if ( IsWallSliding && !_isWallSliding_Audio )
		{
			_isWallSliding_Audio = true;
			if ( WallSlideSound is not null )
				_wallSlideSoundHandle = Sound.Play( WallSlideSound, WorldPosition, 0f );
		}
		else if ( !IsWallSliding && _isWallSliding_Audio )
		{
			_isWallSliding_Audio = false;
			_wallSlideSoundHandle.Stop();
		}

		// Update wall slide sound position to follow player
		if ( _isWallSliding_Audio )
			_wallSlideSoundHandle.Position = WorldPosition;

		_wasGrounded = IsGrounded;
	}

	// ────────────────────────────────────────────────────────────────────────
	// Animations
	// ────────────────────────────────────────────────────────────────────────

	private void UpdateAnimation()
	{
		if ( SpriteRenderer is null ) return;

		// Don't override death animations
		if ( _isDyingInAir ) return;

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

		// During knockback or when no input — keep current facing, don't update
		if ( _knockbackTimer > 0f ) return;

		// Only update facing from input-driven movement, not residual velocity
		float facingDir = _wallKickTimer > 0f ? _wallKickDir : ( _isRunning ? Velocity.x : 0f );

		if ( facingDir > 0.1f )
		{
			FacingDirection = 1f;
			SpriteRenderer.FlipHorizontal = SpriteDirectionRight;
		}
		else if ( facingDir < -0.1f )
		{
			FacingDirection = -1f;
			SpriteRenderer.FlipHorizontal = !SpriteDirectionRight;
		}
		// If facingDir is 0 (no input) we don't update — sprite stays in last direction
	}
}