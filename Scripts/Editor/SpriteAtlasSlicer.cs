using Editor;
using Sandbox;
using System;
using System.IO;

/// <summary>
/// Sprite Atlas Slicer — appears in the Editor Apps sidebar and Apps menu.
/// Load a spritesheet, configure rows/columns or fixed frame size, see a live
/// grid preview, then export each frame as an individual PNG.
///
/// Place this file inside an /Editor/ folder in your project.
/// </summary>
[EditorApp( "Sprite Atlas Slicer", "grid_view", "Slice spritesheets into individual frames" )]
public class SpriteAtlasSlicer : Window
{
	// ── State ─────────────────────────────────────────────────────────────────

	private string _sourcePath   = "";
	private string _outputDir    = "";
	private string _outputName   = "sprite";
	private Bitmap _sourceBitmap = null;

	private int  _columns   = 4;
	private int  _rows      = 4;
	private int  _frameW    = 32;
	private int  _frameH    = 32;
	private int   _padding    = 0;
	private float _frameRate  = 10f;
	// UI refs
	private Label       _infoLabel;
	private Label       _frameInfoLabel;
	private LineEdit    _colsEdit;
	private LineEdit    _rowsEdit;
	private LineEdit    _fwEdit;
	private LineEdit    _fhEdit;
	private LineEdit    _padEdit;
	private LineEdit    _fpsEdit;
	private LineEdit    _nameEdit;
	private LineEdit    _dirEdit;
	private Button      _exportBtn;
	private PreviewPane _preview;

	// ─────────────────────────────────────────────────────────────────────────

	public SpriteAtlasSlicer()
	{
		WindowTitle = "Sprite Atlas Slicer";
		MinimumSize = new Vector2( 800, 500 );

		Canvas        = new Widget( null );
		Canvas.Layout = Layout.Row();
		Canvas.Layout.Margin  = 0;
		Canvas.Layout.Spacing = 0;

		BuildUI();
	}

	// ── UI 

	private void BuildUI()
	{
		// ── Left controls panel
		var left = new Widget( Canvas );
		left.Layout = Layout.Column();
		left.Layout.Margin  = 8;
		left.Layout.Spacing = 6;
		left.MinimumWidth   = 300;
		left.MaximumWidth   = 320;
		Canvas.Layout.Add( left );

		// Load
		var loadBtn = new Button( "Load Spritesheet...", left );
		loadBtn.Clicked += LoadSpritesheet;
		left.Layout.Add( loadBtn );

		_infoLabel = new Label( "No file loaded.", left );
		_infoLabel.SetStyles( "color: #aaaaaa; font-size: 11px;" );
		left.Layout.Add( _infoLabel );

		left.Layout.AddSeparator();

		// Slice settings — all always visible
		var sliceHeader = new Label( "Slice Settings", left );
		sliceHeader.SetStyles( "font-weight: bold;" );
		left.Layout.Add( sliceHeader );

		AddEditRow( left, "Columns:",         "4",  out _colsEdit );
		AddEditRow( left, "Rows:",            "4",  out _rowsEdit );
		AddEditRow( left, "Frame Width (px):", "0", out _fwEdit );
		AddEditRow( left, "Frame Height (px):","0", out _fhEdit );
		AddEditRow( left, "Padding (px):",    "0",  out _padEdit );

		AddEditRow( left, "Frame Rate (fps):", "10", out _fpsEdit );
		_fpsEdit.TextEdited += v => { if ( float.TryParse( v, out var n ) ) _frameRate = n; };

		var hint = new Label( "Set frame size to 0 to calculate from columns/rows.", left );
		hint.SetStyles( "color: #888888; font-size: 10px;" );
		left.Layout.Add( hint );

		_colsEdit.TextEdited += v => { if ( int.TryParse( v, out var n ) ) { _columns = n; Refresh(); } };
		_rowsEdit.TextEdited += v => { if ( int.TryParse( v, out var n ) ) { _rows    = n; Refresh(); } };
		_fwEdit.TextEdited   += v => { if ( int.TryParse( v, out var n ) ) { _frameW  = n; Refresh(); } };
		_fhEdit.TextEdited   += v => { if ( int.TryParse( v, out var n ) ) { _frameH  = n; Refresh(); } };
		_padEdit.TextEdited  += v => { if ( int.TryParse( v, out var n ) ) { _padding = n; Refresh(); } };

		left.Layout.AddSeparator();

		// Frame info
		_frameInfoLabel = new Label( "Load a spritesheet to begin.", left );
		_frameInfoLabel.SetStyles( "color: #aaaaaa; font-size: 11px;" );
		left.Layout.Add( _frameInfoLabel );

		left.Layout.AddSeparator();

		// Output
		AddEditRow( left, "Output name:",   "sprite", out _nameEdit );
		AddEditRow( left, "Output folder:", "",       out _dirEdit );
		_nameEdit.TextEdited += v => _outputName = v;
		_dirEdit.TextEdited  += v => _outputDir  = v;

		var browseBtn = new Button( "Browse Output Folder...", left );
		browseBtn.Clicked += BrowseDir;
		left.Layout.Add( browseBtn );

		left.Layout.AddStretchCell();

		_exportBtn = new Button( "Export All Frames", left );
		_exportBtn.Enabled = false;
		_exportBtn.Clicked += ExportFrames;
		_exportBtn.SetStyles( "font-weight: bold;" );
		left.Layout.Add( _exportBtn );

		// ── Right preview panel 
		_preview = new PreviewPane( Canvas );
		Canvas.Layout.Add( _preview, 1 );

		// Set default output dir to project root
		SetDefaultOutputDir();
	}

	// ─────────────────────────────────────────────────────────────────────────

	private void SetDefaultOutputDir()
	{
		try
		{
			var assetsPath = Project.Current?.GetAssetsPath();
			if ( !string.IsNullOrEmpty( assetsPath ) && Directory.Exists( assetsPath ) )
			{
				_outputDir    = assetsPath;
				_dirEdit.Text = assetsPath;
				return;
			}
		}
		catch { }
	}

	private void AddEditRow( Widget parent, string labelText, string defaultValue, out LineEdit edit )
	{
		var row = new Widget( parent );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 6;
		var lbl = new Label( labelText, row );
		lbl.MinimumWidth = 120;
		row.Layout.Add( lbl );
		edit = new LineEdit( row );
		edit.Text = defaultValue;
		row.Layout.Add( edit, 1 );
		parent.Layout.Add( row );
	}

	private (int cols, int rows, int fw, int fh) GetParams( int imgW, int imgH )
	{
		// If frame size is explicitly set, use it to derive cols/rows.
		// Otherwise calculate frame size from cols/rows.
		int cols, rows, fw, fh;

		if ( _frameW > 0 && _frameH > 0 )
		{
			fw   = _frameW;
			fh   = _frameH;
			cols = Math.Max( 1, ( imgW + _padding ) / Math.Max( 1, fw + _padding ) );
			rows = Math.Max( 1, ( imgH + _padding ) / Math.Max( 1, fh + _padding ) );
		}
		else
		{
			cols = Math.Max( 1, _columns );
			rows = Math.Max( 1, _rows );
			fw   = Math.Max( 1, ( imgW - _padding * ( cols + 1 ) ) / cols );
			fh   = Math.Max( 1, ( imgH - _padding * ( rows + 1 ) ) / rows );
		}

		return ( cols, rows, fw, fh );
	}

	private void Refresh()
	{
		if ( _sourceBitmap is null ) return;

		var ( cols, rows, fw, fh ) = GetParams( _sourceBitmap.Width, _sourceBitmap.Height );
		_frameInfoLabel.Text = $"Frame: {fw}×{fh} px   |   Frames: {cols * rows}   |   Sheet: {_sourceBitmap.Width}×{_sourceBitmap.Height} px";

		// Clone source, draw green grid lines on top, send to preview
		var preview = _sourceBitmap.Clone();
		preview.SetPen( Color.Green, 1f );

		for ( int r = 0; r < rows; r++ )
		for ( int c = 0; c < cols; c++ )
		{
			int x = _padding + c * ( fw + _padding );
			int y = _padding + r * ( fh + _padding );
			preview.DrawRect( x, y, fw, fh );
		}

		_preview.SetBitmap( preview );
	}

	// ── Load 

	private void LoadSpritesheet()
	{
		// Use the confirmed EditorUtility.OpenFileDialog
		var path = EditorUtility.OpenFileDialog( "Load Spritesheet", "Images (*.png *.jpg *.tga *.bmp)", "" );
		if ( string.IsNullOrEmpty( path ) ) return;

		try
		{
			_sourcePath   = path;
			var bytes     = File.ReadAllBytes( path );
			_sourceBitmap = Bitmap.CreateFromBytes( bytes );

			if ( _sourceBitmap is null || !_sourceBitmap.IsValid )
			{
				_infoLabel.Text = "Failed to load image — unsupported format?";
				return;
			}

			_outputName    = Path.GetFileNameWithoutExtension( path );
			_nameEdit.Text = _outputName;
			_infoLabel.Text = $"{Path.GetFileName( path )}  ({_sourceBitmap.Width} × {_sourceBitmap.Height} px)";
			_exportBtn.Enabled = true;

			// Default output to source folder if not already set
			if ( string.IsNullOrEmpty( _outputDir ) )
			{
				_outputDir    = Path.GetDirectoryName( path );
				_dirEdit.Text = _outputDir;
			}

			Refresh();
		}
		catch ( Exception ex )
		{
			_infoLabel.Text = $"Error loading: {ex.Message}";
			Log.Error( $"SpriteAtlasSlicer: Failed to load {path} — {ex.Message}" );
		}
	}

	// ── Browse 

	private void BrowseDir()
	{
		var fd = new FileDialog( null );
		fd.Title = "Select Output Folder";
		fd.SetFindDirectory();

		// Start in the project assets folder
		var assetsPath = Project.Current?.GetAssetsPath();
		if ( !string.IsNullOrEmpty( assetsPath ) )
			fd.Directory = assetsPath;

		if ( !fd.Execute() ) return;
		_outputDir    = fd.SelectedFile;
		_dirEdit.Text = _outputDir;
	}

	// ── Export 

	private void ExportFrames()
	{
		if ( _sourceBitmap is null ) return;

		try
		{
			var ( cols, rows, fw, fh ) = GetParams( _sourceBitmap.Width, _sourceBitmap.Height );

			if ( string.IsNullOrEmpty( _outputDir ) )
				_outputDir = Path.GetDirectoryName( _sourcePath );

			Directory.CreateDirectory( _outputDir );

			// ── Step 1: Export each frame as a PNG 
			var framePaths = new System.Collections.Generic.List<string>();

			int index = 0;
			for ( int r = 0; r < rows; r++ )
			for ( int c = 0; c < cols; c++ )
			{
				int sx = _padding + c * ( fw + _padding );
				int sy = _padding + r * ( fh + _padding );

				var frame  = _sourceBitmap.Crop( new Rect( sx, sy, fw, fh ) );
				var pixmap = Pixmap.FromBitmap( frame );
				var path   = Path.Combine( _outputDir, $"{_outputName}_{index:000}.png" );
				pixmap.SavePng( path );
				framePaths.Add( path );
				index++;
			}

			// ── Step 2: Build textures from saved PNGs 
			var textures = new System.Collections.Generic.List<Texture>();
			foreach ( var path in framePaths )
			{
				// Get raw RGBA bytes from the bitmap pixels
				var bmp    = Bitmap.CreateFromBytes( File.ReadAllBytes( path ) );
				var pixels = bmp.GetPixels32();
				var raw    = new byte[pixels.Length * 4];
				for ( int i = 0; i < pixels.Length; i++ )
				{
					raw[i * 4 + 0] = (byte)(pixels[i].r * 255);
					raw[i * 4 + 1] = (byte)(pixels[i].g * 255);
					raw[i * 4 + 2] = (byte)(pixels[i].b * 255);
					raw[i * 4 + 3] = (byte)(pixels[i].a * 255);
				}
				var tex = Texture.Create( bmp.Width, bmp.Height )
					.WithData( raw )
					.Finish();
				textures.Add( tex );
			}

			// ── Step 3: Create the Sprite with confirmed frameRate parameter 
			var sprite     = Sprite.FromTextures( textures, _frameRate );
			var spritePath = Path.Combine( _outputDir, $"{_outputName}.sprite" );

			// CreateResource makes an empty asset file, SaveToDisk writes the data
			var asset = AssetSystem.CreateResource( "sprite", spritePath );
			asset?.SaveToDisk( sprite );

			// Note: asset browser will refresh automatically on next focus

			ShowMessage( $"Exported {index} frames and created:\n{_outputName}.sprite" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"SpriteAtlasSlicer export failed: {ex.Message}" );
			ShowMessage( $"Export failed:\n{ex.Message}" );
		}
	}

	private void ShowMessage( string text )
	{
		var msg = new Widget( null );
		msg.WindowTitle = "Sprite Atlas Slicer";
		msg.Layout = Layout.Column();
		msg.Layout.Margin  = 16;
		msg.Layout.Spacing = 8;
		msg.Layout.Add( new Label( text, msg ) );
		var ok = new Button( "OK", msg );
		ok.Clicked += () => msg.Close();
		msg.Layout.Add( ok );
		msg.Show();
	}

	// ── Preview Widget 

	/// <summary>
	/// Draws the preview bitmap scaled to fit, with a checkerboard background.
	/// </summary>
	private class PreviewPane : Widget
	{
		private Pixmap _pixmap;
		private int    _bmpW;
		private int    _bmpH;

		public PreviewPane( Widget parent ) : base( parent, false )
		{
			MinimumSize = new Vector2( 200, 200 );
			SetStyles( "background-color: #1a1a1a;" );
		}

		public void SetBitmap( Bitmap bmp )
		{
			// Convert Bitmap -> Pixmap once, cache it
			_pixmap = Pixmap.FromBitmap( bmp );
			_bmpW   = bmp.Width;
			_bmpH   = bmp.Height;
			Update();
		}

		protected override void OnPaint()
		{
			// Flat dark grey background
			Paint.SetBrush( new Color( 0.15f, 0.15f, 0.15f, 1f ) );
			Paint.ClearPen();
			Paint.DrawRect( LocalRect );

			if ( _pixmap is null ) return;

			// Scale to fit preserving aspect ratio
			float scale = Math.Min( Width / (float)_bmpW, Height / (float)_bmpH );
			float drawW = _bmpW * scale;
			float drawH = _bmpH * scale;
			float drawX = ( Width  - drawW ) * 0.5f;
			float drawY = ( Height - drawH ) * 0.5f;

			Paint.Draw( new Rect( drawX, drawY, drawW, drawH ), _pixmap );
		}
	}
}
