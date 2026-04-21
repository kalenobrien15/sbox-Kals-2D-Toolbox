using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Manages HP, damage types, iframes, knockback, death sequence, respawn,
/// sprite flash, floating damage numbers, and HUD health bar.
///
/// Requires Movement2D on the same GameObject.
/// Place this file in your Code/ folder.
/// </summary>
[Title( "Health 2D" )]
[Category( "2D" )]
[Icon( "favorite" )]
public sealed class Health2D : Component
{
	// ── Health ────────────────────────────────────────────────────────────────

	[Property, Group( "Health" )]
	[Range( 1f, 1000f ), Step( 1f  )]
	public float MaxHealth { get; set; } = 100f;

	[Property, Group( "Health" ), ReadOnly]
	public float CurrentHealth { get; private set; }

	// ── Iframes ───────────────────────────────────────────────────────────────

	[Property, Group( "Iframes" )]
	[Range( 0f, 5f ), Step( 0.1f  )]
	[Title( "Invincibility Duration (s)" )]
	public float IframeDuration { get; set; } = 1f;

	[Property, Group( "Iframes" )]
	[Title( "Flash On Damage" )]
	public bool FlashOnDamage { get; set; } = true;

	[Property, Group( "Iframes" )]
	[Title( "Flash Color" )]
	public Color FlashColor { get; set; } = Color.Red;

	[Property, Group( "Iframes" )]
	[Range( 0.05f, 0.5f ), Step( 0.05f  )]
	[Title( "Flash Interval (s)" )]
	public float FlashInterval { get; set; } = 0.1f;

	// ── Knockback ─────────────────────────────────────────────────────────────

	[Property, Group( "Knockback" )]
	[Title( "Enable Knockback" )]
	public bool EnableKnockback { get; set; } = true;

	[Property, Group( "Knockback" )]
	[Range( 0f, 1000f ), Step( 25f  )]
	[Title( "Knockback Horizontal Force" )]
	public float KnockbackForce { get; set; } = 300f;

	[Property, Group( "Knockback" )]
	[Range( 0f, 1000f ), Step( 25f  )]
	[Title( "Knockback Vertical Force (Platformer)" )]
	public float KnockbackVerticalForce { get; set; } = 200f;

	// ── Ticking Damage ────────────────────────────────────────────────────────

	[Property, Group( "Ticking Damage" )]
	[Title( "Ticking Damage Particle" )]
	/// <summary>Child ParticleEffect to activate during ticking damage (e.g. fire/poison).</summary>
	public ParticleEffect TickingParticle { get; set; }

	// ── Death & Respawn ───────────────────────────────────────────────────────

	[Property, Group( "Death & Respawn" )]
	[Title( "Camera Pixelate Component" )]
	public Pixelate PixelateComponent { get; set; }

	[Property, Group( "Death & Respawn" )]
	[Range( 0f, 5f ), Step( 0.1f  )]
	[Title( "Death Animation Hold (s)" )]
	/// <summary>How long to wait after death anim starts before pixelation begins.</summary>
	public float DeathAnimHoldDuration { get; set; } = 1f;

	[Property, Group( "Death & Respawn" )]
	[Range( 0.1f, 5f ), Step( 0.1f  )]
	[Title( "Pixelate In Speed" )]
	/// <summary>How fast the screen pixelates on death (scale per second).</summary>
	public float PixelateInSpeed { get; set; } = 2f;

	[Property, Group( "Death & Respawn" )]
	[Range( 0f, 3f ), Step( 0.1f  )]
	[Title( "Blackout Hold Duration (s)" )]
	/// <summary>How long to hold at full pixelation before respawning.</summary>
	public float BlackoutHoldDuration { get; set; } = 0.5f;

	[Property, Group( "Death & Respawn" )]
	[Range( 0.1f, 5f ), Step( 0.1f  )]
	[Title( "Pixelate Out Speed" )]
	/// <summary>How fast the screen unpixelates after respawn.</summary>
	public float PixelateOutSpeed { get; set; } = 2f;

	// ── Regen ─────────────────────────────────────────────────────────────────

	[Property, Group( "Regen" )]
	[Title( "Enable Checkpoint Regen" )]
	public bool RegenOnCheckpoint { get; set; } = true;

	[Property, Group( "Regen" )]
	[Title( "Enable Respawn Full Heal" )]
	public bool FullHealOnRespawn { get; set; } = true;

	// ── Floating Numbers ──────────────────────────────────────────────────────

	[Property, Group( "Floating Numbers" )]
	[Title( "Enable Damage Numbers" )]
	public bool ShowDamageNumbers { get; set; } = true;

	[Property, Group( "Floating Numbers" )]
	[Range( 50f, 300f ), Step( 10f  )]
	[Title( "Float Speed" )]
	public float NumberFloatSpeed { get; set; } = 80f;

	[Property, Group( "Floating Numbers" )]
	[Range( 0.3f, 3f ), Step( 0.1f  )]
	[Title( "Number Lifetime (s)" )]
	public float NumberLifetime { get; set; } = 1f;

	[Property, Group( "Floating Numbers" )]
	[Title( "Number Spawn Point" )]
	/// <summary>Child GameObject to use as the spawn origin for damage numbers. If unset defaults to player position + 20 units up.</summary>
	public GameObject NumberSpawnPoint { get; set; }

	[Property, Group( "Floating Numbers" )]
	[Title( "Number Font" )]
	public string NumberFont { get; set; } = "Roboto";

	[Property, Group( "Floating Numbers" )]
	[Range( 8f, 64f ), Step( 1f )]
	[Title( "Number Font Size" )]
	public float NumberFontSize { get; set; } = 18f;

	// ── Components ────────────────────────────────────────────────────────────

	[Property, Group( "Components" )]
	public Movement2D Movement { get; set; }

	[Property, Group( "Components" )]
	public SpriteRenderer SpriteRenderer { get; set; }

	// ── Audio ────────────────────────────────────────────────────────────────

	[Property, Group( "Audio" )]
	[Title( "Hurt Sound (Flat Damage)" )]
	/// <summary>Plays on flat damage hit and on the first hit of ticking damage.</summary>
	public SoundEvent HurtSound { get; set; }

	[Property, Group( "Audio" )]
	[Title( "Ticking Damage Sound" )]
	/// <summary>Plays on every tick of ticking damage.</summary>
	public SoundEvent TickingSound { get; set; }

	// ── Events ────────────────────────────────────────────────────────────────

	/// <summary>Fired when damage is taken. float = damage amount.</summary>
	public Action<float> OnDamaged { get; set; }

	/// <summary>Fired when health is restored. float = amount restored.</summary>
	public Action<float> OnHealed { get; set; }

	/// <summary>Fired on death.</summary>
	public Action OnDeath { get; set; }

	/// <summary>Fired after respawn is complete.</summary>
	public Action OnRespawned { get; set; }

	// ── Internal state ────────────────────────────────────────────────────────

	private float   _iframeTimer       = 0f;
	private bool    _isInvincible      = false;
	private bool    _isDead            = false;
	private bool    _isDeathSequence   = false;
	private bool    _pendingDeath      = false;
	private Color   _originalColor     = Color.White;

	// Ticking damage
	private bool    _isTicking         = false;
	private float   _tickDamage        = 0f;
	private float   _tickInterval      = 0f;
	private float   _tickDuration      = 0f;
	private float   _tickTimer         = 0f;
	private float   _tickDurationTimer = 0f;

	// Checkpoint
	private Vector3 _checkpointPosition;
	private bool    _hasCheckpoint = false;

	// Floating numbers (world-space text via Panel)
	private readonly List<FloatingNumber> _floatingNumbers = new();

	// ─────────────────────────────────────────────────────────────────────────

	protected override void OnStart()
	{
		Movement       ??= Components.Get<Movement2D>();
		SpriteRenderer ??= Components.Get<SpriteRenderer>();

		CurrentHealth = MaxHealth;

		if ( SpriteRenderer is not null )
			_originalColor = SpriteRenderer.OverlayColor;

		_checkpointPosition = WorldPosition;

		// Ensure ticking particle is off until damage is applied
		if ( TickingParticle is not null )
			TickingParticle.Enabled = false;

		// Push initial health values to HUD
		UpdateHUD();
	}

	protected override void OnUpdate()
	{
		// Floating numbers always update regardless of death state
		UpdateFloatingNumbers();

		if ( _isDeathSequence ) return;

		UpdateIframes();
		UpdateTickingDamage();
	}

	// ── Iframe flash ──────────────────────────────────────────────────────────

	private float _flashTimer = 0f;
	private bool  _flashState = false;

	private void UpdateIframes()
	{
		if ( !_isInvincible ) return;

		_iframeTimer -= Time.Delta;

		if ( FlashOnDamage && SpriteRenderer is not null )
		{
			_flashTimer -= Time.Delta;
			if ( _flashTimer <= 0f )
			{
				_flashState = !_flashState;
				_flashTimer = FlashInterval;
				SpriteRenderer.OverlayColor = _flashState ? FlashColor : _originalColor;
			}
		}

		if ( _iframeTimer <= 0f )
		{
			_isInvincible = false;
			if ( SpriteRenderer is not null )
				SpriteRenderer.OverlayColor = _originalColor;
		}
	}

	// ── Ticking damage ────────────────────────────────────────────────────────

	private void UpdateTickingDamage()
	{
		if ( !_isTicking ) return;

		_tickDurationTimer -= Time.Delta;
		_tickTimer         -= Time.Delta;

		if ( _tickTimer <= 0f )
		{
			_tickTimer = _tickInterval;
			if ( TickingSound is not null )
				Sound.Play( TickingSound, WorldPosition, 0f );
			ApplyDamageInternal( _tickDamage, DamageType.Ticking, iframes: false );
		}

		if ( _tickDurationTimer <= 0f )
			StopTickingDamage();
	}

	private void StopTickingDamage()
	{
		_isTicking = false;
		if ( TickingParticle is not null )
			TickingParticle.Enabled = false;
	}

	// ── Floating numbers ──────────────────────────────────────────────────────

	private void UpdateFloatingNumbers()
	{
		for ( int i = _floatingNumbers.Count - 1; i >= 0; i-- )
		{
			var fn    = _floatingNumbers[i];
			fn.Timer -= Time.Delta;
			fn.WorldPos += Vector3.Up * NumberFloatSpeed * Time.Delta;

			if ( fn.GO is not null && fn.GO.IsValid() )
			{
				fn.GO.WorldPosition = fn.WorldPos;


			}

			if ( fn.Timer <= 0f )
			{
				fn.GO?.Destroy();
				_floatingNumbers.RemoveAt( i );
			}
		}
	}

	private void SpawnFloatingNumber( float damage, DamageType type )
	{
		if ( !ShowDamageNumbers ) return;

		var colorStr = type == DamageType.Ticking ? "orange" : "white";
		var color    = type == DamageType.Ticking ? Color.Orange : Color.White;
		var spawnPos = NumberSpawnPoint is not null
			? NumberSpawnPoint.WorldPosition
			: WorldPosition + Vector3.Up * 20f;

		var go           = new GameObject( true, "DmgNumber" );
		go.Parent        = Scene;
		go.WorldPosition = spawnPos;

		var tr              = go.AddComponent<TextRenderer>();
		tr.Text             = $"-{(int)damage}";
		tr.FontSize         = NumberFontSize;
		tr.Color            = color;
		tr.FontFamily       = NumberFont;
		tr.FontWeight       = 700;
		tr.Scale            = 0.1f;

		_floatingNumbers.Add( new FloatingNumber
		{
			WorldPos = spawnPos,
			Timer    = NumberLifetime,
			Color    = color,
			ColorStr = colorStr,
			Damage   = $"-{(int)damage}",
			GO       = go,
			TR       = tr
		} );
	}

	// ── Public damage API ─────────────────────────────────────────────────────

	public enum DamageType { Flat, Ticking }

	/// <summary>
	/// Apply flat damage with optional knockback from an attacker position.
	/// </summary>
	public void TakeDamage( float amount, Vector3? attackerPosition = null )
	{
		if ( _isDead || _isInvincible ) return;

		if ( HurtSound is not null )
			Sound.Play( HurtSound, WorldPosition, 0f );

		_pendingDeath = false;
		ApplyDamageInternal( amount, DamageType.Flat, iframes: true );

		// Apply knockback before checking death so velocity carries on fatal hits
		ApplyKnockback( attackerPosition );

		if ( _pendingDeath )
			_ = TriggerDeathSequence();
	}

	/// <summary>
	/// Apply ticking damage (fire/poison). Restarts timer if already ticking.
	/// </summary>
	public void ApplyTickingDamage( float damagePerTick, float tickInterval, float totalDuration )
	{
		if ( _isDead ) return;

		_tickDamage        = damagePerTick;
		_tickInterval      = tickInterval;
		_tickDuration      = totalDuration;
		_tickDurationTimer = totalDuration;
		_tickTimer         = tickInterval; // first tick after one interval

		if ( !_isTicking )
		{
			// First hit triggers iframes and hurt sound
			_isInvincible = true;
			_iframeTimer  = IframeDuration;
			_flashTimer   = 0f;
			_flashState   = true;
			if ( FlashOnDamage && SpriteRenderer is not null )
				SpriteRenderer.OverlayColor = FlashColor;

			if ( HurtSound is not null )
				Sound.Play( HurtSound, WorldPosition, 0f );
		}

		_isTicking = true;

		if ( TickingParticle is not null )
			TickingParticle.Enabled = true;
	}

	/// <summary>
	/// Restore health. Clamped to MaxHealth.
	/// </summary>
	public void Heal( float amount )
	{
		if ( _isDead ) return;
		var prev      = CurrentHealth;
		CurrentHealth = MathX.Clamp( CurrentHealth + amount, 0f, MaxHealth );
		var healed    = CurrentHealth - prev;
		if ( healed > 0f ) OnHealed?.Invoke( healed );
		UpdateHUD();
	}

	/// <summary>
	/// Set the checkpoint position. Called by your checkpoint script.
	/// Optionally heals the player.
	/// </summary>
	public void SetCheckpoint( Vector3 position, float healAmount = 0f )
	{
		_checkpointPosition = position;
		_hasCheckpoint      = true;

		if ( RegenOnCheckpoint && healAmount > 0f )
			Heal( healAmount );
	}

	// ── Internal damage ───────────────────────────────────────────────────────

	private void ApplyDamageInternal( float amount, DamageType type, bool iframes )
	{
		CurrentHealth -= amount;
		CurrentHealth  = CurrentHealth < 0f ? 0f : CurrentHealth;

		OnDamaged?.Invoke( amount );
		SpawnFloatingNumber( amount, type );
		UpdateHUD();

		if ( iframes )
		{
			_isInvincible = true;
			_iframeTimer  = IframeDuration;
			_flashTimer   = 0f;
			_flashState   = true; // start flashing immediately
			if ( FlashOnDamage && SpriteRenderer is not null )
				SpriteRenderer.OverlayColor = FlashColor;
		}

		if ( CurrentHealth <= 0f && !_isDead )
			_ = TriggerDeathSequence();
	}

	private void ApplyKnockback( Vector3? attackerPosition )
	{
		if ( !EnableKnockback || Movement is null ) return;

		float horizontal;

		if ( attackerPosition.HasValue )
		{
			// Top-down: push away from attacker position
			var diff = WorldPosition - attackerPosition.Value;
			horizontal = diff.y > 0f ? 1f : -1f;
		}
		else
		{
			// Platformer: push opposite to facing direction
			horizontal = -Movement.FacingDirection;
		}

		var impulse = Movement.PlatformerMode
			? new Vector2( horizontal * KnockbackForce, KnockbackVerticalForce )
			: new Vector2( horizontal * KnockbackForce, 0f );

		Movement.ApplyImpulse( impulse );
	}

	// ── Death sequence ────────────────────────────────────────────────────────

	private async Task TriggerDeathSequence()
	{
		if ( _isDeathSequence ) return;

		_isDead          = true;
		_isDeathSequence = true;

		StopTickingDamage();
		OnDeath?.Invoke();

		// Clear iframe flash so sprite shows correctly during death
		_isInvincible = false;
		_iframeTimer  = 0f;
		_flashState   = false;
		if ( SpriteRenderer is not null )
			SpriteRenderer.OverlayColor = _originalColor;

		// Block input, keep physics so knockback carries
		if ( Movement is not null )
			Movement.TriggerDeathAir();

		// Small delay so knockback launches the player
		await Task.DelaySeconds( 0.1f );
		if ( !this.IsValid() ) return;

		// Wait for player to land
		if ( Movement is not null )
		{
			while ( !Movement.IsGrounded )
			{
				await Task.Frame();
				if ( !this.IsValid() ) return;
			}

			// Landed — freeze and play ground death animation
			Movement.TriggerDeath();
		}

		// Wait for death animation + any knockback to settle
		await Task.DelaySeconds( DeathAnimHoldDuration );
		if ( !this.IsValid() ) return;

		// Pixelate in
		if ( PixelateComponent is not null )
		{
			PixelateComponent.Scale = 0f;
			while ( PixelateComponent.Scale < 1f )
			{
				PixelateComponent.Scale += PixelateInSpeed * Time.Delta;
				if ( PixelateComponent.Scale > 1f ) PixelateComponent.Scale = 1f;
				await Task.Frame();
				if ( !this.IsValid() ) return;
			}
		}

		// Hold at full pixelation
		await Task.DelaySeconds( BlackoutHoldDuration );
		if ( !this.IsValid() ) return;

		// Respawn
		Respawn();

		// Pixelate out — keep player locked to spawn position during transition
		if ( PixelateComponent is not null )
		{
			var spawnPos = _hasCheckpoint ? _checkpointPosition : WorldPosition;
			while ( PixelateComponent.Scale > 0f )
			{
				WorldPosition = spawnPos;
				if ( Movement is not null ) Movement.ApplyImpulse( Vector2.Zero );
				PixelateComponent.Scale -= PixelateOutSpeed * Time.Delta;
				if ( PixelateComponent.Scale < 0f ) PixelateComponent.Scale = 0f;
				await Task.Frame();
				if ( !this.IsValid() ) return;
			}
		}

		// Re-enable input
		if ( Movement is not null )
			Movement.InputAllowed = true;
	}

	private void Respawn()
	{
		var spawnPos  = _hasCheckpoint ? _checkpointPosition : WorldPosition;
		WorldPosition = spawnPos;

		if ( FullHealOnRespawn )
			CurrentHealth = MaxHealth;

		// Reset all state so player can take damage again
		_isDead          = false;
		_isDeathSequence = false;
		_isInvincible    = false;
		_iframeTimer     = 0f;
		_flashTimer      = 0f;
		_flashState      = false;

		if ( SpriteRenderer is not null )
			SpriteRenderer.OverlayColor = _originalColor;

		if ( Movement is not null )
			Movement.TriggerRespawn();

		UpdateHUD();
		OnRespawned?.Invoke();
	}

	// ── HUD ───────────────────────────────────────────────────────────────────

	private void UpdateHUD()
	{
		// Find the HUD component in the scene and update it
		var hud = Scene.GetAllComponents<HealthHUD2D>().FirstOrDefault();
		hud?.UpdateHealth( CurrentHealth, MaxHealth );
	}

	// ── Helper types ──────────────────────────────────────────────────────────

	private class FloatingNumber
	{
		public Vector3      WorldPos;
		public float        Timer;
		public Color        Color;
		public string       ColorStr;
		public string       Damage;
		public GameObject   GO;
		public TextRenderer            TR;
	}
}