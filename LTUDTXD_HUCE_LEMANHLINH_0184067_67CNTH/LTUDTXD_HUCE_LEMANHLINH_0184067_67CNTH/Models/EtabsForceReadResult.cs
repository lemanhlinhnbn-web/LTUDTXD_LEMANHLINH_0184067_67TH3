using System.Collections.Generic;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

public sealed class EtabsForceReadResult
{
    private EtabsForceReadResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public bool IsSuccess { get; }
    public string Message { get; }
    public string FrameName { get; private set; } = string.Empty;
    public string GoverningMomentCase { get; private set; } = string.Empty;
    public string GoverningShearCase { get; private set; } = string.Empty;
    public double MaximumMoment { get; private set; }
    public double MinimumMoment { get; private set; }
    public double MaximumShear { get; private set; }
    public double SecondaryShear { get; private set; }
    public double AxialForce { get; private set; }
    public double GoverningStation { get; private set; }
    public string SectionName { get; private set; } = string.Empty;
    public string MaterialName { get; private set; } = string.Empty;
    public double Height { get; private set; }
    public double FlangeWidth { get; private set; }
    public double WebThickness { get; private set; }
    public double FlangeThickness { get; private set; }
    public double SpanLength { get; private set; }
    public double UnbracedLength { get; private set; }
    public double YieldStrength { get; private set; }
    public double ElasticModulus { get; private set; }
    public double WorkingConditionFactor { get; private set; }
    public double SuggestedLateralTorsionalBucklingFactor { get; private set; }
    public IReadOnlyList<EtabsForceDiagramPoint> DiagramPoints { get; private set; } = [];

    public static EtabsForceReadResult Success(
        string frameName,
        double maximumMoment,
        double minimumMoment,
        double maximumShear,
        double secondaryShear,
        double axialForce,
        double governingStation,
        string momentCase,
        string shearCase,
        string sectionName,
        string materialName,
        double height,
        double flangeWidth,
        double webThickness,
        double flangeThickness,
        double spanLength,
        double yieldStrength,
        double elasticModulus,
        double suggestedLateralTorsionalBucklingFactor,
        IReadOnlyList<EtabsForceDiagramPoint>? diagramPoints = null) =>
        new(true, $"Đã chọn tổ hợp {momentCase} có |M3| lớn nhất của phần tử {frameName}.")
        {
            FrameName = frameName,
            MaximumMoment = maximumMoment,
            MinimumMoment = minimumMoment,
            MaximumShear = maximumShear,
            SecondaryShear = secondaryShear,
            AxialForce = axialForce,
            GoverningStation = governingStation,
            GoverningMomentCase = momentCase,
            GoverningShearCase = shearCase,
            SectionName = sectionName,
            MaterialName = materialName,
            Height = height,
            FlangeWidth = flangeWidth,
            WebThickness = webThickness,
            FlangeThickness = flangeThickness,
            SpanLength = spanLength,
            UnbracedLength = spanLength,
            YieldStrength = yieldStrength,
            ElasticModulus = elasticModulus,
            WorkingConditionFactor = 1.0,
            SuggestedLateralTorsionalBucklingFactor = suggestedLateralTorsionalBucklingFactor,
            DiagramPoints = diagramPoints ?? []
        };

    public static EtabsForceReadResult Failure(string message) => new(false, message);
}
