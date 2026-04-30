using Sandbox;
using Sandbox.UI;

/// <summary>
/// Screen-space health HUD root component.
/// Add to a GameObject with a ScreenPanel component alongside HealthBar Razor PanelComponent.
/// Health2D finds this automatically and calls UpdateHealth().
/// </summary>
[Title( "Health HUD 2D" )]
[Category( "2D" )]
[Icon( "favorite_border" )]
public sealed class HealthHUD2D : Component
{
	// ── Display ───────────────────────────────────────────────────────────────

	[Property, Group( "Display" )]
	[Title( "Use Heart Mode" )]
	public bool HeartMode { get; set; } = false;

	// ── Bar ───────────────────────────────────────────────────────────────────

	[Property, Group( "Bar" )]
	[Title( "Bar Background Color" )]
	public Color BarBackgroundColor { get; set; } = new Color( 0f, 0f, 0f, 0.5f );

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
	[Title( "HP Text Color" )]
	public Color HPTextColor { get; set; } = Color.White;

	[Property, Group( "Bar" )]
	[Title( "HP Text Font" )]
	public string HPFont { get; set; } = "";

	[Property, Group( "Bar" )]
	[Range( 8f, 48f ), Step( 1f )]
	[Title( "HP Text Font Size" )]
	public float HPFontSize { get; set; } = 14f;

	[Property, Group( "Bar" )]
	[Range( -20f, 20f ), Step( 1f )]
	[Title( "Text Padding Top (px)" )]
	public float TextPaddingTop { get; set; } = 0f;

	[Property, Group( "Bar" )]
	[Range( -20f, 20f ), Step( 1f )]
	[Title( "Text Padding Left (px)" )]
	public float TextPaddingLeft { get; set; } = 0f;

	[Property, Group( "Bar" )]
	[Range( -20f, 20f ), Step( 1f )]
	[Title( "Text Padding Right (px)" )]
	public float TextPaddingRight { get; set; } = 0f;

	// ── Hearts ────────────────────────────────────────────────────────────────

	[Property, Group( "Hearts" )]
	[ResourceType( "png" )]
	[Title( "Heart Filled Texture" )]
	public string HeartFilledTexture { get; set; } = "";

	[Property, Group( "Hearts" )]
	[ResourceType( "png" )]
	[Title( "Heart Empty Texture" )]
	public string HeartEmptyTexture { get; set; } = "";

	[Property, Group( "Hearts" )]
	[Range( 16f, 64f ), Step( 4f )]
	[Title( "Heart Size (px)" )]
	public float HeartSize { get; set; } = 32f;

	[Property, Group( "Hearts" )]
	[Range( -20f, 40f ), Step( 1f )]
	[Title( "Heart Spacing (px)" )]
	public float HeartSpacing { get; set; } = 4f;

	// ── Layout ────────────────────────────────────────────────────────────────

	[Property, Group( "Layout" )]
	[Title( "Center At Bottom" )]
	public bool CenterAtBottom { get; set; } = true;

	[Property, Group( "Layout" )]
	[Range( 0f, 200f ), Step( 1f )]
	[Title( "Bottom Padding (px)" )]
	public float BottomPadding { get; set; } = 20f;

	[Property, Group( "Layout" )]
	[Title( "Position (px from top-left)" )]
	/// <summary>Only used when Center At Bottom is false.</summary>
	public Vector2 ScreenPosition { get; set; } = new Vector2( 20f, 20f );

	[Property, Group( "Layout" )]
	[Range( 100f, 600f ), Step( 10f )]
	[Title( "Bar Width (px)" )]
	public float BarWidth { get; set; } = 200f;

	[Property, Group( "Layout" )]
	[Range( 10f, 60f ), Step( 2f )]
	[Title( "Bar Height (px)" )]
	public float BarHeight { get; set; } = 20f;

	// ── Internal ──────────────────────────────────────────────────────────────

	private HealthBar _panel;
	private bool      _initialized  = false;
	private int       _initFrames   = 0;

	protected override void OnUpdate()
	{
		if ( _initialized ) return;

		var health = Scene.GetAllComponents<Health2D>().FirstOrDefault();
		if ( health is null ) return;

		_panel = Components.Get<HealthBar>();
		if ( _panel is null ) return;

		_panel.Panel.Style.Position = PositionMode.Absolute;

		if ( CenterAtBottom )
		{
			// Center using left 50% — works at any resolution
			_panel.Panel.Style.Left   = Length.Percent( 50f );
			_panel.Panel.Style.Bottom = Length.Pixels( BottomPadding );
			_panel.Panel.Style.Top    = Length.Auto;
			_panel.Panel.Style.Right  = Length.Auto;
		}
		else
		{
			_panel.Panel.Style.Left   = Length.Pixels( ScreenPosition.x );
			_panel.Panel.Style.Top    = Length.Pixels( ScreenPosition.y );
		}

		UpdateHealth( health.CurrentHealth, health.MaxHealth );

		// Wait two frames before marking initialized so StateHasChanged
		// has time to rebuild the panel tree with correct values
		_initFrames++;
		if ( _initFrames >= 2 )
			_initialized = true;
	}

	/// <summary>Called by Health2D whenever HP changes.</summary>
	public void UpdateHealth( float current, float max )
	{
		if ( _panel is null ) return;

		_panel.CurrentHealth      = current;
		_panel.MaxHealth          = max;
		_panel.HeartMode          = HeartMode;
		_panel.ShowText           = ShowHPText;
		_panel.LowHealthThreshold = LowHealthThreshold;
		_panel.BackgroundColor    = ToCssAlpha( BarBackgroundColor );
		_panel.FullColor          = ToCssAlpha( BarColorFull );
		_panel.LowColor           = ToCssAlpha( BarColorLow );
		_panel.TextColor          = ToCss( HPTextColor );
		_panel.BarWidth           = BarWidth;
		_panel.BarHeight          = BarHeight;
		_panel.HeartSize          = HeartSize;
		_panel.HeartSpacing       = HeartSpacing;
		// Use the texture name directly since ResourcePath may be empty
		_panel.FilledSrc = HeartFilledTexture;
		_panel.EmptySrc  = HeartEmptyTexture;
		_panel.FontFamily         = HPFont;
		_panel.FontSize           = HPFontSize;
		_panel.TextPaddingTop     = TextPaddingTop;
		_panel.TextPaddingLeft    = TextPaddingLeft;
		_panel.TextPaddingRight   = TextPaddingRight;
		_panel.StateHasChanged();
	}

	private static string ToCss( Color c ) =>
		$"rgb({(int)(c.r*255)},{(int)(c.g*255)},{(int)(c.b*255)})";

	private static string ToCssAlpha( Color c ) =>
		$"rgba({(int)(c.r*255)},{(int)(c.g*255)},{(int)(c.b*255)},{c.a:F2})";
}