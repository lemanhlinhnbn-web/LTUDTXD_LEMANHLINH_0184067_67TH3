using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Commands;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Models;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Services;

namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly Dictionary<string, PageViewModelBase> _pages = new();
    private NavigationItem? _selectedNavigationItem;
    private PageViewModelBase _currentPageViewModel = null!;

    public MainViewModel(IEtabsService etabsService)
    {
        RunStabilityCheckCommand = new RelayCommand(_ => { });
        RunStrengthCheckCommand = new RelayCommand(_ => { });
        SaveReportCommand = new RelayCommand(_ => { });

        var etabsSourceViewModel = new EtabsSourceViewModel(
            InternalForceSummary,
            InternalForces,
            etabsService);

        etabsSourceViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(EtabsSourceViewModel.ModelName))
            {
                OnPropertyChanged(nameof(EtabsModelName));
            }

            if (args.PropertyName == nameof(EtabsSourceViewModel.ConnectionStatus))
            {
                OnPropertyChanged(nameof(LastSyncText));
            }
        };

        _pages["EtabsSource"] = etabsSourceViewModel;

        _pages["BeamParameters"] = new BeamParametersViewModel(BeamParameters);

        _pages["BeamChecks"] = new BeamCheckViewModel(
            StrengthChecks,
            StabilityChecks,
            RunStrengthCheckCommand,
            RunStabilityCheckCommand);

        NavigationItems.Add(new NavigationItem
        {
            Key = "EtabsSource",
            Icon = "\uE774",
            Title = "Nguồn ETABS",
            Description = "Lấy dữ liệu nội lực"
        });
        NavigationItems.Add(new NavigationItem
        {
            Key = "BeamParameters",
            Icon = "\uE9D2",
            Title = "Thông số dầm I",
            Description = "TCVN 5575:2012"
        });
        NavigationItems.Add(new NavigationItem
        {
            Key = "BeamChecks",
            Icon = "\uE9F9",
            Title = "Kiểm tra",
            Description = "Bền và ổn định"
        });

        SelectedNavigationItem = NavigationItems[0];
    }

    public string ProjectName { get; } = "TÍNH DẦM THÉP";

    public string StudentInfo { get; } = "LE MANH LINH | 0318867 | 67TH3";

    public string StructureName { get; } = "Dầm thép chữ I";

    public string EtabsModelName =>
        (_pages["EtabsSource"] as EtabsSourceViewModel)?.ModelName
        ?? "Nguồn nội lực từ ETABS";

    public string LastSyncText =>
        (_pages["EtabsSource"] as EtabsSourceViewModel)?.ConnectionStatus
        ?? "Chờ kết nối ETABS";

    public string InternalForceSummary { get; } = "Nội lực dầm lấy từ ETABS | M, V, tổ hợp chi phối";

    public ObservableCollection<NavigationItem> NavigationItems { get; } = [];

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (_selectedNavigationItem == value)
            {
                return;
            }

            _selectedNavigationItem = value;
            OnPropertyChanged();

            if (value is not null && _pages.TryGetValue(value.Key, out var page))
            {
                CurrentPageViewModel = page;
            }
        }
    }

    public PageViewModelBase CurrentPageViewModel
    {
        get => _currentPageViewModel;
        private set
        {
            if (_currentPageViewModel == value)
            {
                return;
            }

            _currentPageViewModel = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<BeamParameterItem> BeamParameters { get; } =
    [
        new() { Group = "Hình học", Symbol = "h", Name = "Chiều cao tiết diện", Value = "450", Unit = "mm" },
        new() { Group = "Hình học", Symbol = "b", Name = "Bề rộng cánh", Value = "200", Unit = "mm" },
        new() { Group = "Hình học", Symbol = "tw", Name = "Chiều dày bụng", Value = "8", Unit = "mm" },
        new() { Group = "Hình học", Symbol = "tf", Name = "Chiều dày cánh", Value = "12", Unit = "mm" },
        new() { Group = "Vật liệu", Symbol = "Ry", Name = "Cường độ tính toán", Value = "215", Unit = "MPa" },
        new() { Group = "Vật liệu", Symbol = "E", Name = "Mô đun đàn hồi", Value = "2.06E5", Unit = "MPa" },
        new() { Group = "Nội lực", Symbol = "Mx", Name = "Mô men uốn thiết kế", Value = "186", Unit = "kN.m" },
        new() { Group = "Nội lực", Symbol = "V", Name = "Lực cắt thiết kế", Value = "124", Unit = "kN" }
    ];

    public ObservableCollection<InternalForceItem> InternalForces { get; } =
    [
        new() { Name = "Mmax", Direction = "ULS", ValueText = "186 kN.m", BarHeight = 172, BarBrush = "#1D4F8F" },
        new() { Name = "Vmax", Direction = "ULS", ValueText = "124 kN", BarHeight = 118, BarBrush = "#3F7F7A" },
        new() { Name = "Mmin", Direction = "ULS", ValueText = "-42 kN.m", BarHeight = 68, BarBrush = "#B9851A" }
    ];

    public ObservableCollection<BeamCheckResult> StrengthChecks { get; } =
    [
        new()
        {
            Group = "Bền uốn",
            Name = "Ứng suất pháp do Mx",
            Limit = "σ <= Ryγc",
            CurrentValue = "0.86",
            UtilizationPercent = 86,
            Status = "Đạt",
            StatusBrush = "#2D6A4F",
            Note = "TCVN 5575"
        },
        new()
        {
            Group = "Bền cắt",
            Name = "Ứng suất tiếp bản bụng",
            Limit = "τ <= Rsγc",
            CurrentValue = "0.62",
            UtilizationPercent = 62,
            Status = "Đạt",
            StatusBrush = "#2D6A4F",
            Note = "Vmax"
        },
        new()
        {
            Group = "Tổ hợp",
            Name = "Uốn và cắt đồng thời",
            Limit = "η <= 1.00",
            CurrentValue = "0.88",
            UtilizationPercent = 88,
            Status = "Theo dõi",
            StatusBrush = "#B47A17",
            Note = "ULS chi phối"
        }
    ];

    public ObservableCollection<BeamCheckResult> StabilityChecks { get; } =
    [
        new()
        {
            Group = "Uốn xoắn",
            Name = "Ổn định tổng thể dầm",
            Limit = "M <= φbMb",
            CurrentValue = "0.78",
            UtilizationPercent = 78,
            Status = "Đạt",
            StatusBrush = "#2D6A4F",
            Note = "Lb = 3.0 m"
        },
        new()
        {
            Group = "Cục bộ",
            Name = "Ổn định bản cánh nén",
            Limit = "b/t <= [b/t]",
            CurrentValue = "0.71",
            UtilizationPercent = 71,
            Status = "Đạt",
            StatusBrush = "#2D6A4F",
            Note = "Cánh I"
        },
        new()
        {
            Group = "Sử dụng",
            Name = "Độ võng dầm",
            Limit = "f <= L/250",
            CurrentValue = "0.68",
            UtilizationPercent = 68,
            Status = "Đạt",
            StatusBrush = "#2D6A4F",
            Note = "SLS"
        }
    ];

    public ICommand RunStabilityCheckCommand { get; }

    public ICommand RunStrengthCheckCommand { get; }

    public ICommand SaveReportCommand { get; }
}
