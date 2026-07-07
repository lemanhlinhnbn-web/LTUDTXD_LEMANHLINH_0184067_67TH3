using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Commands;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.ViewModels;

public sealed class BeamParametersViewModel : PageViewModelBase
{
    private readonly Func<string> _getSelectedFrameText;
    private readonly Func<BeamDesignInput, string> _saveForSelectedFrame;
    private string _saveMessage = "Chỉnh thông số rồi lưu cho dầm ETABS đang chọn.";

    public BeamParametersViewModel(
        BeamDesignInput input,
        Func<string> getSelectedFrameText,
        Func<BeamDesignInput, string> saveForSelectedFrame)
        : base(
            "Thông số dầm thép chữ I",
            "Nhập hình học, vật liệu, nội lực và hệ số ổn định theo TCVN 5575:2024.")
    {
        Input = input;
        _getSelectedFrameText = getSelectedFrameText;
        _saveForSelectedFrame = saveForSelectedFrame;
        Input.PropertyChanged += Input_PropertyChanged;
        SteelGrades.Add("CT3");
        SteelGrades.Add("CCT34");
        SteelGrades.Add("CCT38");
        SteelGrades.Add("CCT42");
        EnsureCurrentSteelGrade();
        RefreshParameters();
        SaveForSelectedFrameCommand = new RelayCommand(_ => SaveForSelectedFrame());
    }

    public BeamDesignInput Input { get; }
    public ObservableCollection<BeamParameterItem> Parameters { get; } = [];
    public ObservableCollection<string> SteelGrades { get; } = [];
    public ICommand SaveForSelectedFrameCommand { get; }
    public string SelectedFrameText => _getSelectedFrameText();
    public string SaveMessage { get => _saveMessage; private set => SetField(ref _saveMessage, value); }

    public void RefreshSelectedFrame()
    {
        OnPropertyChanged(nameof(SelectedFrameText));
    }

    private void SaveForSelectedFrame()
    {
        SaveMessage = _saveForSelectedFrame(Input);
        RefreshSelectedFrame();
    }

    private void Input_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BeamDesignInput.SteelGrade))
        {
            EnsureCurrentSteelGrade();
        }

        RefreshParameters();
    }

    private void EnsureCurrentSteelGrade()
    {
        if (!string.IsNullOrWhiteSpace(Input.SteelGrade)
            && !SteelGrades.Contains(Input.SteelGrade))
        {
            SteelGrades.Add(Input.SteelGrade);
        }
    }

    private void RefreshParameters()
    {
        Parameters.Clear();
        Parameters.Add(new() { Group = "Hình học", Symbol = "h", Name = "Chiều cao tiết diện", Value = $"{Input.Height:0.###}", Unit = "mm" });
        Parameters.Add(new() { Group = "Hình học", Symbol = "b", Name = "Bề rộng cánh", Value = $"{Input.FlangeWidth:0.###}", Unit = "mm" });
        Parameters.Add(new() { Group = "Hình học", Symbol = "tw", Name = "Chiều dày bụng", Value = $"{Input.WebThickness:0.###}", Unit = "mm" });
        Parameters.Add(new() { Group = "Hình học", Symbol = "tf", Name = "Chiều dày cánh", Value = $"{Input.FlangeThickness:0.###}", Unit = "mm" });
        Parameters.Add(new() { Group = "Hình học", Symbol = "L", Name = "Nhịp dầm", Value = $"{Input.SpanLength:0.###}", Unit = "m" });
        Parameters.Add(new() { Group = "Ổn định", Symbol = "Lb", Name = "Chiều dài không giằng", Value = $"{Input.UnbracedLength:0.###}", Unit = "m" });
        Parameters.Add(new() { Group = "Ổn định", Symbol = "φb", Name = "Hệ số ổn định tổng thể", Value = $"{Input.LateralTorsionalBucklingFactor:0.###}", Unit = "-" });
        Parameters.Add(new() { Group = "Vật liệu", Symbol = "Ry", Name = "Cường độ tính toán", Value = $"{Input.DesignStrength:0.###}", Unit = "MPa" });
        Parameters.Add(new() { Group = "Vật liệu", Symbol = "E", Name = "Mô đun đàn hồi", Value = $"{Input.ElasticModulus:0.###}", Unit = "MPa" });
        Parameters.Add(new() { Group = "Nội lực", Symbol = "Mx", Name = "Mô men thiết kế", Value = $"{Input.DesignMoment:0.###}", Unit = "kN.m" });
        Parameters.Add(new() { Group = "Nội lực", Symbol = "V", Name = "Lực cắt thiết kế", Value = $"{Input.DesignShear:0.###}", Unit = "kN" });
        Parameters.Add(new() { Group = "Nội lực", Symbol = "V(Max)", Name = "Lực cắt tiết diện M max", Value = $"{Input.ShearAtMaxMoment:0.###}", Unit = "kN" });
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
