using Sandbox;

/// <summary>
/// Debug component for testing Health2D.
/// Attach to the same GameObject as Health2D.
/// - Minus key: take 1 flat damage
/// - Plus key:  apply poison ticking damage
/// - Equals key: heal 1 hp
/// Remove this component before shipping!
/// </summary>
[Title( "Health Debug" )]
[Category( "2D" )]
[Icon( "bug_report" )]
public sealed class HealthDebug : Component
{
	private Health2D _health;

	protected override void OnStart()
	{
		_health = Components.Get<Health2D>();
	}

	protected override void OnUpdate()
	{
		if ( _health is null ) return;

		if ( Input.Pressed( "damage" ) )
			_health.TakeDamage( 1f );

		if ( Input.Pressed( "poison" ) )
			_health.ApplyTickingDamage( damagePerTick: 1f, tickInterval: 1f, totalDuration: 4f);

		if ( Input.Pressed( "heal" ) )
			_health.Heal( 1f );
	}
}
