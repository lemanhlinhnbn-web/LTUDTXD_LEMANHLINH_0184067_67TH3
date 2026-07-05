using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Commands;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly Dictionary<string, PageViewModelBase> _pages = new();
    private readonly EtabsSourceViewModel _etabsViewModel;
    private readonly BeamParametersViewModel _beamParametersViewModel;
    private NavigationItem? _selectedNavigationItem;
    private PageViewModelBase _currentPageViewModel = null!;

    public MainViewModel(
        IEtabsService etabsService,
        IBeamCalculationService calculationService,
        IReportExportService reportExportService,
        IWindowService windowService)
    {
        Input = new BeamDesignInput();

        _etabsViewModel = new EtabsSourceViewModel(
            InternalForceSummary,
            InternalForces,
            Input,
            etabsService);
        _beamParametersViewModel = new BeamParametersViewModel(
            Input,
            () => _etabsViewModel.SelectedFrameSaveText,
            _etabsViewModel.SaveCurrentInputForSelectedFrame);

        _etabsViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(EtabsSourceViewModel.ModelName))
            {
                OnPropertyChanged(nameof(EtabsModelName));
            }

            if (args.PropertyName == nameof(EtabsSourceViewModel.ConnectionStatus))
            {
                OnPropertyChanged(nameof(LastSyncText));
            }

            if (args.PropertyName == nameof(EtabsSourceViewModel.SelectedFrameSaveText))
            {
                _beamParametersViewModel.RefreshSelectedFrame();
            }
        };

        _pages["EtabsSource"] = _etabsViewModel;
        _pages["BeamParameters"] = _beamParametersViewModel;
        _pages["BeamChecks"] = new BeamCheckViewModel(
            Input,
            calculationService,
            _etabsViewModel.SyncSelectedFrameForCheck);

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
            Description = "TCVN 2275:2024"
        });
        NavigationItems.Add(new NavigationItem
        {
            Key = "BeamChecks",
            Icon = "\uE9F9",
            Title = "Kiểm tra",
            Description = "Bền và ổn định"
        });

        SaveReportCommand = new RelayCommand(_ => SaveReport(calculationService, reportExportService));
        MinimizeWindowCommand = new RelayCommand(_ => windowService.MinimizeMainWindow());
        ToggleMaximizeWindowCommand = new RelayCommand(_ => windowService.ToggleMaximizeMainWindow());
        CloseWindowCommand = new RelayCommand(_ => windowService.CloseMainWindow());

        SelectedNavigationItem = NavigationItems[0];
    }

    public BeamDesignInput Input { get; }
    public string ProjectName { get; } = "TÍNH DẦM THÉP";
    public string StructureName { get; } = "Dầm thép chữ I";
    public string EtabsModelName => _etabsViewModel.ModelName;
    public string LastSyncText => _etabsViewModel.ConnectionStatus;
    public string InternalForceSummary { get; } =
        "Nội lực dầm lấy từ ETABS | M, V, tổ hợp chi phối";

    public ObservableCollection<NavigationItem> NavigationItems { get; } = [];
    public ObservableCollection<InternalForceItem> InternalForces { get; } = [];

    public ICommand SaveReportCommand { get; }
    public ICommand MinimizeWindowCommand { get; }
    public ICommand ToggleMaximizeWindowCommand { get; }
    public ICommand CloseWindowCommand { get; }

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

            if (value is not null && _pages.TryGetValue(value.Key, out PageViewModelBase page))
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

    private void SaveReport(
        IBeamCalculationService calculationService,
        IReportExportService reportExportService)
    {
        try
        {
            BeamCalculationResult result = calculationService.Calculate(Input);
            reportExportService.ExportBeamReport(Input, result, EtabsModelName, LastSyncText);
        }
        catch (System.Exception exception)
        {
            System.Windows.MessageBox.Show(
                System.Windows.Application.Current.MainWindow,
                $"Không thể lưu báo cáo Excel.\n{exception.Message}",
                "Lưu báo cáo",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
