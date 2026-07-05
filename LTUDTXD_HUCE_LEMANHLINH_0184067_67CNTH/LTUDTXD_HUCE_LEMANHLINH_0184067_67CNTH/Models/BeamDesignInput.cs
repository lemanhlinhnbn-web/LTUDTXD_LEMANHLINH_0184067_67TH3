using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

public sealed class BeamDesignInput : INotifyPropertyChanged
{
    private double _height = 450;
    private double _flangeWidth = 200;
    private double _webThickness = 8;
    private double _flangeThickness = 12;
    private double _spanLength = 6;
    private double _unbracedLength = 3;
    private double _designStrength = 215;
    private double _elasticModulus = 206000;
    private double _designMoment = 186;
    private double _designShear = 124;
    private double _workingConditionFactor = 1;
    private double _lateralTorsionalBucklingFactor = 0.80;
    private string _steelGrade = "CT3";

    public event PropertyChangedEventHandler? PropertyChanged;

    public double Height { get => _height; set => SetField(ref _height, value); }
    public double FlangeWidth { get => _flangeWidth; set => SetField(ref _flangeWidth, value); }
    public double WebThickness { get => _webThickness; set => SetField(ref _webThickness, value); }
    public double FlangeThickness { get => _flangeThickness; set => SetField(ref _flangeThickness, value); }
    public double SpanLength { get => _spanLength; set => SetField(ref _spanLength, value); }
    public double UnbracedLength { get => _unbracedLength; set => SetField(ref _unbracedLength, value); }
    public double DesignStrength { get => _designStrength; set => SetField(ref _designStrength, value); }
    public double ElasticModulus { get => _elasticModulus; set => SetField(ref _elasticModulus, value); }
    public double DesignMoment { get => _designMoment; set => SetField(ref _designMoment, value); }
    public double DesignShear { get => _designShear; set => SetField(ref _designShear, value); }
    public double WorkingConditionFactor { get => _workingConditionFactor; set => SetField(ref _workingConditionFactor, value); }
    public double LateralTorsionalBucklingFactor { get => _lateralTorsionalBucklingFactor; set => SetField(ref _lateralTorsionalBucklingFactor, value); }

    public string SteelGrade
    {
        get => _steelGrade;
        set
        {
            if (_steelGrade == value)
            {
                return;
            }

            _steelGrade = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SteelGrade)));
        }
    }
    public string Standard { get; } = "TCVN 2275:2024";

    private void SetField(ref double field, double value, [CallerMemberName] string? propertyName = null)
    {
        if (field.Equals(value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
