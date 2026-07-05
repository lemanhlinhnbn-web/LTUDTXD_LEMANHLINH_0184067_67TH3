using System.Collections.Generic;
using System.Windows.Media;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

public sealed class InternalForceDiagramSeries
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public string RangeText { get; set; } = string.Empty;
    public string Stroke { get; set; } = "#2D6CDF";
    public double AxisY { get; set; }
    public string MaxLabelText { get; set; } = string.Empty;
    public double MaxLabelX { get; set; }
    public double MaxLabelY { get; set; }
    public double MaxMarkerX { get; set; }
    public double MaxMarkerY { get; set; }
    public string MinLabelText { get; set; } = string.Empty;
    public double MinLabelX { get; set; }
    public double MinLabelY { get; set; }
    public double MinMarkerX { get; set; }
    public double MinMarkerY { get; set; }
    public PointCollection Points { get; set; } = [];
    public PointCollection AreaPoints { get; set; } = [];
    public List<InternalForceDiagramHatch> Hatches { get; set; } = [];
}
