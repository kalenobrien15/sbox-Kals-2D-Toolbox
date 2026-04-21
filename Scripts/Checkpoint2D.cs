using Sandbox;

/// <summary>
/// 2D checkpoint. Place on a GameObject and size the zone via Width and Height.
/// Uses the same trace system as Collider2D — no physics collider needed.
/// </summary>
[Title( "Checkpoint 2D" )]
[Category( "2D" )]
[Icon( "flag" )]
public sealed class Checkpoint2D : Component
{
	[Property, Group( "Checkpoint" )]
	[Title( "Heal Amount On Touch" )]
	public float HealAmount { get; set; } = 0f;

	[Property, Group( "Shape" )]
	[Range( 1f, 500f ), Step( 1f )]
	public float Width { get; set; } = 32f;

	[Property, Group( "Shape" )]
	[Range( 1f, 500f ), Step( 1f )]
	public float Height { get; set; } = 32f;

	[Property, Group( "Shape" )]
	public Vector3 Offset { get; set; } = Vector3.Zero;

	private bool _triggered = false;

	private BBox Bounds => new BBox(
		Offset + new Vector3( -Width / 2f, -Width / 2f, -Height / 2f ),
		Offset + new Vector3(  Width / 2f,  Width / 2f,  Height / 2f )
	);

	protected override void OnUpdate()
	{
		// Instead of a trace, find Health2D in the scene and check if the
		// player position is inside our bounds each frame
		var health = Scene.GetAllComponents<Health2D>().FirstOrDefault();
		if ( health is null ) return;

		var localPlayerPos = WorldTransform.PointToLocal( health.WorldPosition );
		var bounds         = Bounds;
		var inside         = localPlayerPos.x >= bounds.Mins.x && localPlayerPos.x <= bounds.Maxs.x
		                  && localPlayerPos.y >= bounds.Mins.y && localPlayerPos.y <= bounds.Maxs.y
		                  && localPlayerPos.z >= bounds.Mins.z && localPlayerPos.z <= bounds.Maxs.z;

		if ( inside )
		{
			if ( _triggered ) return;
			_triggered = true;

			health.SetCheckpoint( WorldPosition, HealAmount );
		}
		else
		{
			_triggered = false;
		}
	}

	protected override void DrawGizmos()
	{
		using ( Gizmo.Scope( "checkpoint2d" ) )
		{
			Gizmo.Draw.Color = Color.Yellow.WithAlpha( 0.15f );
			Gizmo.Draw.SolidBox( Bounds );

			Gizmo.Draw.Color = Color.Yellow;
			Gizmo.Draw.LineBBox( Bounds );
		}
	}
}