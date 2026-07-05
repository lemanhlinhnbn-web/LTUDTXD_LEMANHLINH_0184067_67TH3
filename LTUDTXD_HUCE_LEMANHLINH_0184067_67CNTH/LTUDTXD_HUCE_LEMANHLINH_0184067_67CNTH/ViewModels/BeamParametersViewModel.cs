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
    private string _saveMessage = "Chá»‰nh thÃ´ng sá»‘ rá»“i lÆ°u cho dáº§m ETABS Ä‘ang chá»n.";

    public BeamParametersViewModel(
        BeamDesignInput input,
        Func<string> getSelectedFrameText,
        Func<BeamDesignInput, string> saveForSelectedFrame)
        : base(
            "ThÃ´ng sá»‘ dáº§m thÃ©p chá»¯ I",
            "Nháº­p hÃ¬nh há»c, váº­t liá»‡u, ná»™i lá»±c vÃ  há»‡ sá»‘ á»•n Ä‘á»‹nh theo TCVN 2275:2024.")
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
        Parameters.Add(new() { Group = "HÃ¬nh há»c", Symbol = "h", Name = "Chiá»u cao tiáº¿t diá»‡n", Value = $"{Input.Height:0.###}", Unit = "mm" });
        Parameters.Add(new() { Group = "HÃ¬nh há»c", Symbol = "b", Name = "Bá» rá»™ng cÃ¡nh", Value = $"{Input.FlangeWidth:0.###}", Unit = "mm" });
        Parameters.Add(new() { Group = "HÃ¬nh há»c", Symbol = "tw", Name = "Chiá»u dÃ y bá»¥ng", Value = $"{Input.WebThickness:0.###}", Unit = "mm" });
        Parameters.Add(new() { Group = "HÃ¬nh há»c", Symbol = "tf", Name = "Chiá»u dÃ y cÃ¡nh", Value = $"{Input.FlangeThickness:0.###}", Unit = "mm" });
        Parameters.Add(new() { Group = "HÃ¬nh há»c", Symbol = "L", Name = "Nhá»‹p dáº§m", Value = $"{Input.SpanLength:0.###}", Unit = "m" });
        Parameters.Add(new() { Group = "á»”n Ä‘á»‹nh", Symbol = "Lb", Name = "Chiá»u dÃ i khÃ´ng giáº±ng", Value = $"{Input.UnbracedLength:0.###}", Unit = "m" });
        Parameters.Add(new() { Group = "á»”n Ä‘á»‹nh", Symbol = "Ï†b", Name = "Há»‡ sá»‘ á»•n Ä‘á»‹nh tá»•ng thá»ƒ", Value = $"{Input.LateralTorsionalBucklingFactor:0.###}", Unit = "-" });
        Parameters.Add(new() { Group = "Váº­t liá»‡u", Symbol = "Ry", Name = "CÆ°á»ng Ä‘á»™ tÃ­nh toÃ¡n", Value = $"{Input.DesignStrength:0.###}", Unit = "MPa" });
        Parameters.Add(new() { Group = "Váº­t liá»‡u", Symbol = "E", Name = "MÃ´ Ä‘un Ä‘Ã n há»“i", Value = $"{Input.ElasticModulus:0.###}", Unit = "MPa" });
        Parameters.Add(new() { Group = "Ná»™i lá»±c", Symbol = "Mx", Name = "MÃ´ men thiáº¿t káº¿", Value = $"{Input.DesignMoment:0.###}", Unit = "kN.m" });
        Parameters.Add(new() { Group = "Ná»™i lá»±c", Symbol = "V", Name = "Lá»±c cáº¯t thiáº¿t káº¿", Value = $"{Input.DesignShear:0.###}", Unit = "kN" });
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
