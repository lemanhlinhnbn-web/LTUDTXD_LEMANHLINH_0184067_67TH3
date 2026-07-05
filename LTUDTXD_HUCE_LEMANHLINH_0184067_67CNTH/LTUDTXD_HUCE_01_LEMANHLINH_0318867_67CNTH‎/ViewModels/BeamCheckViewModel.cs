using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Commands;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.ViewModels;

public sealed class BeamCheckViewModel : PageViewModelBase
{
    private readonly BeamDesignInput _input;
    private readonly IBeamCalculationService _calculationService;
    private readonly Func<string?>? _syncSelectedFrameBeforeCheck;
    private string _message = "Nhấn Kiểm tra để tính toán từ dữ liệu hiện tại.";
    private string _statusBrush = "#9BA5AF";
    private string _strengthRatioText = "—";
    private string _stabilityRatioText = "—";
    private string _governingCondition = "Chưa có kết quả";
    private BeamSectionProperties? _section;

    public BeamCheckViewModel(
        BeamDesignInput input,
        IBeamCalculationService calculationService,
        Func<string?>? syncSelectedFrameBeforeCheck = null)
        : base(
            "Kiểm tra dầm thép",
            "Kiểm tra điều kiện bền, ổn định tổng thể và ổn định cục bộ theo TCVN 2275:2024.")
    {
        _input = input;
        _calculationService = calculationService;
        _syncSelectedFrameBeforeCheck = syncSelectedFrameBeforeCheck;
        RunAllChecksCommand = new RelayCommand(_ => RunChecks());
    }

    public ObservableCollection<BeamCheckResult> StrengthChecks { get; } = [];
    public ObservableCollection<BeamCheckResult> StabilityChecks { get; } = [];
    public ICommand RunAllChecksCommand { get; }

    public string Message { get => _message; private set => SetField(ref _message, value); }
    public string StatusBrush { get => _statusBrush; private set => SetField(ref _statusBrush, value); }
    public string StrengthRatioText { get => _strengthRatioText; private set => SetField(ref _strengthRatioText, value); }
    public string StabilityRatioText { get => _stabilityRatioText; private set => SetField(ref _stabilityRatioText, value); }
    public string GoverningCondition { get => _governingCondition; private set => SetField(ref _governingCondition, value); }
    public BeamSectionProperties? Section { get => _section; private set => SetField(ref _section, value); }
    public Visibility ResultsEmptyVisibility => StrengthChecks.Count == 0 && StabilityChecks.Count == 0
        ? Visibility.Visible
        : Visibility.Collapsed;
    public Visibility ResultsListVisibility => StrengthChecks.Count == 0 && StabilityChecks.Count == 0
        ? Visibility.Collapsed
        : Visibility.Visible;

    private void RunChecks()
    {
        try
        {
            string? syncError = _syncSelectedFrameBeforeCheck?.Invoke();
            if (!string.IsNullOrWhiteSpace(syncError))
            {
                Message = syncError!;
                StatusBrush = "#F43F5E";
                return;
            }

            BeamCalculationResult result = _calculationService.Calculate(_input);
            ReplaceItems(StrengthChecks, result.StrengthChecks);
            ReplaceItems(StabilityChecks, result.StabilityChecks);
            RefreshResultVisibility();

            Section = result.Section;
            StrengthRatioText = $"η = {result.GoverningStrengthRatio:0.000}";
            StabilityRatioText = $"η = {result.GoverningStabilityRatio:0.000}";
            GoverningCondition = result.GoverningCondition;
            Message = result.IsPassed
                ? "Dầm thỏa mãn các điều kiện đang kiểm tra."
                : "Dầm không thỏa mãn ít nhất một điều kiện. Cần điều chỉnh tiết diện hoặc sơ đồ giữ ổn định.";
            StatusBrush = result.IsPassed ? "#20C9B0" : "#F43F5E";
        }
        catch (ArgumentException exception)
        {
            Message = exception.Message;
            StatusBrush = "#F43F5E";
        }
    }

    private static void ReplaceItems(
        ObservableCollection<BeamCheckResult> target,
        System.Collections.Generic.IEnumerable<BeamCheckResult> source)
    {
        target.Clear();
        foreach (BeamCheckResult item in source)
        {
            target.Add(item);
        }
    }

    private void RefreshResultVisibility()
    {
        OnPropertyChanged(nameof(ResultsEmptyVisibility));
        OnPropertyChanged(nameof(ResultsListVisibility));
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
