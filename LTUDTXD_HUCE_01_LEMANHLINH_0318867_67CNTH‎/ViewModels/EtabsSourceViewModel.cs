using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Commands;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.ViewModels;

public sealed class EtabsSourceViewModel : PageViewModelBase
{
    private readonly IEtabsService _etabsService;
    private readonly BeamDesignInput _input;
    private readonly Dictionary<string, SavedBeamInput> _savedInputsByFrame = new();
    private string _summary;
    private string _connectionStatus = "Chưa kết nối";
    private string _connectionMessage = "Hãy mở ETABS và mô hình cần đọc trước khi kết nối.";
    private string _modelName = "Nguồn nội lực từ ETABS";
    private string _modelPath = string.Empty;
    private string _etabsVersion = "—";
    private string _statusBrush = "#F59E0B";
    private EtabsFrameItem? _selectedFrame;
    private bool _isConnected;

    public EtabsSourceViewModel(
        string summary,
        ObservableCollection<InternalForceItem> internalForces,
        BeamDesignInput input,
        IEtabsService etabsService)
        : base("Nguồn ETABS", "Kết nối ETABS và lấy nội lực dầm thép dùng cho tính toán.")
    {
        _summary = summary;
        InternalForces = internalForces;
        _input = input;
        _etabsService = etabsService;

        ConnectEtabsCommand = new RelayCommand(_ => ConnectEtabs(), _ => !IsConnected);
        DisconnectEtabsCommand = new RelayCommand(_ => DisconnectEtabs(), _ => IsConnected);
        LoadFrameListCommand = new RelayCommand(_ => LoadFrameList(), _ => IsConnected);
        ReadInternalForcesCommand = new RelayCommand(
            _ => ReadInternalForces(),
            _ => CanReadSelectedFrame);
    }

    public ObservableCollection<InternalForceItem> InternalForces { get; }
    public ObservableCollection<EtabsFrameItem> Frames { get; } = [];
    public ObservableCollection<InternalForceDiagramSeries> InternalForceDiagrams { get; } = [];
    public ICommand ConnectEtabsCommand { get; }
    public ICommand DisconnectEtabsCommand { get; }
    public ICommand LoadFrameListCommand { get; }
    public ICommand ReadInternalForcesCommand { get; }

    public string Summary { get => _summary; private set => SetField(ref _summary, value); }
    public string FrameCountText => Frames.Count == 0 ? "Chưa có dữ liệu" : $"{Frames.Count} dầm";
    public Visibility FrameEmptyVisibility => Frames.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FrameGridVisibility => Frames.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    public bool CanReadSelectedFrame => IsConnected;
    public string SelectedFrameText => SelectedFrame is null
        ? "Chưa chọn dầm"
        : $"{SelectedFrame.Name} | {SelectedFrame.SectionName} | L = {SelectedFrame.SpanLengthText}";
    public string SelectedFrameSaveText => SelectedFrame is null
        ? "Chưa chọn dầm ETABS"
        : $"Dầm đang chọn: {SelectedFrame.Name} | {SelectedFrame.SectionName}";
    public Visibility ForceResultEmptyVisibility => InternalForces.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ForceResultListVisibility => InternalForces.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetField(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(CanReadSelectedFrame));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ConnectionStatus { get => _connectionStatus; private set => SetField(ref _connectionStatus, value); }
    public string ConnectionMessage { get => _connectionMessage; private set => SetField(ref _connectionMessage, value); }
    public string ModelName { get => _modelName; private set => SetField(ref _modelName, value); }
    public string ModelPath { get => _modelPath; private set => SetField(ref _modelPath, value); }
    public string EtabsVersion { get => _etabsVersion; private set => SetField(ref _etabsVersion, value); }
    public string StatusBrush { get => _statusBrush; private set => SetField(ref _statusBrush, value); }

    public EtabsFrameItem? SelectedFrame
    {
        get => _selectedFrame;
        set
        {
            if (SetField(ref _selectedFrame, value))
            {
                OnPropertyChanged(nameof(CanReadSelectedFrame));
                OnPropertyChanged(nameof(SelectedFrameText));
                OnPropertyChanged(nameof(SelectedFrameSaveText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string SaveCurrentInputForSelectedFrame(BeamDesignInput input)
    {
        if (SelectedFrame is null)
        {
            return "Chưa chọn dầm ETABS để lưu thông số.";
        }

        _savedInputsByFrame[SelectedFrame.Name] = SavedBeamInput.From(input);
        return $"Đã lưu thông số cho dầm {SelectedFrame.Name} ({SelectedFrame.SectionName}).";
    }

    private void ConnectEtabs()
    {
        ConnectionStatus = "Đang kết nối...";
        ConnectionMessage = "Đang tìm phiên ETABS đang chạy.";
        StatusBrush = "#F59E0B";

        EtabsConnectionResult result = _etabsService.ConnectToRunningInstance();
        IsConnected = result.IsSuccess;

        if (!result.IsSuccess)
        {
            ResetLoadedFrameData();
            ConnectionStatus = "Kết nối thất bại";
            ConnectionMessage = result.Message;
            StatusBrush = "#F43F5E";
            return;
        }

        ModelName = result.ModelName;
        ModelPath = result.ModelPath;
        EtabsVersion = result.Version;
        ResetLoadedFrameData();
        ConnectionStatus = "Đã kết nối";
        ConnectionMessage = result.Message + " Bấm Tải danh sách để lấy danh sách dầm vào bảng.";
        StatusBrush = "#20C9B0";
        Summary = $"Đang kết nối: {result.ModelName} | ETABS {result.Version}";
    }

    public string? SyncSelectedFrameForCheck()
    {
        if (!IsConnected)
        {
            return null;
        }

        return ReadInternalForces()
            ? null
            : ConnectionMessage;
    }

    private void LoadFrameList()
    {
        try
        {
            Frames.Clear();
            foreach (EtabsFrameItem frame in _etabsService.GetFrameList())
            {
                Frames.Add(frame);
            }

            SelectedFrame = Frames.Count > 0 ? Frames[0] : null;
            RefreshFrameTableState();
            ConnectionMessage = Frames.Count > 0
                ? $"Đã tải {Frames.Count} dầm Frame từ ETABS. Chọn một dòng trong bảng rồi bấm Kiểm tra."
                : "Không tìm thấy dầm Frame nào trong mô hình ETABS.";
            StatusBrush = Frames.Count > 0 ? "#20C9B0" : "#F59E0B";
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception exception)
        {
            Frames.Clear();
            SelectedFrame = null;
            RefreshFrameTableState();
            ConnectionMessage = $"Không thể tải danh sách dầm: {exception.Message}";
            StatusBrush = "#F43F5E";
        }
    }

    private bool ReadInternalForces()
    {
        if (SelectedFrame is null && !IsConnected)
        {
            ConnectionMessage = "Hãy chọn một dầm trong bảng danh sách dầm trước khi đọc dữ liệu.";
            StatusBrush = "#F43F5E";
            return false;
        }

        EtabsForceReadResult result = SelectedFrame is null
            ? _etabsService.ReadSelectedFrameForces()
            : _etabsService.ReadFrameForces(SelectedFrame.Name);
        if (!result.IsSuccess)
        {
            ConnectionMessage = result.Message;
            StatusBrush = "#F43F5E";
            return false;
        }

        if (SelectedFrame is null)
        {
            SelectedFrame = Frames.FirstOrDefault(frame =>
                frame.Name.Equals(result.FrameName, StringComparison.OrdinalIgnoreCase));
        }

        _input.Height = result.Height;
        _input.FlangeWidth = result.FlangeWidth;
        _input.WebThickness = result.WebThickness;
        _input.FlangeThickness = result.FlangeThickness;
        _input.SpanLength = result.SpanLength;
        _input.UnbracedLength = result.UnbracedLength;
        _input.DesignStrength = result.YieldStrength;
        _input.ElasticModulus = result.ElasticModulus;
        _input.SteelGrade = result.MaterialName;
        _input.WorkingConditionFactor = result.WorkingConditionFactor;
        _input.LateralTorsionalBucklingFactor =
            result.SuggestedLateralTorsionalBucklingFactor;
        if (_savedInputsByFrame.TryGetValue(result.FrameName, out SavedBeamInput? savedInput))
        {
            savedInput.ApplyTo(_input);
        }
        _input.DesignMoment = Math.Abs(result.MaximumMoment);
        _input.DesignShear = Math.Abs(result.MaximumShear);
        _input.ShearAtMaxMoment = Math.Abs(result.ShearAtMaximumMoment);

        double scale = Math.Max(
            Math.Abs(result.MaximumMoment),
            Math.Max(
                Math.Abs(result.MinimumMoment),
                Math.Max(Math.Abs(result.MaximumShear), Math.Abs(result.AxialForce))));
        scale = scale <= 0 ? 1 : scale;

        InternalForces.Clear();
        InternalForces.Add(CreateForce(
            $"M3 max tại x = {result.GoverningStation:0.###} m", result.GoverningMomentCase,
            result.MaximumMoment, "kN.m", "#2D6CDF", scale));
        InternalForces.Add(CreateForce(
            $"V2 max tại x= {result.GoverningShearStation:0.###} m", result.GoverningShearCase,
            result.MaximumShear, "kN", "#4A948D", scale));
        InternalForces.Add(CreateForce(
            "V2 tại vị trí M3 max", result.GoverningMomentCase,
            result.ShearAtMaximumMoment, "kN", "#4A948D", scale));
        InternalForces.Add(CreateForce(
            "V3 tại vị trí M3 max", result.GoverningShearCase,
            result.SecondaryShear, "kN", "#2563EB", scale));
        InternalForces.Add(CreateForce(
            "P tại vị trí M3 max", result.GoverningShearCase,
            result.AxialForce, "kN", "#C58C18", scale));
        InternalForces.Add(CreateForce(
            "Mmin", result.GoverningMomentCase,
            result.MinimumMoment, "kN.m", "#E11D48", scale));
        RefreshForceResultState();
        ReplaceDiagramSeries(result);

        string axialWarning = Math.Abs(result.AxialForce) > 1.0
            ? $" LƯU Ý: dầm có lực dọc P = {result.AxialForce:0.##} kN; "
                + "chương trình chưa xét ảnh hưởng của lực dọc, cần kiểm tra bổ sung."
            : string.Empty;

        ConnectionMessage = result.Message
            + $" Đã đồng bộ tiết diện {result.SectionName}, vật liệu {result.MaterialName}, "
            + "hình học, chiều dài, M3 max (bền uốn), V2 max toàn dầm (bền cắt) "
            + "và V2 tại tiết diện M3 max (tương tác) sang màn hình thông số dầm."
            + "Biểu đồ nội lực đang hiển thị M3, V2 và V3 của tổ hợp chi phối. "
            + "Hệ số φb đang là giá trị GỢI Ý tự động; phải đối chiếu TCVN 5575 "
            + "cho sơ đồ giữ cánh nén thực tế trước khi dùng cho hồ sơ thiết kế"
            +axialWarning;
        StatusBrush = "#20C9B0";
        Summary = $"Phần tử {result.FrameName} | Biểu đồ M, V, Q từ ETABS";
        return true;
    }

    private void DisconnectEtabs()
    {
        _etabsService.Disconnect();
        IsConnected = false;
        ConnectionStatus = "Đã ngắt kết nối";
        ConnectionMessage = "Kết nối ETABS đã được đóng an toàn.";
        ModelName = "Nguồn nội lực từ ETABS";
        ModelPath = string.Empty;
        EtabsVersion = "—";
        Frames.Clear();
        SelectedFrame = null;
        RefreshFrameTableState();
        InternalForces.Clear();
        InternalForceDiagrams.Clear();
        StatusBrush = "#F59E0B";
        RefreshForceResultState();
        Summary = "Nội lực dầm lấy từ ETABS | M, V, tổ hợp chi phối";
        CommandManager.InvalidateRequerySuggested();
    }

    private void ResetLoadedFrameData()
    {
        Frames.Clear();
        SelectedFrame = null;
        InternalForces.Clear();
        InternalForceDiagrams.Clear();
        RefreshFrameTableState();
        RefreshForceResultState();
    }

    private void RefreshFrameTableState()
    {
        OnPropertyChanged(nameof(FrameCountText));
        OnPropertyChanged(nameof(FrameEmptyVisibility));
        OnPropertyChanged(nameof(FrameGridVisibility));
        OnPropertyChanged(nameof(SelectedFrameText));
        OnPropertyChanged(nameof(SelectedFrameSaveText));
        OnPropertyChanged(nameof(CanReadSelectedFrame));
    }

    private void RefreshForceResultState()
    {
        OnPropertyChanged(nameof(ForceResultEmptyVisibility));
        OnPropertyChanged(nameof(ForceResultListVisibility));
    }

    private static InternalForceItem CreateForce(
        string name,
        string direction,
        double value,
        string unit,
        string brush,
        double scale) =>
        new()
        {
            Name = name,
            Direction = direction,
            ValueText = $"{value:0.###} {unit}",
            BarHeight = 210 * Math.Abs(value) / scale,
            BarBrush = brush
        };

    private void ReplaceDiagramSeries(EtabsForceReadResult result)
    {
        InternalForceDiagrams.Clear();

        IReadOnlyList<EtabsForceDiagramPoint> points = result.DiagramPoints;
        if (points.Count == 0)
        {
            //Không đủ dữ liệu điểm: không vẽ biểu đồ thay vì tự tạo điểm giả
            //Dễ gây hiểu nhầm về hình dạng biểu đồ nội lực thật
            return;
        }

        InternalForceDiagrams.Add(CreateDiagramSeries(
            "M",
            "kN.m",
            result.GoverningMomentCase,
            "#E11D48",
            164,
            points,
            point => point.Moment));
        InternalForceDiagrams.Add(CreateDiagramSeries(
            "V",
            "kN",
            result.GoverningShearCase,
            "#20C9B0",
            96,
            points,
            point => point.Shear));
        InternalForceDiagrams.Add(CreateDiagramSeries(
            "Q",
            "kN",
            result.GoverningMomentCase,
            "#2563EB",
            232,
            points,
            point => point.SecondaryShear));
    }

    private static InternalForceDiagramSeries CreateDiagramSeries(
        string name,
        string unit,
        string caseName,
        string stroke,
        double axisY,
        IReadOnlyList<EtabsForceDiagramPoint> source,
        Func<EtabsForceDiagramPoint, double> valueSelector)        
    {
        const double left = 66;
        const double width = 454;
        const double halfHeight = 24;

        double minStation = source.Min(point => point.Station);
        double maxStation = source.Max(point => point.Station);
        double span = Math.Max(maxStation - minStation, 0.0001);
        double maxAbs = source.Max(point => Math.Abs(valueSelector(point)));
        maxAbs = maxAbs <= 0 ? 1 : maxAbs;

        PointCollection polyline = [];
        List<InternalForceDiagramHatch> hatches = [];
        List<Point> orderedPoints = [];
        List<(EtabsForceDiagramPoint Source, double Value, Point Diagram)> diagramPoints = [];
        foreach (EtabsForceDiagramPoint point in source.OrderBy(point => point.Station))
        {
            double value = valueSelector(point);
            double x = left + (point.Station - minStation) / span * width;            
            double y = axisY - value / maxAbs * halfHeight;
            Point diagramPoint = new(x, y);
            polyline.Add(diagramPoint);
            orderedPoints.Add(diagramPoint);
            diagramPoints.Add((point, value, diagramPoint));
        }

        PointCollection areaPoints = [];
        if (orderedPoints.Count > 0)
        {
            areaPoints.Add(new Point(orderedPoints[0].X, axisY));
            foreach (Point point in orderedPoints)
            {
                areaPoints.Add(point);
            }

            areaPoints.Add(new Point(orderedPoints[orderedPoints.Count - 1].X, axisY));

            int step = Math.Max(1, orderedPoints.Count / 18);
            for (int index = 0; index < orderedPoints.Count; index += step)
            {
                hatches.Add(new InternalForceDiagramHatch
                {
                    X = orderedPoints[index].X,
                    Y1 = axisY,
                    Y2 = orderedPoints[index].Y
                });
            }
        }

        double minValue = source.Min(valueSelector);
        double maxValue = source.Max(valueSelector);
        (EtabsForceDiagramPoint Source, double Value, Point Diagram) maxPoint =
            diagramPoints.OrderByDescending(point => point.Value).FirstOrDefault();
        (EtabsForceDiagramPoint Source, double Value, Point Diagram) minPoint =
            diagramPoints.OrderBy(point => point.Value).FirstOrDefault();

        double maxLabelX = Clamp(maxPoint.Diagram.X - 22, left, left + width - 92);
        double maxLabelY = Clamp(maxPoint.Diagram.Y - 24, axisY - halfHeight - 26, axisY + halfHeight - 4);
        double minLabelX = Clamp(minPoint.Diagram.X - 22, left, left + width - 92);
        double minLabelY = Clamp(minPoint.Diagram.Y + 6, axisY - halfHeight + 4, axisY + halfHeight + 8);

        if (Math.Abs(maxLabelX - minLabelX) < 58 && Math.Abs(maxLabelY - minLabelY) < 18)
        {
            minLabelY = Clamp(minLabelY + 16, axisY - halfHeight + 4, axisY + halfHeight + 10);
        }

        return new InternalForceDiagramSeries
        {
            Name = name,
            Unit = unit,
            CaseName = caseName,
            RangeText = $"{minValue:0.###} .. {maxValue:0.###} {unit}",
            Stroke = stroke,
            AxisY = axisY,
            MaxLabelText = $"max {maxValue:0.###} {unit}",
            MaxLabelX = maxLabelX,
            MaxLabelY = maxLabelY,
            MaxMarkerX = maxPoint.Diagram.X - 3.5,
            MaxMarkerY = maxPoint.Diagram.Y - 3.5,
            MinLabelText = $"min {minValue:0.###} {unit}",
            MinLabelX = minLabelX,
            MinLabelY = minLabelY,
            MinMarkerX = minPoint.Diagram.X - 3.5,
            MinMarkerY = minPoint.Diagram.Y - 3.5,
            Points = polyline,
            AreaPoints = areaPoints,
            Hatches = hatches
        };
    }

    private static double Clamp(double value, double min, double max) =>
        Math.Max(min, Math.Min(max, value));

    private sealed class SavedBeamInput
    {
        public double Height { get; private set; }
        public double FlangeWidth { get; private set; }
        public double WebThickness { get; private set; }
        public double FlangeThickness { get; private set; }
        public double SpanLength { get; private set; }
        public double UnbracedLength { get; private set; }
        public double DesignStrength { get; private set; }
        public double ElasticModulus { get; private set; }
        public double DesignMoment { get; private set; }
        public double DesignShear { get; private set; }
        public double ShearAtMaxMoment { get; private set; }
        public double WorkingConditionFactor { get; private set; }
        public double LateralTorsionalBucklingFactor { get; private set; }
        public string SteelGrade { get; private set; } = string.Empty;

        public static SavedBeamInput From(BeamDesignInput input) =>
            new()
            {
                Height = input.Height,
                FlangeWidth = input.FlangeWidth,
                WebThickness = input.WebThickness,
                FlangeThickness = input.FlangeThickness,
                SpanLength = input.SpanLength,
                UnbracedLength = input.UnbracedLength,
                DesignStrength = input.DesignStrength,
                ElasticModulus = input.ElasticModulus,
                DesignMoment = input.DesignMoment,
                DesignShear = input.DesignShear,
                ShearAtMaxMoment = input.ShearAtMaxMoment,
                WorkingConditionFactor = input.WorkingConditionFactor,
                LateralTorsionalBucklingFactor = input.LateralTorsionalBucklingFactor,
                SteelGrade = input.SteelGrade
            };

        public void ApplyTo(BeamDesignInput input)
        {
            input.Height = Height;
            input.FlangeWidth = FlangeWidth;
            input.WebThickness = WebThickness;
            input.FlangeThickness = FlangeThickness;
            input.SpanLength = SpanLength;
            input.UnbracedLength = UnbracedLength;
            input.DesignStrength = DesignStrength;
            input.ElasticModulus = ElasticModulus;
            input.DesignMoment = DesignMoment;
            input.DesignShear = DesignShear;
            input.ShearAtMaxMoment = ShearAtMaxMoment;
            input.WorkingConditionFactor = WorkingConditionFactor;
            input.LateralTorsionalBucklingFactor = LateralTorsionalBucklingFactor;
            input.SteelGrade = SteelGrade;
        }
    }

    private bool SetField<T>(
        ref T field,
        T value,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
