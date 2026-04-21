using Sandbox;

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

	[Property, Group( "Shape" )]
	[Range( 1f, 500f ), Step( 1f )]
	public float Width { get; set; } = 32f;

	[Property, Group( "Shape" )]
	[Range( 1f, 500f ), Step( 1f )]
	public float Height { get; set; } = 32f;

	[Property, Group( "Shape" )]
	public Vector3 Offset { get; set; } = Vector3.Zero;

	private BBox Bounds => new BBox(
		Offset + new Vector3( -1f, -Width / 2f, -Height / 2f ),
		Offset + new Vector3(  1f,  Width / 2f,  Height / 2f )
	);

	private Health2D _health;

	protected override void OnStart()
	{
		_health = Scene.GetAllComponents<Health2D>().FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		if ( _health is null )
			_health = Scene.GetAllComponents<Health2D>().FirstOrDefault();
		if ( _health is null ) return;

		var localPos = WorldTransform.PointToLocal( _health.WorldPosition );
		var b        = Bounds;
		var inside   = localPos.x >= b.Mins.x && localPos.x <= b.Maxs.x
		            && localPos.y >= b.Mins.y && localPos.y <= b.Maxs.y
		            && localPos.z >= b.Mins.z && localPos.z <= b.Maxs.z;

		if ( !inside ) return;

		if ( IsTicking )
			_health.ApplyTickingDamage( Damage, TickInterval, TickDuration );
		else
			_health.TakeDamage( Damage, WorldPosition );
	}


}