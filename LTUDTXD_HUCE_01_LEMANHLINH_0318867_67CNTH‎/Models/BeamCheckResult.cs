namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Models;

public sealed class BeamCheckResult
{
    public string Group { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Limit { get; set; } = string.Empty;

    public string CurrentValue { get; set; } = string.Empty;

    public double UtilizationPercent { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusBrush { get; set; } = "#1E8E5A";

    public string Note { get; set; } = string.Empty;
}
