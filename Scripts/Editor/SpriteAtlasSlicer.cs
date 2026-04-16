using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Sprite Atlas Slicer — appears in the Editor Apps sidebar and Apps menu.
/// Load a spritesheet, configure slice dimensions, define named animations
/// per row, then export frames and a ready-to-use .sprite asset.
///
/// Place this file inside an /Editor/ folder in your project.
/// </summary>
[EditorApp( "Sprite Atlas Slicer", "grid_view", "Slice spritesheets into individual frames" )]
public class SpriteAtlasSlicer : Window
{
	// ── Animation definition ──────────────────────────────────────────────────

	private class AnimationDef
	{
		public string Name      = "Default";
		public int    StartRow  = 0;
		public int    RowCount  = 1;
		public float  FrameRate = 10f;
		public string LoopMode  = "Loop";   // Loop, Once, PingPong
		public string Origin    = "0.5,0.5";
	}

	// ── State ─────────────────────────────────────────────────────────────────

	private string _sourcePath   = "";
	private string _outputDir    = "";
	private string _outputName   = "sprite";
	private Bitmap _sourceBitmap = null;

	private int _columns = 4;
	private int _rows    = 4;
	private int _frameW  = 0;
	private int _frameH  = 0;
	private int _padding = 0;

	private List<AnimationDef> _animations = new() { new AnimationDef() };

	// UI refs
	private Label       _infoLabel;
	private Label       _frameInfoLabel;
	private LineEdit    _colsEdit;
	private LineEdit    _rowsEdit;
	private LineEdit    _fwEdit;
	private LineEdit    _fhEdit;
	private LineEdit    _padEdit;
	private LineEdit    _nameEdit;
	private LineEdit    _dirEdit;
	private Button      _exportBtn;
	private Widget      _animList;
	private PreviewPane _preview;

	// ─────────────────────────────────────────────────────────────────────────

	public SpriteAtlasSlicer()
	{
		WindowTitle = "Sprite Atlas Slicer";
		MinimumSize = new Vector2( 900, 560 );

		Canvas        = new Widget( null );
		Canvas.Layout = Layout.Row();
		Canvas.Layout.Margin  = 0;
		Canvas.Layout.Spacing = 0;

		BuildUI();
	}

	// ── UI ────────────────────────────────────────────────────────────────────

	private void BuildUI()
	{
		// ── Left controls ─────────────────────────────────────────────────────
		var left = new Widget( Canvas );
		left.Layout = Layout.Column();
		left.Layout.Margin  = 8;
		left.Layout.Spacing = 6;
		left.MinimumWidth   = 340;
		left.MaximumWidth   = 360;
		Canvas.Layout.Add( left );

		// Load
		var loadBtn = new Button( "Load Spritesheet...", left );
		loadBtn.Clicked += LoadSpritesheet;
		left.Layout.Add( loadBtn );

		_infoLabel = new Label( "No file loaded.", left );
		_infoLabel.SetStyles( "color: #aaaaaa; font-size: 11px;" );
		left.Layout.Add( _infoLabel );

		left.Layout.AddSeparator();

		// Slice settings
		var sliceHeader = new Label( "Slice Settings", left );
		sliceHeader.SetStyles( "font-weight: bold;" );
		left.Layout.Add( sliceHeader );

		AddEditRow( left, "Columns:",          "4", out _colsEdit );
		AddEditRow( left, "Rows:",             "4", out _rowsEdit );
		AddEditRow( left, "Frame Width (px):", "0", out _fwEdit );
		AddEditRow( left, "Frame Height (px):","0", out _fhEdit );
		AddEditRow( left, "Padding (px):",     "0", out _padEdit );

		_colsEdit.TextEdited += v => { if ( int.TryParse( v, out var n ) ) { _columns = n; Refresh(); } };
		_rowsEdit.TextEdited += v => { if ( int.TryParse( v, out var n ) ) { _rows    = n; Refresh(); RebuildAnimList(); } };
		_fwEdit.TextEdited   += v => { if ( int.TryParse( v, out var n ) ) { _frameW  = n; Refresh(); } };
		_fhEdit.TextEdited   += v => { if ( int.TryParse( v, out var n ) ) { _frameH  = n; Refresh(); } };
		_padEdit.TextEdited  += v => { if ( int.TryParse( v, out var n ) ) { _padding = n; Refresh(); } };

		var hint = new Label( "Set frame size to 0 to auto-calculate from columns/rows.", left );
		hint.SetStyles( "color: #888888; font-size: 10px;" );
		left.Layout.Add( hint );

		left.Layout.AddSeparator();

		// Frame info
		_frameInfoLabel = new Label( "Load a spritesheet to begin.", left );
		_frameInfoLabel.SetStyles( "color: #aaaaaa; font-size: 11px;" );
		left.Layout.Add( _frameInfoLabel );

		left.Layout.AddSeparator();

		// Animations
		var animHeader = new Widget( left );
		animHeader.Layout = Layout.Row();
		var animLabel = new Label( "Animations", animHeader );
		animLabel.SetStyles( "font-weight: bold;" );
		animHeader.Layout.Add( animLabel, 1 );
		var addAnimBtn = new Button( "+ Add", animHeader );
		addAnimBtn.Clicked += AddAnimation;
		animHeader.Layout.Add( addAnimBtn );
		left.Layout.Add( animHeader );

		_animList = new Widget( left );
		_animList.Layout = Layout.Column();
		_animList.Layout.Spacing = 4;
		left.Layout.Add( _animList );

		RebuildAnimList();

		left.Layout.AddSeparator();

		// Output
		var outputHeader = new Label( "Output", left );
		outputHeader.SetStyles( "font-weight: bold;" );
		left.Layout.Add( outputHeader );

		AddEditRow( left, "Sprite name:",   "sprite", out _nameEdit );
		AddEditRow( left, "Output folder:", "",       out _dirEdit );
		_nameEdit.TextEdited += v => _outputName = v;
		_dirEdit.TextEdited  += v => _outputDir  = v;

		var browseBtn = new Button( "Browse Output Folder...", left );
		browseBtn.Clicked += BrowseDir;
		left.Layout.Add( browseBtn );

		left.Layout.AddStretchCell();

		_exportBtn = new Button( "Export Frames + Create Sprite", left );
		_exportBtn.Enabled = false;
		_exportBtn.Clicked += ExportFrames;
		_exportBtn.SetStyles( "font-weight: bold;" );
		left.Layout.Add( _exportBtn );

		// ── Right preview ─────────────────────────────────────────────────────
		_preview = new PreviewPane( Canvas );
		Canvas.Layout.Add( _preview, 1 );

		SetDefaultOutputDir();
	}

	// ── Animation list UI ─────────────────────────────────────────────────────

	private void RebuildAnimList()
	{
		if ( _animList is null ) return;

		// Clear existing rows
		foreach ( var child in _animList.Children.ToList() )
			child.Destroy();

		for ( int i = 0; i < _animations.Count; i++ )
		{
			var anim = _animations[i];
			var idx  = i;

			var row = new Widget( _animList );
			row.Layout = Layout.Column();
			row.Layout.Spacing = 2;
			row.SetStyles( "background-color: #2a2a2a; border-radius: 4px; padding: 4px;" );

			// Row 1: name + remove button
			var nameRow = new Widget( row );
			nameRow.Layout = Layout.Row();
			nameRow.Layout.Spacing = 4;
			var nameEdit = new LineEdit( nameRow );
			nameEdit.Text = anim.Name;
			nameEdit.TextEdited += v => anim.Name = v;
			nameRow.Layout.Add( nameEdit, 1 );
			var removeBtn = new Button( "✕", nameRow );
			removeBtn.MaximumWidth = 28;
			removeBtn.Clicked += () => { _animations.Remove( anim ); RebuildAnimList(); };
			nameRow.Layout.Add( removeBtn );
			row.Layout.Add( nameRow );

			// Row 2: start row / row count
			var rangeRow = new Widget( row );
			rangeRow.Layout = Layout.Row();
			rangeRow.Layout.Spacing = 4;

			var startLbl = new Label( "Start row:", rangeRow );
			startLbl.MinimumWidth = 60;
			rangeRow.Layout.Add( startLbl );
			var startEdit = new LineEdit( rangeRow );
			startEdit.Text = anim.StartRow.ToString();
			startEdit.TextEdited += v => { if ( int.TryParse( v, out var n ) ) anim.StartRow = Math.Max( 0, n ); };
			rangeRow.Layout.Add( startEdit, 1 );

			var countLbl = new Label( "Rows:", rangeRow );
			rangeRow.Layout.Add( countLbl );
			var countEdit = new LineEdit( rangeRow );
			countEdit.Text = anim.RowCount.ToString();
			countEdit.TextEdited += v => { if ( int.TryParse( v, out var n ) ) anim.RowCount = Math.Max( 1, n ); };
			rangeRow.Layout.Add( countEdit, 1 );
			row.Layout.Add( rangeRow );

			// Row 3: fps + loop mode
			var fpsRow = new Widget( row );
			fpsRow.Layout = Layout.Row();
			fpsRow.Layout.Spacing = 4;

			var fpsLbl = new Label( "FPS:", fpsRow );
			fpsLbl.MinimumWidth = 30;
			fpsRow.Layout.Add( fpsLbl );
			var fpsEdit = new LineEdit( fpsRow );
			fpsEdit.Text = anim.FrameRate.ToString();
			fpsEdit.TextEdited += v => { if ( float.TryParse( v, out var n ) ) anim.FrameRate = n; };
			fpsRow.Layout.Add( fpsEdit, 1 );

			var loopLbl = new Label( "Loop:", fpsRow );
			fpsRow.Layout.Add( loopLbl );
			var loopEdit = new LineEdit( fpsRow );
			loopEdit.Text = anim.LoopMode;
			loopEdit.TextEdited += v => anim.LoopMode = v;
			fpsRow.Layout.Add( loopEdit, 1 );
			row.Layout.Add( fpsRow );

			_animList.Layout.Add( row );
		}
	}

	private void AddAnimation()
	{
		_animations.Add( new AnimationDef
		{
			Name      = $"Animation{_animations.Count}",
			StartRow  = _animations.Count,
			RowCount  = 1,
			FrameRate = 10f,
			LoopMode  = "Loop"
		} );
		RebuildAnimList();
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private void SetDefaultOutputDir()
	{
		try
		{
			var assetsPath = Project.Current?.GetAssetsPath();
			if ( !string.IsNullOrEmpty( assetsPath ) && Directory.Exists( assetsPath ) )
			{
				_outputDir    = assetsPath;
				_dirEdit.Text = assetsPath;
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

	// ── Load ──────────────────────────────────────────────────────────────────

	private void LoadSpritesheet()
	{
		var path = EditorUtility.OpenFileDialog( "Load Spritesheet", "Images (*.png *.jpg *.tga *.bmp)", "" );
		if ( string.IsNullOrEmpty( path ) ) return;

		try
		{
			_sourcePath   = path;
			_sourceBitmap = Bitmap.CreateFromBytes( File.ReadAllBytes( path ) );

			if ( _sourceBitmap is null || !_sourceBitmap.IsValid )
			{
				_infoLabel.Text = "Failed to load image — unsupported format?";
				return;
			}

			_outputName    = Path.GetFileNameWithoutExtension( path );
			_nameEdit.Text = _outputName;
			_infoLabel.Text = $"{Path.GetFileName( path )}  ({_sourceBitmap.Width} × {_sourceBitmap.Height} px)";
			_exportBtn.Enabled = true;

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

	// ── Browse ────────────────────────────────────────────────────────────────

	private void BrowseDir()
	{
		var fd = new FileDialog( null );
		fd.Title = "Select Output Folder";
		fd.SetFindDirectory();
		var assetsPath = Project.Current?.GetAssetsPath();
		if ( !string.IsNullOrEmpty( assetsPath ) )
			fd.Directory = assetsPath;
		if ( !fd.Execute() ) return;
		_outputDir    = fd.SelectedFile;
		_dirEdit.Text = _outputDir;
	}

	// ── Export ────────────────────────────────────────────────────────────────

	private void ExportFrames()
	{
		if ( _sourceBitmap is null ) return;

		try
		{
			var ( cols, rows, fw, fh ) = GetParams( _sourceBitmap.Width, _sourceBitmap.Height );

			if ( string.IsNullOrEmpty( _outputDir ) )
				_outputDir = Path.GetDirectoryName( _sourcePath );

			Directory.CreateDirectory( _outputDir );

			// ── Step 1: Export each frame as a PNG ───────────────────────────────
			// Calculate relative path from Assets folder once.
			// S&box expects paths like "subfolder/filename.png" relative to Assets root.
			var assetsRoot = Path.GetFullPath( Project.Current?.GetAssetsPath() ?? _outputDir );
			var outputFull = Path.GetFullPath( _outputDir );
			var frameNames = new string[rows, cols];

			// Build the subfolder prefix — e.g. if Assets=C:/proj/Assets and output=C:/proj/Assets/zombies
			// then subFolder = "zombies/"
			string subFolder;
			if ( outputFull.StartsWith( assetsRoot, StringComparison.OrdinalIgnoreCase ) )
			{
				subFolder = outputFull.Substring( assetsRoot.Length )
					.TrimStart( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar )
					.Replace( '\\', '/' );
				if ( subFolder.Length > 0 && !subFolder.EndsWith( '/' ) )
					subFolder += "/";
			}
			else
			{
				// Output is outside assets — just use folder name as prefix
				subFolder = Path.GetFileName( outputFull ) + "/";
			}


			for ( int r = 0; r < rows; r++ )
			for ( int c = 0; c < cols; c++ )
			{
				int sx = _padding + c * ( fw + _padding );
				int sy = _padding + r * ( fh + _padding );

				var frame    = _sourceBitmap.Crop( new Rect( sx, sy, fw, fh ) );
				var pixmap   = Pixmap.FromBitmap( frame );
				var fileName = $"{_outputName}_r{r}_c{c}.png";
				pixmap.SavePng( Path.Combine( _outputDir, fileName ) );

				var relPath = subFolder + fileName;
				frameNames[r, c] = relPath;
			}

			// ── Step 2: Build .sprite JSON ───────────────────────────────────────
			var sb = new StringBuilder();
			sb.AppendLine( "{" );
			sb.AppendLine( "  \"Animations\": [" );

			for ( int a = 0; a < _animations.Count; a++ )
			{
				var anim     = _animations[a];
				var lastAnim = a == _animations.Count - 1;

				sb.AppendLine( "    {" );
				sb.AppendLine( $"      \"Name\": \"{anim.Name}\"," );
				sb.AppendLine( $"      \"FrameRate\": {anim.FrameRate}," );
				sb.AppendLine( $"      \"Origin\": \"{anim.Origin}\"," );
				sb.AppendLine( $"      \"LoopMode\": \"{anim.LoopMode}\"," );
				sb.AppendLine( "      \"Frames\": [" );

				// Collect all frames for this animation's rows
				var frames = new List<string>();
				int endRow = Math.Min( anim.StartRow + anim.RowCount, rows );
				for ( int r = anim.StartRow; r < endRow; r++ )
				for ( int c = 0; c < cols; c++ )
					frames.Add( frameNames[r, c] );

				for ( int f = 0; f < frames.Count; f++ )
				{
					var comma = f < frames.Count - 1 ? "," : "";
					sb.AppendLine( "        {" );
					sb.AppendLine( "          \"Texture\": {" );
					sb.AppendLine( "            \"$compiler\": \"texture\"," );
					sb.AppendLine( "            \"$source\": \"imagefile\"," );
					sb.AppendLine( "            \"data\": {" );
					sb.AppendLine( $"              \"FilePath\": \"{frames[f]}\"," );
					sb.AppendLine( "              \"MaxSize\": 4096," );
					sb.AppendLine( "              \"ConvertHeightToNormals\": false," );
					sb.AppendLine( "              \"NormalScale\": 1," );
					sb.AppendLine( "              \"Rotate\": 0," );
					sb.AppendLine( "              \"FlipVertical\": false," );
					sb.AppendLine( "              \"FlipHorizontal\": false," );
					sb.AppendLine( "              \"Cropping\": { \"Left\": 0, \"Top\": 0, \"Right\": 0, \"Bottom\": 0 }," );
					sb.AppendLine( "              \"Padding\": { \"Left\": 0, \"Top\": 0, \"Right\": 0, \"Bottom\": 0 }," );
					sb.AppendLine( "              \"InvertColor\": false," );
					sb.AppendLine( "              \"Tint\": \"1,1,1,1\"," );
					sb.AppendLine( "              \"Blur\": 0," );
					sb.AppendLine( "              \"Sharpen\": 0," );
					sb.AppendLine( "              \"Brightness\": 1," );
					sb.AppendLine( "              \"Contrast\": 1," );
					sb.AppendLine( "              \"Saturation\": 1," );
					sb.AppendLine( "              \"Hue\": 0," );
					sb.AppendLine( "              \"Colorize\": false," );
					sb.AppendLine( "              \"TargetColor\": \"1,1,1,1\"," );
					sb.AppendLine( "              \"CacheToDisk\": true" );
					sb.AppendLine( "            }," );
					sb.AppendLine( "            \"compiled\": null" );
					sb.AppendLine( "          }," );
					sb.AppendLine( "          \"BroadcastMessages\": []" );
					sb.AppendLine( $"        }}{comma}" );
				}

				sb.AppendLine( "      ]" );
				sb.AppendLine( lastAnim ? "    }" : "    }," );
			}

			sb.AppendLine( "  ]," );
			sb.AppendLine( "  \"__references\": []," );
			sb.AppendLine( "  \"__version\": 0" );
			sb.AppendLine( "}" );

			var spritePath = Path.Combine( _outputDir, $"{_outputName}.sprite" );
			File.WriteAllText( spritePath, sb.ToString() );

			ShowMessage( $"Done!\n\nExported frames and created:\n{_outputName}.sprite\n\nAnimations: {string.Join( ", ", _animations.ConvertAll( a => a.Name ) )}" );
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

	// ── Preview ───────────────────────────────────────────────────────────────

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
			_pixmap = Pixmap.FromBitmap( bmp );
			_bmpW   = bmp.Width;
			_bmpH   = bmp.Height;
			Update();
		}

		protected override void OnPaint()
		{
			Paint.SetBrush( new Color( 0.15f, 0.15f, 0.15f, 1f ) );
			Paint.ClearPen();
			Paint.DrawRect( LocalRect );

			if ( _pixmap is null ) return;

			float scale = Math.Min( Width / (float)_bmpW, Height / (float)_bmpH );
			float drawW = _bmpW * scale;
			float drawH = _bmpH * scale;
			float drawX = ( Width  - drawW ) * 0.5f;
			float drawY = ( Height - drawH ) * 0.5f;

			Paint.Draw( new Rect( drawX, drawY, drawW, drawH ), _pixmap );
		}
	}
}