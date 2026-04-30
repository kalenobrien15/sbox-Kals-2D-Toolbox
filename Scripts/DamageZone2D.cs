using Sandbox;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Damage zone for 2D games.
/// Add a BoxCollider to the same GameObject and set Is Trigger to true.
/// The player needs a BoxCollider (not trigger) tagged "player".
/// Shape and position are controlled entirely by the BoxCollider in the inspector.
/// </summary>
[Title( "Damage Zone 2D" )]
[Category( "2D" )]
[Icon( "dangerous" )]
public sealed class DamageZone2D : Component
{
	[Property, Group( "Damage" )]
	[Range( 0f, 1000f ), Step( 1f )]
	public float Damage { get; set; } = 10f;

	[Property, Group( "Damage" )]
	[Title( "Is Ticking Damage" )]
	public bool IsTicking { get; set; } = false;

	[Property, Group( "Damage" )]
	[Range( 0.1f, 5f ), Step( 0.1f )]
	[Title( "Tick Interval (s)" )]
	public float TickInterval { get; set; } = 0.5f;

	[Property, Group( "Damage" )]
	[Range( 0.1f, 10f ), Step( 0.1f )]
	[Title( "Tick Duration (s)" )]
	public float TickDuration { get; set; } = 3f;

	private List<BoxCollider> _colliders = new();

	protected override void OnStart()
	{
		// Get all BoxColliders on this GameObject and its children
		_colliders = Components.GetAll<BoxCollider>( FindMode.EverythingInSelfAndDescendants ).ToList();

		if ( _colliders.Count == 0 )
		{
			Log.Warning( "DamageZone2D: No BoxColliders found. Add at least one and set Is Trigger to true." );
			return;
		}

		foreach ( var col in _colliders )
			col.OnTriggerEnter += OnTriggerEnter;
	}

	protected override void OnDestroy()
	{
		foreach ( var col in _colliders )
			if ( col is not null )
				col.OnTriggerEnter -= OnTriggerEnter;
	}

	private void OnTriggerEnter( Collider other )
	{
		if ( !other.Tags.Has( "player" ) ) return;

		var health = other.GameObject.GetComponent<Health2D>();
		if ( health is null )
			health = other.GameObject.Components.GetInAncestors<Health2D>();
		if ( health is null ) return;

		if ( IsTicking )
			health.ApplyTickingDamage( Damage, TickInterval, TickDuration );
		else
			health.TakeDamage( Damage, WorldPosition );
	}
}