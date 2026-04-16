using Sandbox;

/// <summary>
/// A 2D collision box for use with Movement2D.
/// Handles box sweep traces and gizmo visualization independently
/// of any movement logic.

[Title( "2D Collider" )]
[Category( "2D" )]
[Icon( "crop_square" )]
public sealed class Collider2D : Component
{
	// ── Shape 

	[Property, Group( "Shape" )]
	[Range( 1f, 500f, 1f )]
	[Title( "Width" )]
	public float Width { get; set; } = 32f;

	[Property, Group( "Shape" )]
	[Range( 1f, 500f, 1f )]
	[Title( "Height" )]
	public float Height { get; set; } = 32f;

	[Property, Group( "Shape" )]
	[Title( "Offset" )]
	public Vector3 Offset { get; set; } = Vector3.Zero;

	// ── Gizmo 

	[Property, Group( "Gizmo" )]
	[Title( "Gizmo Color" )]
	public Color GizmoColor { get; set; } = Color.Green;

	[Property, Group( "Gizmo" )]
	[Range( 0f, 1f, 0.05f )]
	[Title( "Gizmo Fill Opacity" )]
	public float GizmoOpacity { get; set; } = 0.15f;

	// ── Public API 

	/// <summary>
	/// Returns the BBox for this collider in local space.

	public BBox Bounds => new BBox(
		Offset + new Vector3( -Width / 2f, -Width / 2f, -Height / 2f ),
		Offset + new Vector3(  Width / 2f,  Width / 2f,  Height / 2f )
	);

	/// <summary>
	/// Sweeps the collider from <paramref name="from"/> toward
	/// <paramref name="from"/> + <paramref name="delta"/>, stopping at any hit.
	/// Returns true if the move was unobstructed.

	public bool SweepMove( Vector3 delta )
	{
		var from = GameObject.WorldPosition;
		var to   = from + delta;

		var tr = Scene.Trace
			.Box( Bounds, from, to )
			.WithoutTags( "player", "trigger" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( tr.Hit )
		{
			GameObject.WorldPosition = tr.EndPosition;
			return false;
		}

		GameObject.WorldPosition = to;
		return true;
	}

	// ── Gizmo ─────────────────────────────────────────────────────────────────

	protected override void DrawGizmos()
	{
		var bounds = Bounds;

		Gizmo.Hitbox.BBox( bounds );

		using ( Gizmo.Scope( "collider2d" ) )
		{
			Gizmo.Draw.Color = GizmoColor.WithAlpha( GizmoOpacity );
			Gizmo.Draw.SolidBox( bounds );

			Gizmo.Draw.Color = GizmoColor;
			Gizmo.Draw.LineBBox( bounds );
		}
	}
}
