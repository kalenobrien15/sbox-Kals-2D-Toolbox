using Sandbox;

/// <summary>
/// Manages the 2D camera intro and death/respawn pixelation sequences.
/// Replaces StartAnimation.cs — attach to the same GameObject as your camera.
/// </summary>
[Title( "Camera Controller 2D" )]
[Category( "2D" )]
[Icon( "videocam" )]
public sealed class CameraController2D : Component
{
	[Property, Group( "References" )]
	[Title( "Pixelate Component" )]
	public Pixelate PixelCameraIntro { get; set; }

	[Property, Group( "References" )]
	[Title( "Player" )]
	public GameObject Player { get; set; }

	[Property, Group( "Intro" )]
	[Range( 0.1f, 5f, 0.1f )]
	[Title( "Intro Unpixelate Speed" )]
	public float IntroSpeed { get; set; } = 1f;

	private Movement2D _movement;
	private bool       _introComplete = false;

	protected override void OnStart()
	{
		if ( PixelCameraIntro is not null )
			PixelCameraIntro.Scale = 1f;

		if ( Player is not null )
			_movement = Player.GetComponent<Movement2D>();

		if ( _movement is not null )
			_movement.InputAllowed = false;
	}

	protected override void OnUpdate()
	{
		if ( _introComplete ) return;
		if ( PixelCameraIntro is null ) return;

		PixelCameraIntro.Scale -= IntroSpeed * Time.Delta;

		if ( PixelCameraIntro.Scale <= 0f )
		{
			PixelCameraIntro.Scale = 0f;
			_introComplete         = true;

			if ( _movement is not null )
				_movement.InputAllowed = true;
		}
	}
}
