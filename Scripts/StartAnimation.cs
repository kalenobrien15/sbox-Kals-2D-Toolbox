using Sandbox;

public sealed class StartAnimation : Component
{
	[Property, Group( "Pixelation Component" )]
	[Title( "Component for camera pixelation at Start" )]
	public Pixelate PixelCameraIntro;

	[Property, Group( "Player Input" )]
	[Title( "Component for players movement" )]

	public GameObject Player;
	protected override void OnStart()
	{
		PixelCameraIntro.Scale = 1;
	
	}	

	protected override void OnUpdate()
	{
		PixelCameraIntro.Scale -= 1 * Time.Delta;
		if ( PixelCameraIntro.Scale <= 0)
		{
				Player.GetComponent<Movement2D>().InputAllowed=true;
		}
	}
}
