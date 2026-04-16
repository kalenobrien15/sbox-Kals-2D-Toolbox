using Sandbox;

/// <summary>
/// Smoothly follows a target GameObject with configurable lag.
/// Attach to your Camera GameObject.

[Title( "2D Camera Follow" )]
[Category( "2D" )]
[Icon( "videocam" )]
public sealed class CameraFollow2D : Component
{
	// ── Target 

	[Property, Group( "Target" )]
	[Title( "Follow Target" )]
	public GameObject Target { get; set; }

	/// <summary>
	/// Offset from the target's position. Useful for looking ahead
	/// or adjusting the camera anchor point.

	[Property, Group( "Target" )]
	[Title( "Offset" )]
	public Vector3 Offset { get; set; } = Vector3.Zero;

	// ── Smoothing

	/// <summary>
	/// How quickly the camera catches up to the target.
	/// Higher = snappier, lower = more lag. 0 = no movement.

	[Property, Group( "Smoothing" )]
	[Range( 0f, 30f, 0.5f )]
	[Title( "Follow Speed" )]
	public float FollowSpeed { get; set; } = 5f;

	/// <summary>
	/// When enabled the camera will snap instantly to the target
	/// on the first frame instead of sliding in from (0,0,0).

	[Property, Group( "Smoothing" )]
	[Title( "Snap On Start" )]
	public bool SnapOnStart { get; set; } = true;

	// ── Deadzone 

	/// <summary>
	/// The camera won't move until the target is further than this
	/// distance away. Gives the player room to move without the
	/// camera constantly shifting. Set to 0 to disable.

	[Property, Group( "Deadzone" )]
	[Range( 0f, 500f, 5f )]
	[Title( "Deadzone Radius" )]
	public float DeadzoneRadius { get; set; } = 0f;

	// ── State 

	[Property, Group( "State" ), ReadOnly]
	public float DistanceToTarget { get; private set; }

	// ─────────────────────────────────────────────────────────────────────────

	protected override void OnStart()
	{
		if ( SnapOnStart && Target is not null )
			WorldPosition = Target.WorldPosition + Offset;
	}

	protected override void OnUpdate()
	{
		if ( Target is null ) return;

		var targetPos = Target.WorldPosition + Offset;

		DistanceToTarget = Vector3.DistanceBetween( WorldPosition, targetPos );

		// Don't move if within the deadzone
		if ( DistanceToTarget <= DeadzoneRadius ) return;

		WorldPosition = Vector3.Lerp( WorldPosition, targetPos, Time.Delta * FollowSpeed );
	}

	// ────────────────────────────────────────────────────────────────────────
	// Draw the deadzone and target line in the editor.
	// ────────────────────────────────────────────────────────────────────────

	protected override void DrawGizmos()
	{
		// Deadzone circle
		if ( DeadzoneRadius > 0f )
		{
			using ( Gizmo.Scope( "deadzone" ) )
			{
				Gizmo.Draw.Color = Color.Yellow.WithAlpha( 0.3f );
				Gizmo.Draw.LineSphere( Vector3.Zero, DeadzoneRadius );
			}
		}

		// Line to target
		if ( Target is not null )
		{
			using ( Gizmo.Scope( "target_line" ) )
			{
				Gizmo.Draw.Color = Color.Cyan.WithAlpha( 0.5f );
				Gizmo.Draw.Line( WorldPosition, Target.WorldPosition );
			}
		}
	}
}
