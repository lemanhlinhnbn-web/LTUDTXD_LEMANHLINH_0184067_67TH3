using System.Collections.Generic;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

public sealed class BeamCalculationResult
{
    public BeamSectionProperties Section { get; set; } = new();
    public IReadOnlyList<BeamCheckResult> StrengthChecks { get; set; } = [];
    public IReadOnlyList<BeamCheckResult> StabilityChecks { get; set; } = [];
    public double GoverningStrengthRatio { get; set; }
    public double GoverningStabilityRatio { get; set; }
    public string GoverningCondition { get; set; } = string.Empty;
    public bool IsPassed { get; set; }
}
