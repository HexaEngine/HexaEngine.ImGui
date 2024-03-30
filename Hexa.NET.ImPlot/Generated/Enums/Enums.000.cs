// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using System.Numerics;
using Hexa.NET.ImGui;

namespace Hexa.NET.ImPlot
{
	public enum ImAxis
	{
		X1 = unchecked(0),
		X2 = unchecked(1),
		X3 = unchecked(2),
		Y1 = unchecked(3),
		Y2 = unchecked(4),
		Y3 = unchecked(5),
		Count = unchecked(6),
	}

	public enum ImPlotFlags
	{
		None = unchecked(0),
		NoTitle = unchecked(1),
		NoLegend = unchecked(2),
		NoMouseText = unchecked(4),
		NoInputs = unchecked(8),
		NoMenus = unchecked(16),
		NoBoxSelect = unchecked(32),
		NoFrame = unchecked(64),
		Equal = unchecked(128),
		Crosshairs = unchecked(256),
		CanvasOnly = unchecked(55),
	}

	public enum ImPlotAxisFlags
	{
		None = unchecked(0),
		NoLabel = unchecked(1),
		NoGridLines = unchecked(2),
		NoTickMarks = unchecked(4),
		NoTickLabels = unchecked(8),
		NoInitialFit = unchecked(16),
		NoMenus = unchecked(32),
		NoSideSwitch = unchecked(64),
		NoHighlight = unchecked(128),
		Opposite = unchecked(256),
		Foreground = unchecked(512),
		Invert = unchecked(1024),
		AutoFit = unchecked(2048),
		RangeFit = unchecked(4096),
		PanStretch = unchecked(8192),
		LockMin = unchecked(16384),
		LockMax = unchecked(32768),
		Lock = unchecked(49152),
		NoDecorations = unchecked(15),
		AuxDefault = unchecked(258),
	}

	public enum ImPlotSubplotFlags
	{
		None = unchecked(0),
		NoTitle = unchecked(1),
		NoLegend = unchecked(2),
		NoMenus = unchecked(4),
		NoResize = unchecked(8),
		NoAlign = unchecked(16),
		ShareItems = unchecked(32),
		LinkRows = unchecked(64),
		LinkCols = unchecked(128),
		LinkAllX = unchecked(256),
		LinkAllY = unchecked(512),
		ColMajor = unchecked(1024),
	}

	public enum ImPlotLegendFlags
	{
		None = unchecked(0),
		NoButtons = unchecked(1),
		NoHighlightItem = unchecked(2),
		NoHighlightAxis = unchecked(4),
		NoMenus = unchecked(8),
		Outside = unchecked(16),
		Horizontal = unchecked(32),
		Sort = unchecked(64),
	}

	public enum ImPlotMouseTextFlags
	{
		None = unchecked(0),
		NoAuxAxes = unchecked(1),
		NoFormat = unchecked(2),
		ShowAlways = unchecked(4),
	}

	public enum ImPlotDragToolFlags
	{
		None = unchecked(0),
		NoCursors = unchecked(1),
		NoFit = unchecked(2),
		NoInputs = unchecked(4),
		Delayed = unchecked(8),
	}

	public enum ImPlotColormapScaleFlags
	{
		None = unchecked(0),
		NoLabel = unchecked(1),
		Opposite = unchecked(2),
		Invert = unchecked(4),
	}

	public enum ImPlotItemFlags
	{
		None = unchecked(0),
		NoLegend = unchecked(1),
		NoFit = unchecked(2),
	}

	public enum ImPlotLineFlags
	{
		None = unchecked(0),
		Segments = unchecked(1024),
		Loop = unchecked(2048),
		SkipNaN = unchecked(4096),
		NoClip = unchecked(8192),
		Shaded = unchecked(16384),
	}

	public enum ImPlotScatterFlags
	{
		None = unchecked(0),
		NoClip = unchecked(1024),
	}

	public enum ImPlotStairsFlags
	{
		None = unchecked(0),
		PreStep = unchecked(1024),
		Shaded = unchecked(2048),
	}

	public enum ImPlotShadedFlags
	{
		None = unchecked(0),
	}

	public enum ImPlotBarsFlags
	{
		None = unchecked(0),
		Horizontal = unchecked(1024),
	}

	public enum ImPlotBarGroupsFlags
	{
		None = unchecked(0),
		Horizontal = unchecked(1024),
		Stacked = unchecked(2048),
	}

	public enum ImPlotErrorBarsFlags
	{
		None = unchecked(0),
		Horizontal = unchecked(1024),
	}

	public enum ImPlotStemsFlags
	{
		None = unchecked(0),
		Horizontal = unchecked(1024),
	}

	public enum ImPlotInfLinesFlags
	{
		None = unchecked(0),
		Horizontal = unchecked(1024),
	}

	public enum ImPlotPieChartFlags
	{
		None = unchecked(0),
		Normalize = unchecked(1024),
		IgnoreHidden = unchecked(2048),
	}

	public enum ImPlotHeatmapFlags
	{
		None = unchecked(0),
		ColMajor = unchecked(1024),
	}

	public enum ImPlotHistogramFlags
	{
		None = unchecked(0),
		Horizontal = unchecked(1024),
		Cumulative = unchecked(2048),
		Density = unchecked(4096),
		NoOutliers = unchecked(8192),
		ColMajor = unchecked(16384),
	}

	public enum ImPlotDigitalFlags
	{
		None = unchecked(0),
	}

	public enum ImPlotImageFlags
	{
		None = unchecked(0),
	}

	public enum ImPlotTextFlags
	{
		None = unchecked(0),
		Vertical = unchecked(1024),
	}

	public enum ImPlotDummyFlags
	{
		None = unchecked(0),
	}

	public enum ImPlotCond
	{
		None = unchecked((int)0),
		Always = unchecked((int)1),
		Once = unchecked((int)2),
	}

	public enum ImPlotCol
	{
		Line = unchecked(0),
		Fill = unchecked(1),
		MarkerOutline = unchecked(2),
		MarkerFill = unchecked(3),
		ErrorBar = unchecked(4),
		FrameBg = unchecked(5),
		Bg = unchecked(6),
		Border = unchecked(7),
		LegendBg = unchecked(8),
		LegendBorder = unchecked(9),
		LegendText = unchecked(10),
		TitleText = unchecked(11),
		InlayText = unchecked(12),
		AxisText = unchecked(13),
		AxisGrid = unchecked(14),
		AxisTick = unchecked(15),
		AxisBg = unchecked(16),
		AxisBgHovered = unchecked(17),
		AxisBgActive = unchecked(18),
		Selection = unchecked(19),
		Crosshairs = unchecked(20),
		Count = unchecked(21),
	}

	public enum ImPlotStyleVar
	{
		LineWeight = unchecked(0),
		Marker = unchecked(1),
		MarkerSize = unchecked(2),
		MarkerWeight = unchecked(3),
		FillAlpha = unchecked(4),
		ErrorBarSize = unchecked(5),
		ErrorBarWeight = unchecked(6),
		DigitalHeight = unchecked(7),
		DigitalGap = unchecked(8),
		BorderSize = unchecked(9),
		MinorAlpha = unchecked(10),
		MajorTickLen = unchecked(11),
		MinorTickLen = unchecked(12),
		MajorTickSize = unchecked(13),
		MinorTickSize = unchecked(14),
		MajorGridSize = unchecked(15),
		MinorGridSize = unchecked(16),
		Padding = unchecked(17),
		LabelPadding = unchecked(18),
		LegendPadding = unchecked(19),
		LegendInnerPadding = unchecked(20),
		LegendSpacing = unchecked(21),
		MousePosPadding = unchecked(22),
		AnnotationPadding = unchecked(23),
		FitPadding = unchecked(24),
		DefaultSize = unchecked(25),
		MinSize = unchecked(26),
		Count = unchecked(27),
	}

	public enum ImPlotScale
	{
		Linear = unchecked(0),
		Time = unchecked(1),
		Log10 = unchecked(2),
		SymLog = unchecked(3),
	}

	public enum ImPlotMarker
	{
		None = unchecked(-1),
		Circle = unchecked(0),
		Square = unchecked(1),
		Diamond = unchecked(2),
		Up = unchecked(3),
		Down = unchecked(4),
		Left = unchecked(5),
		Right = unchecked(6),
		Cross = unchecked(7),
		Plus = unchecked(8),
		Asterisk = unchecked(9),
		Count = unchecked(10),
	}

	public enum ImPlotColormap
	{
		Deep = unchecked(0),
		Dark = unchecked(1),
		Pastel = unchecked(2),
		Paired = unchecked(3),
		Viridis = unchecked(4),
		Plasma = unchecked(5),
		Hot = unchecked(6),
		Cool = unchecked(7),
		Pink = unchecked(8),
		Jet = unchecked(9),
		Twilight = unchecked(10),
		RdBu = unchecked(11),
		BrBg = unchecked(12),
		PiYg = unchecked(13),
		Spectral = unchecked(14),
		Greys = unchecked(15),
	}

	public enum ImPlotLocation
	{
		Center = unchecked(0),
		North = unchecked(1),
		South = unchecked(2),
		West = unchecked(4),
		East = unchecked(8),
		NorthWest = unchecked(5),
		NorthEast = unchecked(9),
		SouthWest = unchecked(6),
		SouthEast = unchecked(10),
	}

	public enum ImPlotBin
	{
		Sqrt = unchecked(-1),
		Sturges = unchecked(-2),
		Rice = unchecked(-3),
		Scott = unchecked(-4),
	}

	public enum ImPlotTimeUnit
	{
		Us = unchecked(0),
		Ms = unchecked(1),
		S = unchecked(2),
		Min = unchecked(3),
		Hr = unchecked(4),
		Day = unchecked(5),
		Mo = unchecked(6),
		Yr = unchecked(7),
		Count = unchecked(8),
	}

	public enum ImPlotDateFmt
	{
		None = unchecked(0),
		DayMo = unchecked(1),
		DayMoYr = unchecked(2),
		MoYr = unchecked(3),
		Mo = unchecked(4),
		Yr = unchecked(5),
	}

	public enum ImPlotTimeFmt
	{
		None = unchecked(0),
		Us = unchecked(1),
		SUs = unchecked(2),
		SMs = unchecked(3),
		S = unchecked(4),
		MinSMs = unchecked(5),
		HrMinSMs = unchecked(6),
		HrMinS = unchecked(7),
		HrMin = unchecked(8),
		Hr = unchecked(9),
	}

}
