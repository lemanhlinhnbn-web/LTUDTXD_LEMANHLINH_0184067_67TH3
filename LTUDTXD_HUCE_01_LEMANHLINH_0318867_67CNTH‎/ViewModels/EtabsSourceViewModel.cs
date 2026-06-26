using System.Collections.ObjectModel;
using System.Windows.Input;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Commands;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Models;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Services;

namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.ViewModels;

public sealed class EtabsSourceViewModel : PageViewModelBase
{
    private readonly IEtabsService _etabsService;
    private string _summary;
    private string _connectionStatus = "Chưa kết nối";
    private string _connectionMessage = "Hãy mở ETABS và mô hình cần đọc trước khi kết nối.";
    private string _modelName = "Nguồn nội lực từ ETABS";
    private string _modelPath = string.Empty;
    private string _etabsVersion = "—";
    private string _statusBrush = "#F59E0B";
    private bool _isConnected;

    public EtabsSourceViewModel(
        string summary,
        ObservableCollection<InternalForceItem> internalForces,
        IEtabsService etabsService)
        : base("Nguồn ETABS", "Kết nối ETABS và lấy nội lực dầm thép dùng cho tính toán.")
    {
        _summary = summary;
        InternalForces = internalForces;
        _etabsService = etabsService;

        ConnectEtabsCommand = new RelayCommand(_ => ConnectEtabs(), _ => !IsConnected);
        DisconnectEtabsCommand = new RelayCommand(_ => DisconnectEtabs(), _ => IsConnected);
        ReadInternalForcesCommand = new RelayCommand(_ => { }, _ => IsConnected);
    }

    public ObservableCollection<InternalForceItem> InternalForces { get; }
    public ICommand ConnectEtabsCommand { get; }
    public ICommand DisconnectEtabsCommand { get; }
    public ICommand ReadInternalForcesCommand { get; }

    public string Summary
    {
        get => _summary;
        private set => SetField(ref _summary, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (!SetField(ref _isConnected, value))
            {
                return;
            }

            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetField(ref _connectionStatus, value);
    }

    public string ConnectionMessage
    {
        get => _connectionMessage;
        private set => SetField(ref _connectionMessage, value);
    }

    public string ModelName
    {
        get => _modelName;
        private set => SetField(ref _modelName, value);
    }

    public string ModelPath
    {
        get => _modelPath;
        private set => SetField(ref _modelPath, value);
    }

    public string EtabsVersion
    {
        get => _etabsVersion;
        private set => SetField(ref _etabsVersion, value);
    }

    public string StatusBrush
    {
        get => _statusBrush;
        private set => SetField(ref _statusBrush, value);
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
            ConnectionStatus = "Kết nối thất bại";
            ConnectionMessage = result.Message;
            StatusBrush = "#F43F5E";
            return;
        }

        ModelName = result.ModelName;
        ModelPath = result.ModelPath;
        EtabsVersion = result.Version;
        ConnectionStatus = "Đã kết nối";
        ConnectionMessage = result.Message;
        StatusBrush = "#20C9B0";
        Summary = $"Đang kết nối: {result.ModelName} | ETABS {result.Version}";
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
        StatusBrush = "#F59E0B";
        Summary = "Nội lực dầm lấy từ ETABS | M, V, tổ hợp chi phối";
    }

    private bool SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
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
