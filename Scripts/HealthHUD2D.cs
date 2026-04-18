using Sandbox;
using Sandbox.UI;

/// <summary>
/// Health HUD root panel component.
/// Add to a GameObject that also has a ScreenPanel component.
/// Health2D finds this in the scene and calls UpdateHealth().
/// </summary>
[Title( "Health HUD 2D" )]
[Category( "2D" )]
[Icon( "favorite_border" )]
public sealed class HealthHUD2D : PanelComponent
{
	[Property, Group( "Display" )]
	[Title( "Use Heart Mode" )]
	public bool HeartMode { get; set; } = false;

	[Property, Group( "Bar" )]
	[Title( "Bar Color (full)" )]
	public Color BarColorFull { get; set; } = Color.Green;

	[Property, Group( "Bar" )]
	[Title( "Bar Color (low)" )]
	public Color BarColorLow { get; set; } = Color.Red;

	[Property, Group( "Bar" )]
	[Range( 0f, 1f ), Step( 0.05f )]
	[Title( "Low Health Threshold" )]
	public float LowHealthThreshold { get; set; } = 0.25f;

	[Property, Group( "Bar" )]
	[Title( "Show HP Text" )]
	public bool ShowHPText { get; set; } = true;

	[Property, Group( "Bar" )]
	[Title( "HP Text Font" )]
	public string HPFont { get; set; } = "Roboto";

	[Property, Group( "Bar" )]
	[Title( "HP Text Color" )]
	public Color HPTextColor { get; set; } = Color.White;

	[Property, Group( "Hearts" )]
	[Title( "Heart Filled Texture" )]
	public Texture HeartFilledTexture { get; set; }

	[Property, Group( "Hearts" )]
	[Title( "Heart Empty Texture" )]
	public Texture HeartEmptyTexture { get; set; }

	[Property, Group( "Hearts" )]
	[Range( 16f, 64f ), Step( 4f )]
	[Title( "Heart Size (px)" )]
	public float HeartSize { get; set; } = 32f;

	[Property, Group( "Layout" )]
	[Title( "Position (px from top-left)" )]
	public Vector2 ScreenPosition { get; set; } = new Vector2( 20f, 20f );

	[Property, Group( "Layout" )]
	[Range( 100f, 600f ), Step( 10f )]
	[Title( "Bar Width (px)" )]
	public float BarWidth { get; set; } = 200f;

	[Property, Group( "Layout" )]
	[Range( 10f, 40f ), Step( 2f )]
	[Title( "Bar Height (px)" )]
	public float BarHeight { get; set; } = 20f;

	// ── Internal ──────────────────────────────────────────────────────────────


	private Panel _barFill;
	private Label _hpLabel;
	private Panel _heartContainer;

	protected override void OnTreeFirstBuilt()
	{
		base.OnTreeFirstBuilt();

		Panel.Style.Position = PositionMode.Absolute;
		Panel.Style.Left     = ScreenPosition.x;
		Panel.Style.Top      = ScreenPosition.y;
	}

	private void BuildPanels()
	{
		// Clear anything previously built
		Panel.DeleteChildren( true );
		_barFill        = null;
		_hpLabel        = null;
		_heartContainer = null;

		if ( HeartMode )
		{
			_heartContainer                     = new Panel();
			_heartContainer.Style.Display       = DisplayMode.Flex;
			_heartContainer.Style.FlexDirection = FlexDirection.Row;
			_heartContainer.Style.FlexWrap      = Wrap.Wrap;
			_heartContainer.Parent              = Panel;
		}
		else
		{
			var bg                           = new Panel();
			bg.Style.Width                   = Length.Pixels( BarWidth );
			bg.Style.Height                  = Length.Pixels( BarHeight );
			bg.Style.BackgroundColor         = Color.Black.WithAlpha( 0.5f );
			bg.Style.BorderTopLeftRadius     = Length.Pixels( 4f );
			bg.Style.BorderTopRightRadius    = Length.Pixels( 4f );
			bg.Style.BorderBottomLeftRadius  = Length.Pixels( 4f );
			bg.Style.BorderBottomRightRadius = Length.Pixels( 4f );
			bg.Style.Overflow                = OverflowMode.Hidden;
			bg.Parent                        = Panel;

			_barFill                       = new Panel();
			_barFill.Style.Height          = Length.Fraction( 1f );
			_barFill.Style.Width           = Length.Fraction( 1f );
			_barFill.Style.BackgroundColor = BarColorFull;
			_barFill.Parent                = bg;

			if ( ShowHPText )
			{
				// Label sits inside the bar background so it centers over the fill
				_hpLabel                       = new Label();
				_hpLabel.Style.Position        = PositionMode.Absolute;
				_hpLabel.Style.Left            = Length.Pixels( 0f );
				_hpLabel.Style.Top             = Length.Pixels( 5f );
				_hpLabel.Style.Width           = Length.Fraction( 1f );
				_hpLabel.Style.Height          = Length.Fraction( 1f );
				_hpLabel.Style.FontSize        = 18f;
				_hpLabel.Style.FontWeight      = 600;
				_hpLabel.Style.FontColor       = HPTextColor;
				_hpLabel.Style.TextAlign       = TextAlign.Center;
				_hpLabel.Style.FontFamily      = HPFont;
				_hpLabel.Parent                = bg;
			}
		}
	}

	private bool _initialized = false;

	protected override void OnUpdate()
	{
		if ( _initialized ) return;

		var health = Scene.GetAllComponents<Health2D>().FirstOrDefault();
		if ( health is null ) return;

		_initialized = true;
		BuildPanels();
		UpdateHealth( health.CurrentHealth, health.MaxHealth );
	}

	/// <summary>Called by Health2D whenever HP changes.</summary>
	public void UpdateHealth( float current, float max )
	{
		if ( HeartMode )
		{
			RebuildHearts( current, max );
		}
		else
		{
			if ( _barFill is null || max <= 0f ) return;

			float f    = current / max;
			f          = f < 0f ? 0f : f > 1f ? 1f : f;
			var color  = f <= LowHealthThreshold ? BarColorLow : BarColorFull;

			_barFill.Style.Width           = Length.Percent( f * 100f );
			_barFill.Style.BackgroundColor = color;

			if ( _hpLabel is not null )
				_hpLabel.Text = $"{(int)current} / {(int)max}";
		}
	}

	private void RebuildHearts( float current, float max )
	{
		if ( _heartContainer is null ) return;
		_heartContainer.DeleteChildren( true );

		int total  = (int)max;
		int filled = (int)current;

		for ( int i = 0; i < total; i++ )
		{
			var heart                    = new Panel();
			heart.Style.Width        = Length.Pixels( HeartSize );
			heart.Style.Height       = Length.Pixels( HeartSize );
			heart.Style.MarginRight  = Length.Pixels( 4f );
			heart.Style.BackgroundSizeX   = Length.Percent( 100f );
			heart.Style.BackgroundSizeY   = Length.Percent( 100f );
			heart.Style.ImageRendering    = ImageRendering.Point;

			var tex = i < filled ? HeartFilledTexture : HeartEmptyTexture;
			if ( tex is not null )
				heart.Style.BackgroundImage = tex;

			heart.Parent = _heartContainer;
		}
	}
}